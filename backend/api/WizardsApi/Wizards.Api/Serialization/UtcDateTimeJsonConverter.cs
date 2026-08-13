using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wizards.Api.Serialization;

/// <summary>
/// Reads every inbound date and time as a UTC instant and writes every outbound one the same way.
/// </summary>
/// <remarks>
/// <para>
/// This is the one place inbound instants are normalized. Everything behind it, the request DTOs, the
/// services, the domain entities and the database, deals only in <see cref="DateTimeKind.Utc"/>, and
/// <c>Event</c> rejects anything else outright, so a caller's zone is resolved here or nowhere.
/// </para>
/// <para>
/// A value carrying an offset is converted to the instant it denotes, so <c>18:00:00+02:00</c> and
/// <c>16:00:00Z</c> are the same instant and store identically. A value carrying no zone marker is
/// read as UTC rather than as the server's local time, because the server's zone is an accident of
/// deployment and would make the same request mean different things on different hosts.
/// </para>
/// <para>
/// Registering this replaces the default handling for <see cref="DateTime"/> and, since the
/// serializer unwraps nullables onto the underlying converter, for <see cref="Nullable{T}"/> of
/// <see cref="DateTime"/> as well.
/// </para>
/// </remarks>
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    private const DateTimeStyles UtcStyles = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal;

    // K absorbs a trailing Z, an explicit offset such as +02:00, or no zone marker at all, and the
    // "." before F is itself dropped when no fractional digits are supplied.
    private static readonly string[] AcceptedInstantFormats =
    [
        "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
        "yyyy-MM-ddTHH:mmK"
    ];

    /// <summary>
    /// Reads an ISO 8601 date and time and returns the UTC instant it denotes.
    /// </summary>
    /// <param name="reader">The reader positioned on the value to read.</param>
    /// <param name="typeToConvert">The type being deserialized. Always <see cref="DateTime"/>.</param>
    /// <param name="options">The serializer options in force. Not consulted.</param>
    /// <returns>The instant, carrying <see cref="DateTimeKind.Utc"/>.</returns>
    /// <exception cref="JsonException">
    /// Thrown when the value is not a string, or is not an ISO 8601 date and time of the shape
    /// <c>2026-08-13T16:00:00</c>, with the seconds or the fractional seconds optionally omitted and
    /// with an optional <c>Z</c> or offset suffix. A date on its own is rejected, because an instant
    /// needs a time. Model binding reports this as a validation error rather than a server fault.
    /// </exception>
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected a JSON string holding an ISO 8601 date and time, such as 2026-08-13T16:00:00Z.");
        }

        string? suppliedInstant = reader.GetString();

        if (!DateTime.TryParseExact(suppliedInstant, AcceptedInstantFormats, CultureInfo.InvariantCulture, UtcStyles, out DateTime instant))
        {
            throw new JsonException("Expected an ISO 8601 date and time, such as 2026-08-13T16:00:00Z.");
        }

        return instant;
    }

    /// <summary>
    /// Writes a date and time as a UTC instant with a trailing <c>Z</c>.
    /// </summary>
    /// <param name="writer">The writer to write the value to.</param>
    /// <param name="value">The instant to write.</param>
    /// <param name="options">The serializer options in force. Not consulted.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer"/> is <see langword="null"/>.
    /// </exception>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        DateTime utcInstant = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        writer.WriteStringValue(utcInstant);
    }
}
