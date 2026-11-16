namespace PolarTQR.Services;

/// <summary>
/// Hemmeligheter (skanner-nøkkel og vaktens innlogging) ligger i SecureStorage
/// (Android Keystore / iOS Keychain). Server-URL ligger i Preferences.
/// </summary>
public sealed class SettingsStore
{
    const string KeyName = "scanner_key";
    const string StaffTokenName = "staff_token";
    const string BaseUrlName = "base_url";
    const string LastEmailName = "last_email";

    public string BaseUrl
    {
        get => Preferences.Default.Get(BaseUrlName, ScannerApi.DefaultBaseUrl);
        set => Preferences.Default.Set(BaseUrlName, value.Trim().TrimEnd('/'));
    }

    /// <summary>Forrige e-post, så vakten slipper å skrive den hver gang.</summary>
    public string LastEmail
    {
        get => Preferences.Default.Get(LastEmailName, "");
        set => Preferences.Default.Set(LastEmailName, value.Trim());
    }

    public Task<string?> GetKeyAsync() => GetSecretAsync(KeyName);
    public Task SaveKeyAsync(string key) => SecureStorage.Default.SetAsync(KeyName, key);
    public void ClearKey() => SecureStorage.Default.Remove(KeyName);

    public Task<string?> GetStaffTokenAsync() => GetSecretAsync(StaffTokenName);
    public Task SaveStaffTokenAsync(string token) => SecureStorage.Default.SetAsync(StaffTokenName, token);
    public void ClearStaffToken() => SecureStorage.Default.Remove(StaffTokenName);

    static async Task<string?> GetSecretAsync(string name)
    {
        try { return await SecureStorage.Default.GetAsync(name); }
        catch
        {
            // Kan skje hvis Keystore er nullstilt (f.eks. ny skjermlås). Da må vakten logge inn på nytt.
            SecureStorage.Default.Remove(name);
            return null;
        }
    }

    /// <summary>Felles dialog for å bytte server (brukes bare ved testing).</summary>
    public async Task<bool> PromptServerAsync(Page page)
    {
        var value = await page.DisplayPromptAsync("Server", "Adressen til Borealis. Endres bare ved testing.",
            accept: "Lagre", cancel: "Avbryt", initialValue: BaseUrl, keyboard: Keyboard.Url);
        if (value is null) return false;
        value = string.IsNullOrWhiteSpace(value) ? ScannerApi.DefaultBaseUrl : value.Trim();
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http"))
        {
            await page.DisplayAlertAsync("Ugyldig adresse", "Adressen må starte med https://", "OK");
            return false;
        }
        BaseUrl = value;
        return true;
    }

    public string ServerLabel => BaseUrl == ScannerApi.DefaultBaseUrl ? "Server: standard" : "Server: " + BaseUrl;
}
