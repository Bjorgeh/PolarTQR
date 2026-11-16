using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PolarTQR.Models;

namespace PolarTQR.Services;

public enum ApiStatus
{
    Ok,
    /// <summary>Nøkkel eller innlogging er ugyldig, trukket tilbake eller utløpt (401).</summary>
    Unauthorized,
    /// <summary>Billettmodulen er slått av, eller vakten har ikke tilgang (403).</summary>
    Disabled,
    /// <summary>Serveren avviste forespørselen med en forklaring (400, 423). Se Reason.</summary>
    Rejected,
    /// <summary>Ingen kontakt med serveren.</summary>
    Offline,
    ServerError,
}

public sealed record ApiResult<T>(ApiStatus Status, T? Value, string? Message = null, string? Reason = null)
{
    public bool IsOk => Status == ApiStatus.Ok;
}

/// <summary>
/// Klient for Borealis sin billettkontroll.
/// - /blog_tickets/scanner/*  autentiseres med X-Scanner-Key (gjelder ett arrangement)
/// - /blog_tickets/staff/*    autentiseres med "Authorization: Staff &lt;token&gt;" (innlogget vakt)
/// </summary>
public sealed class ScannerApi
{
    public const string DefaultBaseUrl = "https://borealis.polarcode.solutions";

    enum Auth { None, ScannerKey, Staff }

    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new FlaskDateTimeConverter() },
    };

    readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };

    public string BaseUrl { get; private set; } = DefaultBaseUrl;
    /// <summary>Skanner-nøkkel for arrangementet som skannes nå.</summary>
    public string? Key { get; private set; }
    /// <summary>Innloggingstoken for vakten.</summary>
    public string? StaffToken { get; private set; }

    public void Configure(string baseUrl, string? key)
    {
        BaseUrl = baseUrl.TrimEnd('/');
        Key = key;
    }

    public void SetKey(string? key) => Key = key;

    public void SetStaffToken(string? token) => StaffToken = token;

    // ---- Skanning (X-Scanner-Key) ----

    public Task<ApiResult<ScannerSession>> GetSessionAsync(CancellationToken ct = default) =>
        SendAsync<ScannerSession>(HttpMethod.Get, "/blog_tickets/scanner/session", null, Auth.ScannerKey, ct);

    public Task<ApiResult<TicketStats>> GetStatsAsync(CancellationToken ct = default) =>
        SendAsync<TicketStats>(HttpMethod.Get, "/blog_tickets/scanner/stats", null, Auth.ScannerKey, ct);

    public Task<ApiResult<CheckInResponse>> CheckInCodeAsync(string code, CancellationToken ct = default) =>
        SendAsync<CheckInResponse>(HttpMethod.Post, "/blog_tickets/scanner/check_in", new { code }, Auth.ScannerKey, ct, checkIn: true);

    public Task<ApiResult<CheckInResponse>> CheckInTicketAsync(int ticketId, CancellationToken ct = default) =>
        SendAsync<CheckInResponse>(HttpMethod.Post, "/blog_tickets/scanner/check_in", new { ticket_id = ticketId }, Auth.ScannerKey, ct, checkIn: true);

    public Task<ApiResult<UndoResponse>> UndoCheckInAsync(int ticketId, CancellationToken ct = default) =>
        SendAsync<UndoResponse>(HttpMethod.Post, "/blog_tickets/scanner/undo_check_in", new { ticket_id = ticketId }, Auth.ScannerKey, ct);

    public Task<ApiResult<SearchResponse>> SearchAsync(string query, CancellationToken ct = default) =>
        SendAsync<SearchResponse>(HttpMethod.Get, "/blog_tickets/scanner/tickets?q=" + Uri.EscapeDataString(query), null, Auth.ScannerKey, ct);

    // ---- Adgangsvakt ----

    public Task<ApiResult<StaffSession>> LoginAsync(string email, string password, CancellationToken ct = default) =>
        SendAsync<StaffSession>(HttpMethod.Post, "/blog_tickets/staff/login", new { email, password }, Auth.None, ct);

    public Task<ApiResult<StaffSession>> ActivateAsync(string email, string inviteCode, string password, CancellationToken ct = default) =>
        SendAsync<StaffSession>(HttpMethod.Post, "/blog_tickets/staff/activate",
            new { email, invite_code = inviteCode, password }, Auth.None, ct);

    public Task<ApiResult<StaffEventsResponse>> GetStaffEventsAsync(CancellationToken ct = default) =>
        SendAsync<StaffEventsResponse>(HttpMethod.Get, "/blog_tickets/staff/events", null, Auth.Staff, ct);

    public Task<ApiResult<StartScanningResponse>> StartScanningAsync(string site, int eventId, CancellationToken ct = default) =>
        SendAsync<StartScanningResponse>(HttpMethod.Post, "/blog_tickets/staff/scanner",
            new { site, event_id = eventId }, Auth.Staff, ct);

    public Task<ApiResult<ErrorResponse>> LogoutAsync(CancellationToken ct = default) =>
        SendAsync<ErrorResponse>(HttpMethod.Post, "/blog_tickets/staff/logout", null, Auth.Staff, ct);

    async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? body, Auth auth, CancellationToken ct, bool checkIn = false)
    {
        if (auth == Auth.ScannerKey && Key is null) return new(ApiStatus.Unauthorized, default, "Ingen skanner-nøkkel");
        if (auth == Auth.Staff && StaffToken is null) return new(ApiStatus.Unauthorized, default, "Ikke innlogget");
        try
        {
            using var req = new HttpRequestMessage(method, BaseUrl + path);
            if (auth == Auth.ScannerKey) req.Headers.Add("X-Scanner-Key", Key);
            if (auth == Auth.Staff) req.Headers.TryAddWithoutValidation("Authorization", "Staff " + StaffToken);
            if (body is not null) req.Content = JsonContent.Create(body, options: Json);

            using var resp = await _http.SendAsync(req, ct);

            // check_in svarer 404/409 med et vanlig resultat (not_found, already_checked_in osv.)
            var isResult = resp.IsSuccessStatusCode
                || (checkIn && resp.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict);
            if (isResult)
            {
                var value = await resp.Content.ReadFromJsonAsync<T>(Json, ct);
                return new(ApiStatus.Ok, value);
            }

            var err = await ReadErrorAsync(resp, ct);
            return resp.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new(ApiStatus.Unauthorized, default, err?.Error, err?.Reason),
                HttpStatusCode.Forbidden => new(ApiStatus.Disabled, default, err?.Error, err?.Reason),
                HttpStatusCode.BadRequest or HttpStatusCode.Locked => new(ApiStatus.Rejected, default, err?.Error, err?.Reason),
                _ => new(ApiStatus.ServerError, default, $"{(int)resp.StatusCode}: {err?.Error}"),
            };
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new(ApiStatus.Offline, default, "Serveren svarte ikke i tide");
        }
        catch (HttpRequestException ex)
        {
            return new(ApiStatus.Offline, default, ex.Message);
        }
        catch (JsonException ex)
        {
            return new(ApiStatus.ServerError, default, "Uventet svar fra serveren: " + ex.Message);
        }
    }

    static async Task<ErrorResponse?> ReadErrorAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        try { return await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json, ct); }
        catch { return new ErrorResponse { Error = resp.ReasonPhrase }; }
    }
}
