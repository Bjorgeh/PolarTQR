using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PolarTQR.Models;
using PolarTQR.Services;

namespace PolarTQR.ViewModels;

public enum ScanOutcome { None, Ok, AlreadyIn, Rejected, Offline }

/// <summary>
/// Logikken for billettkontrollen: tar imot skannede koder, sjekker inn mot Borealis
/// og viser grønt/gult/rødt resultat. Kjører alltid på UI-tråden.
/// </summary>
public partial class ScanViewModel : ObservableObject
{
    public const string TicketPrefix = "PCT1:";
    static readonly Regex RawCode = new("^[A-Za-z0-9_-]{8,64}$", RegexOptions.Compiled);
    static readonly TimeSpan SameCodeCooldown = TimeSpan.FromSeconds(4);

    readonly ScannerApi _api;
    readonly Action<string> _onKeyRejected;
    CancellationTokenSource? _overlayCts;
    string? _lastCode;
    DateTime _lastCodeAt;

    public ScanViewModel(ScannerApi api, ScannerSession session, Action<string> onKeyRejected)
    {
        _api = api;
        _onKeyRejected = onKeyRejected;
        EventName = session.Event.EventName;
        EventTime = session.Event.StartDate is { } start ? start.ToString("dddd d. MMM 'kl.' HH:mm") : "";
        Label = session.Label;
        ApplyStats(session.Stats);
    }

    public string EventName { get; }
    public string EventTime { get; }
    public string Label { get; }

    [ObservableProperty] private int checkedIn;
    [ObservableProperty] private int issued;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool torchOn;

    // Resultatvisning (fullskjerm-overlegg)
    [ObservableProperty] private bool overlayVisible;
    [ObservableProperty] private ScanOutcome outcome;
    [ObservableProperty] private Color overlayColor = Colors.Transparent;
    [ObservableProperty] private string overlayIcon = "";
    [ObservableProperty] private string overlayTitle = "";
    [ObservableProperty] private string overlayName = "";
    [ObservableProperty] private string overlayDetail = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUndo))]
    private Ticket? lastCheckedIn;

    public bool CanUndo => LastCheckedIn is not null;

    public string CounterText => $"av {Issued}";

    partial void OnIssuedChanged(int value) => OnPropertyChanged(nameof(CounterText));

    /// <summary>Kalles fra kameraet. Ignorerer koder mens forrige behandles og samme kode rett etter.</summary>
    public async Task HandleScanAsync(string raw)
    {
        var now = DateTime.UtcNow;
        if (raw == _lastCode && now - _lastCodeAt < SameCodeCooldown)
        {
            // Samme billett ligger fortsatt foran kameraet: ikke vis «allerede sjekket inn» for den
            _lastCodeAt = now;
            return;
        }
        if (IsBusy || OverlayVisible) return;
        _lastCode = raw;
        _lastCodeAt = now;

        var isTicket = raw.StartsWith(TicketPrefix, StringComparison.Ordinal) || RawCode.IsMatch(raw);
        if (!isTicket)
        {
            ShowRejected("Ukjent QR-kode", "Dette er ikke en PolarCode-billett.");
            return;
        }

        await CheckInAsync(() => _api.CheckInCodeAsync(raw));
    }

    /// <summary>Manuell innsjekk fra søk (gjest uten billett på mobilen).</summary>
    public Task CheckInTicketAsync(int ticketId) => CheckInAsync(() => _api.CheckInTicketAsync(ticketId));

    async Task CheckInAsync(Func<Task<ApiResult<CheckInResponse>>> call)
    {
        IsBusy = true;
        try
        {
            var result = await call();
            if (!result.IsOk)
            {
                HandleFailure(result.Status, result.Message);
                return;
            }

            var r = result.Value!;
            if (r.Stats is not null) ApplyStats(r.Stats);
            var t = r.Ticket;
            switch (r.Result)
            {
                case "ok":
                    LastCheckedIn = t;
                    Show(ScanOutcome.Ok, "#16A34A", "✓", "Velkommen!", t?.HolderName ?? "", t?.TicketTypeName ?? "", null);
                    Feedback.Success();
                    break;
                case "already_checked_in":
                    var when = t?.CheckedInAt is { } at ? $"kl. {at:HH:mm}" : "";
                    var by = string.IsNullOrEmpty(t?.CheckedInBy) ? "" : $" ved {t!.CheckedInBy}";
                    Show(ScanOutcome.AlreadyIn, "#D97706", "!", "Allerede sjekket inn", t?.HolderName ?? "",
                        $"{t?.TicketTypeName} · inne {when}{by}".Trim(' ', '·'), null);
                    Feedback.Warning();
                    break;
                case "cancelled":
                    ShowRejected("Billetten er kansellert", t?.HolderName ?? "", name: true);
                    break;
                case "wrong_event":
                    ShowRejected("Feil arrangement", "Billetten gjelder et annet arrangement.");
                    break;
                default:
                    ShowRejected("Ugyldig billett", "Fant ingen billett med denne koden.");
                    break;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    async Task UndoAsync()
    {
        if (LastCheckedIn is not { } ticket || IsBusy) return;
        IsBusy = true;
        try
        {
            var result = await _api.UndoCheckInAsync(ticket.Id);
            if (!result.IsOk)
            {
                HandleFailure(result.Status, result.Message);
                return;
            }
            if (result.Value?.Stats is { } stats) ApplyStats(stats);
            LastCheckedIn = null;
            _lastCode = null; // samme billett kan skannes på nytt med en gang
            Show(ScanOutcome.None, "#475569", "↶", "Innsjekk angret", ticket.HolderName, ticket.TicketTypeName, TimeSpan.FromSeconds(2));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    void ToggleTorch() => TorchOn = !TorchOn;

    [RelayCommand]
    void DismissOverlay()
    {
        _overlayCts?.Cancel();
        OverlayVisible = false;
    }

    public async Task RefreshStatsAsync()
    {
        if (IsBusy) return;
        var result = await _api.GetStatsAsync();
        if (result.IsOk) ApplyStats(result.Value!);
        else if (result.Status is ApiStatus.Unauthorized or ApiStatus.Disabled) HandleFailure(result.Status, result.Message);
    }

    void HandleFailure(ApiStatus status, string? message)
    {
        switch (status)
        {
            case ApiStatus.Unauthorized:
                _onKeyRejected("Tilgangen til arrangementet er fjernet eller utløpt.");
                break;
            case ApiStatus.Disabled:
                _onKeyRejected("Billettkontroll er slått av for denne kunden.");
                break;
            case ApiStatus.Offline:
                _lastCode = null; // la dørvakten skanne samme billett igjen med en gang
                Show(ScanOutcome.Offline, "#334155", "⚠", "Ingen kontakt med serveren",
                    "Billetten er IKKE sjekket inn.", "Sjekk nettet og skann på nytt.", null);
                Feedback.Error();
                break;
            default:
                _lastCode = null;
                Show(ScanOutcome.Offline, "#334155", "⚠", "Serverfeil", "Billetten er IKKE sjekket inn.", message ?? "", null);
                Feedback.Error();
                break;
        }
    }

    void ShowRejected(string title, string text, bool name = false)
    {
        Show(ScanOutcome.Rejected, "#DC2626", "✕", title, name ? text : "", name ? "" : text, null);
        Feedback.Error();
    }

    /// <summary>Viser resultatet. duration = null: blir stående til vakten trykker (alle skanneresultater),
    /// så navnet rekker å bli lest og ingen ny billett skannes ved et uhell.</summary>
    void Show(ScanOutcome outcome, string color, string icon, string title, string holder, string detail, TimeSpan? duration)
    {
        _overlayCts?.Cancel();
        var cts = _overlayCts = new CancellationTokenSource();

        Outcome = outcome;
        OverlayColor = Color.FromArgb(color);
        OverlayIcon = icon;
        OverlayTitle = title;
        OverlayName = holder;
        OverlayDetail = detail;
        OverlayVisible = true;

        if (duration is { } d) _ = HideAfterAsync(d, cts.Token);
    }

    async Task HideAfterAsync(TimeSpan duration, CancellationToken ct)
    {
        try
        {
            await Task.Delay(duration, ct);
            OverlayVisible = false;
        }
        catch (TaskCanceledException) { }
    }

    void ApplyStats(TicketStats stats)
    {
        CheckedIn = stats.CheckedIn;
        Issued = stats.Issued;
    }
}
