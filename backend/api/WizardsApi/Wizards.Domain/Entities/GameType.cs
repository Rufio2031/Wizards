namespace Wizards.Domain.Entities;

/// <summary>
/// Represents a game that in-store events can be played with.
/// </summary>
public class GameType
{
    /// <summary>The maximum length of a game type's name.</summary>
    public const int MaxNameLength = 100;

    /// <summary>Gets the primary key of the game type.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the unique identifier of the game type.</summary>
    public Guid PublicId { get; private set; }

    /// <summary>Gets the display name of the game type.</summary>
    public string Name { get; private set; } = string.Empty;

    private GameType() { }

    /// <summary>
    /// Creates a game type that has never been persisted, assigning it a new identifier.
    /// </summary>
    /// <param name="name">
    /// The display name of the game type. Surrounding whitespace is trimmed before the length is
    /// checked, so a name that only fits once trimmed is accepted.
    /// </param>
    /// <returns>The new game type, carrying its assigned identifier and no primary key.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is <see langword="null"/>, empty, whitespace, or longer
    /// than <see cref="MaxNameLength"/> characters once trimmed.
    /// </exception>
    public static GameType Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        name = name.Trim();

        if (name.Length > MaxNameLength)
        {
            throw new ArgumentException($"Game type name cannot exceed {MaxNameLength} characters.", nameof(name));
        }

        return new()
        {
            PublicId = Guid.CreateVersion7(),
            Name = name
        };
    }

    /// <summary>
    /// Rebuilds a game type from already-persisted state, applying no validation.
    /// </summary>
    /// <remarks>
    /// This is for persistence mapping only. Callers creating a game type for the first time must use
    /// <see cref="Create(string)"/>, which enforces the entity's invariants.
    /// </remarks>
    /// <param name="id">The stored primary key of the game type.</param>
    /// <param name="publicId">The stored identifier of the game type.</param>
    /// <param name="name">The stored display name of the game type.</param>
    /// <returns>The rehydrated game type.</returns>
    public static GameType Reconstitute(int id, Guid publicId, string name) =>
        new()
        {
            Id = id,
            PublicId = publicId,
            Name = name
        };
}
