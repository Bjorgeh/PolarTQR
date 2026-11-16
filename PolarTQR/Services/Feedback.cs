namespace PolarTQR.Services;

/// <summary>Vibrasjon ved skann, så dørvakten slipper å se på skjermen hver gang.</summary>
public static class Feedback
{
    public static void Success() => Try(() => Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(80)));

    public static void Warning() => Try(() => Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(400)));

    public static void Error() => Try(async () =>
    {
        // To korte støt: lett å kjenne forskjell på OK og avvist
        Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(250));
        await Task.Delay(400);
        Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(250));
    });

    static async void Try(Func<Task> action)
    {
        try { await action(); } catch { /* ikke alle enheter har vibrasjon */ }
    }

    static void Try(Action action)
    {
        try { action(); } catch { }
    }
}
