namespace Wizards.Infrastructure.Persistence.Records;

internal sealed class GameType
{
    /// <summary>Gets or sets the primary key of the game type.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the unique identifier of the game type.</summary>
    public Guid PublicId { get; set; }

    /// <summary>Gets or sets the display name of the game type.</summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the settings the game type exposes. Only populated on reads that explicitly load
    /// them.
    /// </summary>
    public List<GameTypeSetting> Settings { get; set; } = [];
}
