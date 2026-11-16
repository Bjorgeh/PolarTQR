using PolarTQR.Services;

namespace PolarTQR.Pages;

/// <summary>Startsiden: adgangsvakten logger inn med e-post og passord.</summary>
public partial class LoginPage : ContentPage
{
    readonly App _app;
    readonly ScannerApi _api;
    readonly SettingsStore _settings;
    bool _working;

    public LoginPage(App app, ScannerApi api, SettingsStore settings, string? message, bool canRetry)
    {
        InitializeComponent();
        _app = app;
        _api = api;
        _settings = settings;
        Email.Text = settings.LastEmail;
        RetryButton.IsVisible = canRetry;
        ServerButton.Text = settings.ServerLabel;
        if (message is not null) ShowStatus(message);
    }

    void OnEmailCompleted(object? sender, EventArgs e) => Password.Focus();

    async void OnLogin(object? sender, EventArgs e)
    {
        if (_working) return;
        var email = (Email.Text ?? "").Trim();
        var password = Password.Text ?? "";
        if (email.Length == 0 || password.Length == 0)
        {
            ShowStatus("Skriv inn e-post og passord.");
            return;
        }

        SetBusy(true);
        try
        {
            _api.Configure(_settings.BaseUrl, null);
            var result = await _api.LoginAsync(email, password);
            if (result.IsOk)
            {
                await _app.OnStaffLoggedInAsync(result.Value!, email);
                return;
            }
            Password.Text = "";
            ShowStatus(result.Status switch
            {
                ApiStatus.Unauthorized => "Feil e-post eller passord.",
                ApiStatus.Rejected when result.Reason == "locked" => "For mange feil forsøk. Vent 15 minutter og prøv igjen.",
                ApiStatus.Disabled => "Billettkontroll er ikke aktivert for arrangøren din.",
                ApiStatus.Offline => "Får ikke kontakt med serveren. Sjekk nettet.",
                _ => "Noe gikk galt: " + result.Message,
            });
        }
        finally
        {
            SetBusy(false);
        }
    }

    async void OnRetry(object? sender, EventArgs e)
    {
        RetryButton.IsVisible = false;
        ShowStatus("Kobler til…");
        await _app.StartAsync();
    }

    async void OnActivate(object? sender, EventArgs e) =>
        await Navigation.PushAsync(new ActivatePage(_app, _api, _settings, Email.Text));

    async void OnChangeServer(object? sender, EventArgs e)
    {
        if (await _settings.PromptServerAsync(this)) ServerButton.Text = _settings.ServerLabel;
    }

    void SetBusy(bool busy)
    {
        _working = busy;
        LoginButton.IsEnabled = !busy;
        Busy.IsVisible = Busy.IsRunning = busy;
    }

    void ShowStatus(string text)
    {
        StatusBox.IsVisible = true;
        Status.Text = text;
    }
}
