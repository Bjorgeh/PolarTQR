using PolarTQR.Models;
using PolarTQR.Services;
using PolarTQR.ViewModels;
using ZXing.Net.Maui;

namespace PolarTQR.Pages;

public partial class ScanPage : ContentPage
{
    readonly App _app;
    readonly ScannerApi _api;
    readonly ScanViewModel _vm;
    IDispatcherTimer? _statsTimer;

    public ScanPage(App app, ScannerApi api, ScannerSession session)
    {
        InitializeComponent();
        _app = app;
        _api = api;
        _vm = new ScanViewModel(api, session, message => _app.OnKeyRejected(message));
        BindingContext = _vm;
        Camera.Options = new BarcodeReaderOptions { Formats = BarcodeFormat.QrCode, AutoRotate = true, Multiple = false };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        DeviceDisplay.Current.KeepScreenOn = true;

        // Telleren oppdateres ved hvert skann, og hvert 20. sekund for innsjekk fra andre dører
        _statsTimer = Dispatcher.CreateTimer();
        _statsTimer.Interval = TimeSpan.FromSeconds(20);
        _statsTimer.Tick += async (_, _) => await _vm.RefreshStatsAsync();
        _statsTimer.Start();

        Camera.IsDetecting = await CameraPermission.EnsureAsync(CameraMessage);
    }

    protected override void OnDisappearing()
    {
        Camera.IsDetecting = false;
        _statsTimer?.Stop();
        DeviceDisplay.Current.KeepScreenOn = false;
        base.OnDisappearing();
    }

    void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        var value = e.Results.FirstOrDefault()?.Value;
        if (string.IsNullOrEmpty(value)) return;
        MainThread.BeginInvokeOnMainThread(async () => await _vm.HandleScanAsync(value));
    }

    async void OnSearch(object? sender, EventArgs e) =>
        await Navigation.PushAsync(new SearchPage(_api, _vm));

    async void OnLeave(object? sender, EventArgs e) => await _app.LeaveEventAsync();

    async void OnLogout(object? sender, EventArgs e)
    {
        if (await DisplayAlertAsync("Logg ut?", "Du må logge inn på nytt for å skanne.", "Logg ut", "Avbryt"))
            await _app.LogoutAsync();
    }
}
