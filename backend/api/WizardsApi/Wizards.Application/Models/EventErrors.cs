using Wizards.Application.Enums;

namespace Wizards.Application.Models;

/// <summary>The failures an event write can report.</summary>
public static class EventErrors
{
    /// <summary>No game type carries the requested identifier, so nothing was written.</summary>
    public static readonly ApplicationError GameTypeNotFound = new(
        ErrorKind.Invalid,
        "gameType.gameTypeId",
        "No game type is registered under that identifier.");

    private const string SelectionsField = "gameType.selections";

    /// <summary>
    /// Reports that the supplied details break a rule about what makes a valid event, so nothing was
    /// written.
    /// </summary>
    /// <param name="message">The explanation of the rule that was broken, safe to surface as-is.</param>
    /// <param name="field">
    /// The request field the failure is attributed to, or null to attribute it to the request as a
    /// whole.
    /// </param>
    /// <returns>The failure to report to the caller.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the message is blank, or the field is supplied but blank.
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

    /// <summary>
    /// Reports that the settings chosen for the game type break a rule the game type states, so
    /// nothing was written.
    /// </summary>
    /// <param name="message">The explanation of the rule that was broken, safe to surface as-is.</param>
    /// <param name="settingKey">
    /// The key of the setting the rule is about, or null to attribute the failure to the chosen
    /// settings as a whole.
    /// </param>
    /// <returns>The failure to report to the caller.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the message is blank, or the setting key is supplied but blank.
    /// </exception>
    public static ApplicationError InvalidSelection(string message, string? settingKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (settingKey is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(settingKey);
        }

        return new ApplicationError(
            ErrorKind.Invalid,
            settingKey is null ? SelectionsField : $"{SelectionsField}.{settingKey}",
            message);
    }
}
