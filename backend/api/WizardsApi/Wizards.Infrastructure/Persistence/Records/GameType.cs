namespace Wizards.Infrastructure.Persistence.Records;

/// <summary>
/// Represents a game that in-store events can be played with.
/// </summary>
internal sealed class GameType
{
    /// <summary>Gets or sets the primary key of the game type.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the unique identifier of the game type.</summary>
    public Guid PublicId { get; set; }

    /// <summary>Gets or sets the display name of the game type.</summary>
    public required string Name { get; set; }
}
