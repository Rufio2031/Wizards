using Wizards.Domain.Exceptions;

namespace Wizards.Domain.Entities;

/// <summary>One setting an organizer settled for an event, as a key and the value chosen for it.</summary>
public class EventGameTypeSelection
{
    /// <summary>Gets the primary key of the selection.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the key of the game type setting this value was chosen for.</summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>Gets the chosen value, as text.</summary>
    public string Value { get; private set; } = string.Empty;

    private EventGameTypeSelection() { }

    /// <summary>
    /// Creates a selection that has never been persisted, trimming the key and value before their
    /// lengths are checked.
    /// </summary>
    /// <remarks>The value is not checked against the setting the key names.</remarks>
    /// <param name="key">
    /// The key of the game type setting being settled, capped at
    /// <see cref="GameTypeSetting.MaxKeyLength"/> characters once trimmed.
    /// </param>
    /// <param name="value">
    /// The value chosen for that setting, capped at <see cref="GameTypeSetting.MaxValueLength"/>
    /// characters once trimmed.
    /// </param>
    /// <returns>The new selection, carrying no primary key.</returns>
    /// <exception cref="DomainException">
    /// Thrown when the key or the value is missing or too long.
    /// </exception>
    public static EventGameTypeSelection Create(string key, string value)
    {
        key = ValidateAndNormalizeKey(key);
        value = ValidateAndNormalizeValue(key, value);

        return new()
        {
            Key = key,
            Value = value
        };
    }

    /// <summary>Rebuilds a selection from already-persisted state, applying no validation.</summary>
    /// <remarks>
    /// This is for persistence mapping only, and a new selection must come from
    /// <see cref="Create(string, string)"/>.
    /// </remarks>
    /// <param name="id">The stored primary key of the selection.</param>
    /// <param name="key">The stored key of the game type setting the value was chosen for.</param>
    /// <param name="value">The stored value, as text.</param>
    /// <returns>The rehydrated selection.</returns>
    public static EventGameTypeSelection Reconstitute(int id, string key, string value) =>
        new()
        {
            Id = id,
            Key = key,
            Value = value
        };

    private static string ValidateAndNormalizeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException("A game type setting key is required.");
        }

        key = key.Trim();

        if (key.Length > GameTypeSetting.MaxKeyLength)
        {
            throw new DomainException(
                $"A game type setting key cannot exceed {GameTypeSetting.MaxKeyLength} characters.");
        }

        return key;
    }

    private static string ValidateAndNormalizeValue(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"A value is required for the '{key}' setting.") { Key = key };
        }

        value = value.Trim();

        if (value.Length > GameTypeSetting.MaxValueLength)
        {
            throw new DomainException(
                $"The value chosen for the '{key}' setting cannot exceed {GameTypeSetting.MaxValueLength} characters.")
            {
                Key = key
            };
        }

        return value;
    }
}
