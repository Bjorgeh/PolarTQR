using PolarTQR.Models;
using PolarTQR.Services;

namespace PolarTQR.Pages;

/// <summary>Arrangementene vakten er godkjent for. Trykk for å begynne å skanne.</summary>
public partial class EventPickerPage : ContentPage
{
    readonly App _app;
    readonly ScannerApi _api;
    bool _working;

    public EventPickerPage(App app, ScannerApi api, SettingsStore settings, StaffEventsResponse data, string? message)
    {
        InitializeComponent();
        _app = app;
        _api = api;
        Show(data);
        if (message is not null) ShowStatus(message);
    }

    void Show(StaffEventsResponse data)
    {
        Hello.Text = $"Hei, {data.Staff.Name.Split(' ')[0]}";
        Events.ItemsSource = data.Events;
    }

    async void OnRefresh(object? sender, EventArgs e)
    {
        var result = await _api.GetStaffEventsAsync();
        Refresh.IsRefreshing = false;
        if (result.IsOk)
        {
            StatusBox.IsVisible = false;
            Show(result.Value!);
        }
        else if (result.Status is ApiStatus.Unauthorized or ApiStatus.Disabled) await _app.ShowEventsOrLoginAsync();
        else ShowStatus("Får ikke kontakt med serveren. Dra ned for å prøve igjen.");
    }

    async void OnEventTapped(object? sender, TappedEventArgs e)
    {
        if (_working || e.Parameter is not StaffEvent ev) return;
        _working = true;
        ShowStatus($"Åpner {ev.EventName}…");
        try
        {
            var start = await _api.StartScanningAsync(ev.Site, ev.EventId);
            if (!start.IsOk)
            {
                if (start.Status == ApiStatus.Unauthorized) { await _app.ShowEventsOrLoginAsync(); return; }
                ShowStatus(start.Status switch
                {
                    ApiStatus.Disabled => "Du har ikke lenger tilgang til dette arrangementet.",
                    ApiStatus.Offline => "Får ikke kontakt med serveren. Sjekk nettet.",
                    _ => "Noe gikk galt: " + start.Message,
                });
                return;
            }

            _api.SetKey(start.Value!.Key);
            var session = await _api.GetSessionAsync();
            if (!session.IsOk)
            {
                _api.SetKey(null);
                ShowStatus("Kunne ikke åpne arrangementet: " + session.Message);
                return;
            }
            await _app.OnScannerKeyAsync(start.Value.Key, session.Value!);
        }
        finally
        {
            _working = false;
        }
    }

    async void OnLogout(object? sender, EventArgs e)
    {
        if (await DisplayAlertAsync("Logg ut?", "Du må logge inn på nytt for å skanne.", "Logg ut", "Avbryt"))
            await _app.LogoutAsync();
    }

    void ShowStatus(string text)
    {
        StatusBox.IsVisible = true;
        Status.Text = text;
    }
}
