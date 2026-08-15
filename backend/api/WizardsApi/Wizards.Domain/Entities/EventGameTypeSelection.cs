using Wizards.Domain.Exceptions;

namespace Wizards.Domain.Entities;

/// <summary>One setting an organizer settled for an event, as the setting and the value chosen for it.</summary>
public class EventGameTypeSelection
{
    /// <summary>Gets the primary key of the selection.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the game type setting this value was chosen for.</summary>
    public GameTypeSetting GameTypeSetting { get; private set; } = null!;

    /// <summary>Gets the chosen value, as text.</summary>
    public string Value { get; private set; } = string.Empty;

    private EventGameTypeSelection() { }

    /// <summary>
    /// Creates a selection that has never been persisted, trimming the value before its length is
    /// checked and storing it in the form the setting stores it in.
    /// </summary>
    /// <param name="setting">The game type setting being settled.</param>
    /// <param name="value">
    /// The value chosen for that setting, capped at <see cref="GameTypeSetting.MaxValueLength"/>
    /// characters once trimmed.
    /// </param>
    /// <returns>The new selection, carrying no primary key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the setting is null.</exception>
    /// <exception cref="DomainException">
    /// Thrown when the value is missing, too long, or not one the setting accepts.
    /// </exception>
    public static EventGameTypeSelection Create(GameTypeSetting setting, string value)
    {
        ArgumentNullException.ThrowIfNull(setting);

        value = ValidateAndNormalizeValue(setting, value);

        return new()
        {
            GameTypeSetting = setting,
            Value = value
        };
    }

    /// <summary>Rebuilds a selection from already-persisted state, applying no validation.</summary>
    /// <remarks>
    /// This is for persistence mapping only, and a new selection must come from
    /// <see cref="Create(GameTypeSetting, string)"/>.
    /// </remarks>
    /// <param name="id">The stored primary key of the selection.</param>
    /// <param name="setting">The stored game type setting the value was chosen for, already rehydrated.</param>
    /// <param name="value">The stored value, as text.</param>
    /// <returns>The rehydrated selection.</returns>
    public static EventGameTypeSelection Reconstitute(int id, GameTypeSetting setting, string value) =>
        new()
        {
            Id = id,
            GameTypeSetting = setting,
            Value = value
        };

    private static string ValidateAndNormalizeValue(GameTypeSetting setting, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"A value is required for the '{setting.Key}' setting.")
            {
                Key = setting.Key
            };
        }

        value = value.Trim();

        if (value.Length > GameTypeSetting.MaxValueLength)
        {
            throw new DomainException(
                $"The value chosen for the '{setting.Key}' setting cannot exceed {GameTypeSetting.MaxValueLength} characters.")
            {
                Key = setting.Key
            };
        }

        if (!setting.Accepts(value))
        {
            throw new DomainException(
                $"The value chosen for the '{setting.Key}' setting must be {setting.DescribeAllowedValues()}.")
            {
                Key = setting.Key
            };
        }

        return setting.Normalize(value);
    }
}
