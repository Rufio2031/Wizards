using Wizards.Domain.Exceptions;

namespace Wizards.Domain.Entities;

/// <summary>
/// Represents a game that in-store events can be played with.
/// </summary>
/// <remarks>
/// What varies between one game and the next is carried as <see cref="Settings"/> rather than as
/// fields, so registering a game played by rules the application has never seen is a matter of
/// listing different settings rather than of changing the schema.
/// </remarks>
public class GameType
{
    /// <summary>The maximum length of a game type's name.</summary>
    public const int MaxNameLength = 100;

    private List<GameTypeSetting> settings = [];

    /// <summary>Gets the primary key of the game type.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the unique identifier of the game type.</summary>
    public Guid PublicId { get; private set; }

    /// <summary>Gets the display name of the game type.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the settings or rules this game type exposes, in the order they were supplied.
    /// </summary>
    public IReadOnlyList<GameTypeSetting> Settings => this.settings;

    private GameType() { }

    /// <summary>
    /// Creates a game type that has never been persisted, assigning it a new identifier.
    /// </summary>
    /// <param name="name">
    /// The display name of the game type. Surrounding whitespace is trimmed before the length is
    /// checked, so a name that only fits once trimmed is accepted.
    /// </param>
    /// <param name="settings">
    /// The settings the game type exposes, kept in the order given, or <see langword="null"/> when it
    /// exposes none.
    /// </param>
    /// <returns>The new game type, carrying its assigned identifier and no primary key.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="settings"/> contains a null entry.
    /// </exception>
    /// <exception cref="DomainException">
    /// Thrown when <paramref name="name"/> is <see langword="null"/>, empty, whitespace, or longer
    /// than <see cref="MaxNameLength"/> characters once trimmed, or when <paramref name="settings"/>
    /// names the same key twice.
    /// </exception>
    public static GameType Create(string name, IEnumerable<GameTypeSetting>? settings = null)
    {
        name = ValidateAndNormalizeName(name);

        List<GameTypeSetting> gameTypeSettings = settings?.ToList() ?? [];

        ValidateSettings(name, gameTypeSettings);

        return new()
        {
            PublicId = Guid.CreateVersion7(),
            Name = name,
            settings = gameTypeSettings
        };
    }

    /// <summary>
    /// Rebuilds a game type from already-persisted state, applying no validation.
    /// </summary>
    /// <remarks>
    /// This is for persistence mapping only. Callers creating a game type for the first time must use
    /// <see cref="Create(string, IEnumerable{GameTypeSetting})"/>, which enforces the entity's
    /// invariants.
    /// </remarks>
    /// <param name="id">The stored primary key of the game type.</param>
    /// <param name="publicId">The stored identifier of the game type.</param>
    /// <param name="name">The stored display name of the game type.</param>
    /// <param name="settings">The stored settings of the game type, already rehydrated.</param>
    /// <returns>The rehydrated game type.</returns>
    public static GameType Reconstitute(
        int id,
        Guid publicId,
        string name,
        IEnumerable<GameTypeSetting>? settings = null) =>
        new()
        {
            Id = id,
            PublicId = publicId,
            Name = name,
            settings = settings?.ToList() ?? []
        };

    /// <summary>
    /// Checks the settings an organizer chose against what this game type exposes, filling in the ones
    /// they left alone.
    /// </summary>
    /// <remarks>
    /// Every setting this game type exposes appears in the result, whether it was chosen or defaulted,
    /// and each value comes back in the form its setting stores it in.
    /// </remarks>
    /// <param name="selections">
    /// The settings the organizer chose, in any order, or <see langword="null"/> to accept every
    /// default.
    /// </param>
    /// <returns>
    /// One selection per setting this game type exposes, in the same order as <see cref="Settings"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="selections"/> contains a null entry.
    /// </exception>
    /// <exception cref="DomainException">
    /// Thrown when a setting is chosen more than once, when a choice names a setting this game type
    /// does not expose, or when a chosen value is not one its setting allows.
    /// </exception>
    public IReadOnlyList<EventGameTypeSelection> Validate(IEnumerable<EventGameTypeSelection>? selections)
    {
        List<EventGameTypeSelection> chosen = selections?.ToList() ?? [];

        if (chosen.Any(selection => selection is null))
        {
            throw new ArgumentException("A chosen setting cannot be null.", nameof(selections));
        }

        Dictionary<string, string> chosenValues = new(StringComparer.OrdinalIgnoreCase);

        foreach (EventGameTypeSelection selection in chosen)
        {
            if (!chosenValues.TryAdd(selection.Key, selection.Value))
            {
                throw new DomainException($"The '{selection.Key}' setting was chosen more than once.")
                {
                    Key = selection.Key
                };
            }
        }

        List<EventGameTypeSelection> validated = new(this.settings.Count);

        foreach (GameTypeSetting setting in this.settings)
        {
            if (!chosenValues.Remove(setting.Key, out string? value))
            {
                validated.Add(EventGameTypeSelection.Create(setting.Key, setting.DefaultValue));

                continue;
            }

            if (!setting.Accepts(value))
            {
                throw new DomainException(
                    $"The {this.Name} '{setting.Key}' setting must be {setting.DescribeAllowedValues()}.")
                {
                    Key = setting.Key
                };
            }

            validated.Add(EventGameTypeSelection.Create(setting.Key, setting.Normalize(value)));
        }

        // Each setting removed the value chosen for it, so anything still here names no setting.
        if (chosenValues.Count > 0)
        {
            string unknownKey = chosenValues.Keys.First();

            throw new DomainException($"{this.Name} has no '{unknownKey}' setting.")
            {
                Key = unknownKey
            };
        }

        return validated;
    }

    private static string ValidateAndNormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Game type name is required.");
        }

        name = name.Trim();

        if (name.Length > MaxNameLength)
        {
            throw new DomainException($"Game type name cannot exceed {MaxNameLength} characters.");
        }

        return name;
    }

    private static void ValidateSettings(string name, List<GameTypeSetting> settings)
    {
        if (settings.Any(setting => setting is null))
        {
            throw new ArgumentException("A game type setting cannot be null.", nameof(settings));
        }

        // Selections name the setting they were chosen for by key, so two settings sharing a key would
        // leave which of them a chosen value answered undecidable.
        HashSet<string> seenKeys = new(StringComparer.OrdinalIgnoreCase);

        foreach (GameTypeSetting setting in settings)
        {
            if (!seenKeys.Add(setting.Key))
            {
                throw new DomainException(
                    $"Game type '{name}' cannot expose the '{setting.Key}' setting twice.");
            }
        }
    }
}
