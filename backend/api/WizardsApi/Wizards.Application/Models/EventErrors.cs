using Wizards.Application.Enums;

namespace Wizards.Application.Models;

/// <summary>
/// The failures an event write can report.
/// </summary>
public static class EventErrors
{
    /// <summary>
    /// No game type carries the requested identifier, so nothing was written.
    /// </summary>
    public static readonly ApplicationError GameTypeNotFound = new(
        ErrorKind.Invalid,
        "gameType.gameTypeId",
        "No game type is registered under that identifier.");

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

    /// <summary>
    /// Reports that the value chosen for one game type setting breaks a rule the game type states, so
    /// nothing was written.
    /// </summary>
    /// <remarks>
    /// Attributed to the field the value arrived in, so a caller rendering a form can mark the input
    /// that has to change rather than the form as a whole.
    /// </remarks>
    /// <param name="message">
    /// The explanation of the rule that was broken, stated by the domain and safe to surface as-is.
    /// </param>
    /// <param name="settingKey">The key of the setting the rule is about.</param>
    /// <returns>The failure to report to the caller.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="message"/> or <paramref name="settingKey"/> is
    /// <see langword="null"/>, empty, or whitespace.
    /// </exception>
    public static ApplicationError InvalidSelection(string message, string settingKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(settingKey);

        return new ApplicationError(
            ErrorKind.Invalid,
            $"gameType.selections.{settingKey}",
            message);
    }
}
