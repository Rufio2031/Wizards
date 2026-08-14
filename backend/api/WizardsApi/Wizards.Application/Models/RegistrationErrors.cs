using Wizards.Application.Enums;

namespace Wizards.Application.Models;

/// <summary>
/// The failures a registration write can report.
/// </summary>
public static class RegistrationErrors
{
    /// <summary>
    /// No event carries the requested identifier, so nothing was written.
    /// </summary>
    public static readonly ApplicationError EventNotFound = new(
        ErrorKind.NotFound,
        string.Empty,
        "No event is scheduled under that identifier.");

    /// <summary>
    /// The event has taken every registration it accepts, so nothing was written.
    /// </summary>
    /// <remarks>
    /// The request itself is well formed and resending it unchanged is what the player would do once a
    /// seat frees up, so this reports the state of the event rather than a fault in the request.
    /// </remarks>
    public static readonly ApplicationError EventFull = new(
        ErrorKind.Conflict,
        string.Empty,
        "This event is full.");

    /// <summary>
    /// Reports that the supplied details break a rule about what makes a valid registration, so
    /// nothing was written.
    /// </summary>
    /// <param name="message">
    /// The explanation of the rule that was broken, stated by the domain and safe to surface as-is.
    /// </param>
    /// <param name="field">
    /// The field the failure is attributed to, or <see langword="null"/> when the rule spans more than
    /// one, in which case it is attributed to the request as a whole with the empty key model-level
    /// validation failures use. The registration's fields are named the same in the domain and on the
    /// wire, so a key the domain states is carried across as it stands.
    /// </param>
    /// <returns>The failure to report to the caller.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="message"/> is <see langword="null"/>, empty, or whitespace, or when
    /// <paramref name="field"/> is supplied but is empty or whitespace.
    /// </exception>
    public static ApplicationError Invalid(string message, string? field = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (field is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(field);
        }

        return new ApplicationError(ErrorKind.Invalid, field ?? string.Empty, message);
    }
}
