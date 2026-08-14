namespace Wizards.Infrastructure.Persistence.Records;

internal sealed class EventGameTypeSelection
{
    /// <summary>Gets or sets the primary key of the selection.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the foreign key of the event the selection belongs to.</summary>
    public int EventId { get; set; }

    /// <summary>
    /// Gets or sets the event the selection belongs to. Only populated on reads that load it.
    /// </summary>
    public Event Event { get; set; } = null!;

    /// <summary>Gets or sets the key of the game type setting the value was chosen for.</summary>
    public required string Key { get; set; }

    /// <summary>Gets or sets the chosen value.</summary>
    public required string Value { get; set; }
}
