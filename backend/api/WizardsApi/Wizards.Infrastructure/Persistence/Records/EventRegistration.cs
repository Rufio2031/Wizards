namespace Wizards.Infrastructure.Persistence.Records;

internal sealed class EventRegistration
{
    /// <summary>Gets or sets the primary key of the registration.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the foreign key of the event the player registered for.</summary>
    public int EventId { get; set; }

    /// <summary>
    /// Gets or sets the event the player registered for. Only populated on reads that load it.
    /// </summary>
    public Event Event { get; set; } = null!;

    /// <summary>Gets or sets the name the player registered under.</summary>
    public required string Name { get; set; }
}
