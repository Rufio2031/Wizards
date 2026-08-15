using Wizards.Domain.Enums;
using Wizards.Domain.Exceptions;
using Wizards.Domain.Helpers;

namespace Wizards.Domain.Entities;

/// <summary>
/// One knob a game type exposes, such as the number of players or the size of a deck, together with
/// the values it will accept.
/// </summary>
/// <remarks>
/// <para>
/// Settings are what make a game type's rules data rather than code: registering a game that is
/// played differently is a matter of listing different settings, not of adding fields. A setting
/// states what it allows and answers whether a chosen value satisfies it, so no caller has to read
/// its range and reimplement the check.
/// </para>
/// <para>
/// Every value is carried as text, whatever the setting's kind, because the settings a game type
/// exposes are not known at compile time and so cannot share a typed shape.
/// </para>
/// </remarks>
public class GameTypeSetting
{
    /// <summary>The maximum length of a setting's key.</summary>
    public const int MaxKeyLength = 50;

    /// <summary>The maximum length of a setting's label.</summary>
    public const int MaxLabelLength = 100;

    /// <summary>The maximum length of a setting's description.</summary>
    public const int MaxDescriptionLength = 500;

    /// <summary>
    /// The maximum length of any value a setting holds: its default, each of its options, and a value
    /// chosen for it on an event. One cap, because a default and an option are both copied onto an
    /// event as the chosen value.
    /// </summary>
    public const int MaxValueLength = 100;

    private List<GameTypeSettingOption> options = [];

    /// <summary>Gets the primary key of the setting.</summary>
    public int Id { get; private set; }

    /// <summary>
    /// Gets the stable identifier of the setting, such as <c>deckSize</c>. Unique within the game type
    /// that exposes it, and matched without regard to case.
    /// </summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>Gets the name the setting is presented under, such as <c>Deck size</c>.</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the explanation of the setting shown alongside it, or <see langword="null"/> when the
    /// label says enough on its own.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>Gets the kind of value the setting holds.</summary>
    public SettingType Type { get; private set; }

    /// <summary>
    /// Gets the smallest value the setting accepts, or <see langword="null"/> when it is unbounded
    /// below. Only ever set for a <see cref="SettingType.Int"/> setting.
    /// </summary>
    public int? MinValue { get; private set; }

    /// <summary>
    /// Gets the largest value the setting accepts, or <see langword="null"/> when it is unbounded
    /// above. Only ever set for a <see cref="SettingType.Int"/> setting.
    /// </summary>
    public int? MaxValue { get; private set; }

    /// <summary>
    /// Gets the value used when an organizer does not choose one. Always a value this setting
    /// accepts, so falling back to it can never produce a state the setting forbids.
    /// </summary>
    public string DefaultValue { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the fixed values the setting allows, which is empty for every kind other than
    /// <see cref="SettingType.Enum"/>.
    /// </summary>
    public IReadOnlyList<GameTypeSettingOption> Options => this.options;

    private GameTypeSetting() { }

    /// <summary>
    /// Creates a setting that has never been persisted, trimming the key, label, description, and
    /// default value before their lengths are checked. A null or whitespace description leaves the
    /// setting without one.
    /// </summary>
    /// <returns>The new setting, carrying no primary key.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="type"/> is not a defined <see cref="SettingType"/>.
    /// </exception>
    /// <exception cref="DomainException">
    /// Thrown when <paramref name="key"/>, <paramref name="label"/>, or
    /// <paramref name="defaultValue"/> is missing or too long, when <paramref name="description"/> is
    /// too long, when a bound or an option list is supplied for a kind that has no use for it, when
    /// <paramref name="minValue"/> exceeds <paramref name="maxValue"/>, when an enum setting lists no
    /// options or lists the same one twice, when an option value is missing or too long, or when
    /// <paramref name="defaultValue"/> is not a value the resulting setting accepts.
    /// </exception>
    public static GameTypeSetting Create(
        string key,
        string label,
        SettingType type,
        string defaultValue,
        int? minValue = null,
        int? maxValue = null,
        string? description = null,
        params string[] options)
    {
        key = ValidateAndNormalizeKey(key);
        label = ValidateAndNormalizeLabel(key, label);
        description = ValidateAndNormalizeDescription(key, description);
        defaultValue = ValidateAndNormalizeDefaultValue(key, defaultValue);

        List<GameTypeSettingOption> settingOptions = options
            .Select(GameTypeSettingOption.Create)
            .ToList();

        ValidateShape(key, type, minValue, maxValue, settingOptions);

        GameTypeSetting setting = new()
        {
            Key = key,
            Label = label,
            Description = description,
            Type = type,
            MinValue = minValue,
            MaxValue = maxValue,
            DefaultValue = defaultValue,
            options = settingOptions
        };

        if (!setting.Accepts(defaultValue))
        {
            throw new DomainException(
                $"The default value of the '{key}' setting must be {setting.DescribeAllowedValues()}.");
        }

        setting.DefaultValue = setting.Normalize(defaultValue);

        return setting;
    }

    /// <summary>
    /// Rebuilds a setting from already-persisted state, applying no validation.
    /// </summary>
    /// <remarks>
    /// This is for persistence mapping only. Callers creating a setting for the first time must use
    /// <see cref="Create"/>, which enforces the entity's invariants.
    /// </remarks>
    /// <returns>The rehydrated setting.</returns>
    public static GameTypeSetting Reconstitute(
        int id,
        string key,
        string label,
        string? description,
        SettingType type,
        string defaultValue,
        int? minValue,
        int? maxValue,
        IEnumerable<GameTypeSettingOption>? options = null) =>
        new()
        {
            Id = id,
            Key = key,
            Label = label,
            Description = description,
            Type = type,
            MinValue = minValue,
            MaxValue = maxValue,
            DefaultValue = defaultValue,
            options = options?.ToList() ?? []
        };

    /// <summary>
    /// Reports whether a chosen value is one this setting allows.
    /// </summary>
    /// <remarks>
    /// The range a setting permits is its own rule, so it is answered here rather than read off by
    /// whoever is doing the choosing. Surrounding whitespace is ignored, and an enum value is matched
    /// without regard to case.
    /// </remarks>
    /// <param name="value">The chosen value, as text.</param>
    /// <returns>
    /// <see langword="true"/> when the value satisfies this setting, otherwise
    /// <see langword="false"/>. A <see langword="null"/> or blank value is never accepted.
    /// </returns>
    public bool Accepts(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        value = value.Trim();

        return this.Type switch
        {
            SettingType.Int => NumberHelper.TryParseInt(value, out int parsed)
                && (!this.MinValue.HasValue || parsed >= this.MinValue.Value)
                && (!this.MaxValue.HasValue || parsed <= this.MaxValue.Value),
            SettingType.Bool => bool.TryParse(value, out _),
            SettingType.Enum => this.options.Any(
                option => string.Equals(option.Value, value, StringComparison.OrdinalIgnoreCase)),
            _ => false
        };
    }

    /// <summary>
    /// Reduces an accepted value to the single form this setting stores it in.
    /// </summary>
    /// <remarks>
    /// Storing one form per value means two events that chose the same thing compare equal, however
    /// the value was spelled on the way in. The caller must already have established that the value is
    /// accepted; anything else is returned trimmed and otherwise untouched.
    /// </remarks>
    /// <param name="value">The chosen value, which <see cref="Accepts"/> has already approved.</param>
    /// <returns>The value as this setting stores it.</returns>
    internal string Normalize(string value)
    {
        value = value.Trim();

        switch (this.Type)
        {
            case SettingType.Int when NumberHelper.TryParseInt(value, out int parsed):
                return NumberHelper.ToText(parsed);

            case SettingType.Bool when bool.TryParse(value, out bool parsed):
                return parsed ? "true" : "false";

            case SettingType.Enum:
                return this.options
                    .FirstOrDefault(option =>
                        string.Equals(option.Value, value, StringComparison.OrdinalIgnoreCase))
                    ?.Value ?? value;

            default:
                return value;
        }
    }

    /// <summary>
    /// States what this setting allows, phrased to complete a sentence such as "player count must be".
    /// </summary>
    /// <remarks>
    /// Written for the organizer who chose a value this setting turned down, so it names the bounds
    /// and options rather than the rule that rejected them.
    /// </remarks>
    /// <returns>The description of the allowed values.</returns>
    internal string DescribeAllowedValues() =>
        this.Type switch
        {
            SettingType.Int when this.MinValue.HasValue && this.MaxValue.HasValue =>
                $"a whole number between {this.MinValue.Value} and {this.MaxValue.Value}",
            SettingType.Int when this.MinValue.HasValue => $"a whole number of at least {this.MinValue.Value}",
            SettingType.Int when this.MaxValue.HasValue => $"a whole number of at most {this.MaxValue.Value}",
            SettingType.Int => "a whole number",
            SettingType.Bool => "true or false",
            SettingType.Enum => $"one of: {string.Join(", ", this.options.Select(option => option.Value))}",
            _ => "a supported value"
        };

    private static string ValidateAndNormalizeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException("A game type setting key is required.");
        }

        key = key.Trim();

        if (key.Length > MaxKeyLength)
        {
            throw new DomainException($"A game type setting key cannot exceed {MaxKeyLength} characters.");
        }

        return key;
    }

    private static string ValidateAndNormalizeLabel(string key, string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new DomainException($"A label is required for the '{key}' setting.");
        }

        label = label.Trim();

        if (label.Length > MaxLabelLength)
        {
            throw new DomainException(
                $"The label of the '{key}' setting cannot exceed {MaxLabelLength} characters.");
        }

        return label;
    }

    private static string? ValidateAndNormalizeDescription(string key, string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        description = description.Trim();

        if (description.Length > MaxDescriptionLength)
        {
            throw new DomainException(
                $"The description of the '{key}' setting cannot exceed {MaxDescriptionLength} characters.");
        }

        return description;
    }

    private static string ValidateAndNormalizeDefaultValue(string key, string defaultValue)
    {
        if (string.IsNullOrWhiteSpace(defaultValue))
        {
            throw new DomainException($"A default value is required for the '{key}' setting.");
        }

        defaultValue = defaultValue.Trim();

        if (defaultValue.Length > MaxValueLength)
        {
            throw new DomainException(
                $"The default value of the '{key}' setting cannot exceed {MaxValueLength} characters.");
        }

        return defaultValue;
    }
    
    private static void ValidateShape(
        string key,
        SettingType type,
        int? minValue,
        int? maxValue,
        List<GameTypeSettingOption> options)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentException($"The '{key}' setting has an unrecognized type.", nameof(type));
        }

        if (type == SettingType.Int)
        {
            if (minValue.HasValue && maxValue.HasValue && minValue.Value > maxValue.Value)
            {
                throw new DomainException(
                    $"The minimum value of the '{key}' setting cannot exceed its maximum value.");
            }
        }
        else if (minValue.HasValue || maxValue.HasValue)
        {
            throw new DomainException(
                $"The '{key}' setting is not a whole number setting and cannot carry bounds.");
        }

        if (type == SettingType.Enum)
        {
            if (options.Count == 0)
            {
                throw new DomainException(
                    $"The '{key}' setting is a choice setting and must list at least one option.");
            }
            
            HashSet<string> seenValues = new(StringComparer.OrdinalIgnoreCase);

            foreach (GameTypeSettingOption option in options)
            {
                if (!seenValues.Add(option.Value))
                {
                    throw new DomainException($"The '{key}' setting cannot list the same option twice.");
                }
            }

        }
        else if (options.Count > 0)
        {
            throw new DomainException(
                $"The '{key}' setting is not a choice setting and cannot carry options.");
        }
    }
}