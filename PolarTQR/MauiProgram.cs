using Microsoft.Extensions.Logging;
using PolarTQR.Services;
using ZXing.Net.Maui.Controls;

namespace PolarTQR;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Appen er på norsk: datoer og klokkeslett også, uansett språk på telefonen
        var norwegian = new System.Globalization.CultureInfo("nb-NO");
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = norwegian;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = norwegian;

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Tekstfelt uten Androids understrek: feltene får egen avrundet ramme (stilen «Field»)
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
        });

        builder.Services.AddSingleton<ScannerApi>();
        builder.Services.AddSingleton<SettingsStore>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
