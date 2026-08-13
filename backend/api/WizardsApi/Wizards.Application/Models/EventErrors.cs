using Wizards.Application.Enums;

namespace Wizards.Application.Models;

/// <summary>
/// The failures an event write can report.
/// </summary>
public static class EventErrors
{
    /// <summary>
    /// No game type is registered under the requested name, so nothing was written.
    /// </summary>
    public static readonly ApplicationError GameTypeNotFound = new(
        ErrorKind.Invalid,
        "gameType.name",
        "No game type is registered under that name.");

    /// <summary>
    /// No event carries the supplied identifier, so nothing was written.
    /// </summary>
    public static readonly ApplicationError EventNotFound = new(
        ErrorKind.NotFound,
        "eventId",
        "No event carries the supplied identifier.");

    /// <summary>
    /// Reports that the supplied details break a rule about what makes a valid event, so nothing was
    /// written.
    /// </summary>
    /// <remarks>
    /// A rule of this kind constrains the details against one another rather than any single field, so
    /// the failure is attributed to the request as a whole with the empty key model-level validation
    /// failures use.
    /// </remarks>
    /// <param name="message">
    /// The explanation of the rule that was broken, stated by the domain and safe to surface as-is.
    /// </param>
    /// <returns>The failure to report to the caller.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="message"/> is <see langword="null"/>, empty, or whitespace.
    /// </exception>
    public static ApplicationError Invalid(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new ApplicationError(ErrorKind.Invalid, string.Empty, message);
    }
}
