namespace PolarTQR.Pages;

/// <summary>Vises et øyeblikk ved oppstart mens lagret nøkkel sjekkes mot serveren.</summary>
public sealed class StartupPage : ContentPage
{
    readonly App _app;
    bool _started;

    public StartupPage(App app)
    {
        _app = app;
        BackgroundColor = Color.FromArgb("#0F172A");
        Content = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Spacing = 16,
            Children =
            {
                new Label { Text = "PolarTQR", FontSize = 32, FontFamily = "OpenSansSemibold", TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center },
                new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#38BDF8") },
            },
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_started) return;
        _started = true;
        await _app.StartAsync();
    }
}
