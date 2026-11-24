using PolarTQR.Models;
using PolarTQR.Pages;
using PolarTQR.Services;

namespace PolarTQR;

/// <summary>
/// Navigering: Innlogging ─▶ Velg arrangement ─▶ Skanner.
/// Vakter har alltid egen bruker (invitert av arrangøren). Ved oppstart hoppes det
/// rett til det siste steget som fortsatt er gyldig.
/// </summary>
public partial class App : Application
{
    readonly ScannerApi _api;
    readonly SettingsStore _settings;

    public App(ScannerApi api, SettingsStore settings)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;
        _api = api;
        _settings = settings;
    }

    protected override Window CreateWindow(Microsoft.Maui.IActivationState? activationState) =>
        new(new StartupPage(this));

    internal async Task StartAsync()
    {
        _api.Configure(_settings.BaseUrl, await _settings.GetKeyAsync());
        _api.SetStaffToken(await _settings.GetStaffTokenAsync());

        if (_api.StaffToken is null)
        {
            // Ingen innlogging (eller rester fra en eldre versjon med oppsett-QR): start på nytt
            ClearKey();
            ShowLogin();
            return;
        }

        if (_api.Key is not null)
        {
            var session = await _api.GetSessionAsync();
            if (session.IsOk)
            {
                ShowScanner(session.Value!);
                return;
            }
            if (session.Status == ApiStatus.Offline)
            {
                ShowLogin("Får ikke kontakt med serveren. Sjekk nettet og trykk «Prøv igjen».", canRetry: true);
                return;
            }
            ClearKey();
        }

        await ShowEventsOrLoginAsync();
    }

    /// <summary>Etter innlogging, eller når vakten vil bytte arrangement.</summary>
    internal async Task ShowEventsOrLoginAsync(string? message = null)
    {
        var events = await _api.GetStaffEventsAsync();
        switch (events.Status)
        {
            case ApiStatus.Ok:
                SetRoot(Nav(new EventPickerPage(this, _api, _settings, events.Value!, message)));
                break;
            case ApiStatus.Offline:
                ShowLogin("Får ikke kontakt med serveren. Sjekk nettet og trykk «Prøv igjen».", canRetry: true);
                break;
            case ApiStatus.Disabled:
                ClearStaff();
                ShowLogin("Billettkontroll er slått av for denne arrangøren.");
                break;
            default:
                ClearStaff();
                ShowLogin("Du er logget ut. Logg inn på nytt.");
                break;
        }
    }

    internal async Task OnStaffLoggedInAsync(StaffSession session, string email)
    {
        await _settings.SaveStaffTokenAsync(session.Token);
        _settings.LastEmail = email;
        _api.SetStaffToken(session.Token);
        await ShowEventsOrLoginAsync();
    }

    internal async Task OnScannerKeyAsync(string key, ScannerSession session)
    {
        await _settings.SaveKeyAsync(key);
        _api.SetKey(key);
        ShowScanner(session);
    }

    internal void ShowLogin(string? message = null, bool canRetry = false) =>
        SetRoot(Nav(new LoginPage(this, _api, _settings, message, canRetry)));

    internal void ShowScanner(ScannerSession session) =>
        SetRoot(Nav(new ScanPage(this, _api, session)));

    /// <summary>Nøkkelen ble avvist under skanning (trukket tilbake, utløpt, tilgang fjernet).</summary>
    internal async void OnKeyRejected(string message)
    {
        ClearKey();
        await ShowEventsOrLoginAsync(message);
    }

    /// <summary>Tilbake til listen over arrangementer.</summary>
    internal async Task LeaveEventAsync()
    {
        ClearKey();
        await ShowEventsOrLoginAsync();
    }

    internal async Task LogoutAsync()
    {
        await _api.LogoutAsync(); // feiler stille uten nett; tokenet slettes uansett lokalt
        ClearKey();
        ClearStaff();
        ShowLogin();
    }

    void ClearKey()
    {
        _settings.ClearKey();
        _api.SetKey(null);
    }

    void ClearStaff()
    {
        _settings.ClearStaffToken();
        _api.SetStaffToken(null);
    }

    static NavigationPage Nav(Page root) => new(root)
    {
        BarBackgroundColor = Color.FromArgb("#0B1220"),
        BarTextColor = Colors.White,
    };

    static void SetRoot(Page page) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (Current?.Windows.FirstOrDefault() is { } window) window.Page = page;
        });
}
