using Wizards.Domain.Exceptions;

namespace Wizards.Domain.Entities;

/// <summary>
/// One setting an organizer settled for an event, as a key and the value chosen for it.
/// </summary>
/// <remarks>
/// A selection is a copy taken when the event was created, not a pointer at the game type's setting,
/// so later edits to the game type leave a scheduled event as it was booked.
/// </remarks>
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
    /// <remarks>
    /// This checks only that a key and a value are present and short enough. It does not check the
    /// value against the setting the key names, because a selection holds no reference to the game
    /// type. <see cref="GameType.Validate"/> does that check.
    /// </remarks>
    /// <returns>The new selection, carrying no primary key.</returns>
    /// <exception cref="DomainException">
    /// Thrown when <paramref name="key"/> or <paramref name="value"/> is <see langword="null"/>,
    /// empty, whitespace, or too long.
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

    /// <summary>
    /// Rebuilds a selection from already-persisted state, applying no validation.
    /// </summary>
    /// <remarks>
    /// This is for persistence mapping only. Callers creating a selection for the first time must use
    /// <see cref="Create(string, string)"/>, which enforces the entity's invariants.
    /// </remarks>
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
            throw new DomainException($"A value is required for the '{key}' setting.");
        }

        value = value.Trim();

        if (value.Length > GameTypeSetting.MaxValueLength)
        {
            throw new DomainException(
                $"The value chosen for the '{key}' setting cannot exceed {GameTypeSetting.MaxValueLength} characters.");
        }

        return value;
    }
}
