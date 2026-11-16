using PolarTQR.Models;
using PolarTQR.Services;
using PolarTQR.ViewModels;

namespace PolarTQR.Pages;

/// <summary>Manuelt oppslag: gjesten har ikke billetten på mobilen, men står på listen.</summary>
public partial class SearchPage : ContentPage
{
    readonly ScannerApi _api;
    readonly ScanViewModel _scan;
    CancellationTokenSource? _searchCts;

    public SearchPage(ScannerApi api, ScanViewModel scan)
    {
        InitializeComponent();
        _api = api;
        _scan = scan;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Query.Focus();
    }

    async void OnQueryChanged(object? sender, TextChangedEventArgs e)
    {
        _searchCts?.Cancel();
        var cts = _searchCts = new CancellationTokenSource();
        var q = (e.NewTextValue ?? "").Trim();
        if (q.Length < 2)
        {
            Results.ItemsSource = null;
            Hint.Text = "Skriv minst to bokstaver.";
            return;
        }

        try
        {
            await Task.Delay(300, cts.Token); // vent til dørvakten er ferdig med å skrive
            var result = await _api.SearchAsync(q, cts.Token);
            if (cts.IsCancellationRequested) return;
            if (!result.IsOk)
            {
                Hint.Text = result.Status == ApiStatus.Offline ? "Ingen kontakt med serveren." : "Søket feilet: " + result.Message;
                return;
            }
            var tickets = result.Value!.Tickets;
            Results.ItemsSource = tickets;
            Hint.Text = tickets.Count switch
            {
                0 => "Ingen treff.",
                50 => "Viser de 50 første. Skriv mer for å snevre inn.",
                _ => $"{tickets.Count} treff. Trykk på en gjest for å sjekke inn.",
            };
        }
        catch (TaskCanceledException) { }
    }

    async void OnTicketTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not Ticket t) return;
        if (t.IsCancelled)
        {
            await DisplayAlertAsync("Kansellert", $"Billetten til {t.HolderName} er kansellert.", "OK");
            return;
        }
        if (t.IsCheckedIn)
        {
            await DisplayAlertAsync("Allerede inne", $"{t.HolderName} ble sjekket inn kl. {t.CheckedInAt:HH:mm}" +
                (string.IsNullOrEmpty(t.CheckedInBy) ? "." : $" ved {t.CheckedInBy}."), "OK");
            return;
        }

        var ok = await DisplayAlertAsync("Sjekk inn?",
            $"{t.HolderName}\n{t.HolderEmail}\n{t.TicketTypeName}\n\nBe om legitimasjon hvis du er usikker.", "Sjekk inn", "Avbryt");
        if (!ok) return;

        // Tilbake til skanneren, der resultatet vises i grønt/rødt som ved skann
        await Navigation.PopAsync();
        await _scan.CheckInTicketAsync(t.Id);
    }
}
