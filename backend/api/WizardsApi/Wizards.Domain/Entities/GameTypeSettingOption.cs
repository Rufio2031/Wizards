using Wizards.Domain.Exceptions;

namespace Wizards.Domain.Entities;

/// <summary>
/// One of the fixed values a <see cref="SettingType.Enum"/> setting allows.
/// </summary>
/// <remarks>
/// The value doubles as the label shown to the organizer, so it is stored in the casing it should
/// be presented in.
/// </remarks>
public class GameTypeSettingOption
{
    /// <summary>Gets the primary key of the option.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the value this option allows, which is also its display label.</summary>
    public string Value { get; private set; } = string.Empty;

    private GameTypeSettingOption() { }

    /// <summary>
    /// Creates an option that has never been persisted.
    /// </summary>
    /// <param name="value">
    /// The value this option allows. Surrounding whitespace is trimmed before the length is checked,
    /// so a value that only fits once trimmed is accepted.
    /// </param>
    /// <returns>The new option, carrying no primary key.</returns>
    /// <exception cref="DomainException">
    /// Thrown when <paramref name="value"/> is <see langword="null"/>, empty, whitespace, or longer
    /// than <see cref="GameTypeSetting.MaxValueLength"/> characters once trimmed.
    /// </exception>
    public static GameTypeSettingOption Create(string value)
    {
        value = ValidateAndNormalizeValue(value);

        return new()
        {
            Value = value
        };
    }

    /// <summary>
    /// Rebuilds an option from already-persisted state, applying no validation.
    /// </summary>
    /// <param name="id">The stored primary key of the option.</param>
    /// <param name="value">The stored value of the option.</param>
    /// <returns>The rehydrated option.</returns>
    public static GameTypeSettingOption Reconstitute(int id, string value) =>
        new()
        {
            Id = id,
            Value = value
        };

    private static string ValidateAndNormalizeValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("A game type setting option value is required.");
        }

        value = value.Trim();

        if (value.Length > GameTypeSetting.MaxValueLength)
        {
            throw new DomainException(
                $"A game type setting option value cannot exceed {GameTypeSetting.MaxValueLength} characters.");
        }

        return value;
    }
}
