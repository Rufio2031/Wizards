using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wizards.Domain.Extensions;

namespace Wizards.Api.Serialization;

/// <summary>
/// Reads every inbound date and time as a UTC instant and writes every outbound one the same way.
/// </summary>
/// <remarks>
/// Instants arriving in a query string are bound by the framework and never reach this converter.
/// Registering it also covers <see cref="Nullable{T}"/> of <see cref="DateTime"/>, since the
/// serializer unwraps nullables.
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
    /// Thrown when the value is not a string, or not of the shape <c>2026-08-13T16:00:00</c> with
    /// optional seconds, fractional seconds and <c>Z</c> or offset suffix. A bare date is rejected,
    /// because an instant needs a time. Model binding surfaces this as a validation error rather
    /// than a server fault.
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

        writer.WriteStringValue(value.ToUtcInstant());
    }
}
