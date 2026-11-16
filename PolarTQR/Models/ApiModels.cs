using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PolarTQR.Models;

/// <summary>Svar fra GET /blog_tickets/scanner/session.</summary>
public sealed class ScannerSession
{
    public string Label { get; set; } = "";
    public EventInfo Event { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
    public TicketStats Stats { get; set; } = new();
}

public sealed class EventInfo
{
    public int Id { get; set; }
    public string EventName { get; set; } = "";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public sealed class TicketStats
{
    public int Issued { get; set; }
    public int CheckedIn { get; set; }
    public int Cancelled { get; set; }
    public int? Capacity { get; set; }
}

public sealed class Ticket
{
    public int Id { get; set; }
    public string TicketTypeName { get; set; } = "";
    public string HolderName { get; set; } = "";
    public string HolderEmail { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? CheckedInAt { get; set; }
    public string? CheckedInBy { get; set; }

    public bool IsCancelled => Status == "cancelled";
    public bool IsCheckedIn => CheckedInAt is not null;
    public string StateText => IsCancelled ? "Kansellert"
        : IsCheckedIn ? $"Inne {CheckedInAt:HH:mm}" : "Ikke sjekket inn";
}

/// <summary>Svar fra POST /blog_tickets/scanner/check_in (både 200, 404 og 409).</summary>
public sealed class CheckInResponse
{
    /// <summary>ok, not_found, wrong_event, cancelled eller already_checked_in.</summary>
    public string Result { get; set; } = "";
    public string? Error { get; set; }
    public Ticket? Ticket { get; set; }
    public TicketStats? Stats { get; set; }
}

public sealed class UndoResponse
{
    public Ticket? Ticket { get; set; }
    public TicketStats? Stats { get; set; }
}

public sealed class SearchResponse
{
    public List<Ticket> Tickets { get; set; } = [];
}

public sealed class ErrorResponse
{
    public string? Error { get; set; }
    /// <summary>Maskinlesbar årsak, f.eks. weak_password, invalid_code, invalid_credentials, locked.</summary>
    public string? Reason { get; set; }
}

// ---- Adgangsvakter ----

public sealed class StaffInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

/// <summary>Svar fra /blog_tickets/staff/login og /activate.</summary>
public sealed class StaffSession
{
    public string Token { get; set; } = "";
    public StaffInfo Staff { get; set; } = new();
}

public sealed class StaffEvent
{
    public string Site { get; set; } = "";
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public string When => StartDate is { } s ? s.ToString("dddd d. MMM 'kl.' HH:mm") : "";
    public bool IsOngoing => StartDate <= DateTime.Now && EndDate >= DateTime.Now;
}

public sealed class StaffEventsResponse
{
    public StaffInfo Staff { get; set; } = new();
    public List<StaffEvent> Events { get; set; } = [];
}

public sealed class StartScanningResponse
{
    public string Key { get; set; } = "";
}

/// <summary>
/// Flask serialiserer datetime som RFC 1123 ("Sun, 20 Dec 2026 19:00:00 GMT"), men
/// verdiene i databasen er lokal tid uten tidssone. Vi beholder derfor klokkeslettet
/// slik det står, uten å konvertere til telefonens tidssone.
/// </summary>
public sealed class FlaskDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        var s = reader.GetString();
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateTime.TryParseExact(s, "r", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var rfc))
            return DateTime.SpecifyKind(rfc, DateTimeKind.Unspecified);
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var other))
            return DateTime.SpecifyKind(other, DateTimeKind.Unspecified);
        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
    }
}
