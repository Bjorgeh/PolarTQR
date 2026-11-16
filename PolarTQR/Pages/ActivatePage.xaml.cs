using PolarTQR.Services;

namespace PolarTQR.Pages;

/// <summary>Første innlogging (eller glemt passord): invitasjonskode + eget passord.</summary>
public partial class ActivatePage : ContentPage
{
    readonly App _app;
    readonly ScannerApi _api;
    readonly SettingsStore _settings;
    bool _working;

    public ActivatePage(App app, ScannerApi api, SettingsStore settings, string? email)
    {
        InitializeComponent();
        _app = app;
        _api = api;
        _settings = settings;
        Email.Text = email;
    }

    async void OnActivate(object? sender, EventArgs e)
    {
        if (_working) return;
        var email = (Email.Text ?? "").Trim();
        var code = (Code.Text ?? "").Trim();
        if (email.Length == 0 || code.Length == 0 || string.IsNullOrEmpty(Password.Text))
        {
            ShowStatus("Fyll inn e-post, kode og passord.");
            return;
        }
        if (Password.Text != Password2.Text)
        {
            ShowStatus("Passordene er ikke like.");
            return;
        }

        _working = true;
        ActivateButton.IsEnabled = false;
        Busy.IsVisible = Busy.IsRunning = true;
        try
        {
            _api.Configure(_settings.BaseUrl, null);
            var result = await _api.ActivateAsync(email, code, Password.Text!);
            if (result.IsOk)
            {
                await _app.OnStaffLoggedInAsync(result.Value!, email);
                return;
            }
            ShowStatus(result.Reason switch
            {
                "weak_password" => "Passordet er for svakt. Bruk minst 8 tegn med stor og liten bokstav, tall og et spesialtegn.",
                "invalid_code" => "Koden er feil eller utløpt. Sjekk e-posten, eller be arrangøren sende en ny kode.",
                _ when result.Status == ApiStatus.Offline => "Får ikke kontakt med serveren. Sjekk nettet.",
                _ => "Noe gikk galt: " + result.Message,
            });
        }
        finally
        {
            _working = false;
            ActivateButton.IsEnabled = true;
            Busy.IsVisible = Busy.IsRunning = false;
        }
    }

    void ShowStatus(string text)
    {
        StatusBox.IsVisible = true;
        Status.Text = text;
    }
}
