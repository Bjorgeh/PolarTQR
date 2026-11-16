namespace PolarTQR.Services;

public static class CameraPermission
{
    /// <summary>Ber om kameratilgang. Viser forklaring i <paramref name="message"/> hvis det avslås.</summary>
    public static async Task<bool> EnsureAsync(Label message)
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<Permissions.Camera>();

        var granted = status == PermissionStatus.Granted;
        message.IsVisible = !granted;
        if (!granted)
            message.Text = "Appen trenger tilgang til kameraet for å skanne. Slå det på under Innstillinger → Apper → PolarTQR → Tillatelser.";
        return granted;
    }
}
