namespace Wizards.Infrastructure.Persistence.Records;

/// <summary>
/// Represents an in-store game event.
/// </summary>
internal sealed class Event
{
    /// <summary>Gets or sets the primary key of the event.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the unique identifier of the event.</summary>
    public Guid PublicId { get; set; }

    /// <summary>Gets or sets the display name of the event.</summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the long-form description of the event, or <see langword="null"/> when the
    /// organizer has not supplied one.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the foreign key of the game the event is played with.</summary>
    public int GameTypeId { get; set; }

    /// <summary>
    /// Gets or sets the game the event is played with. Only populated on reads that explicitly load
    /// it; writes set <see cref="GameTypeId"/> and leave this alone so the game type is never
    /// inserted or updated as a side effect of saving an event.
    /// </summary>
    public GameType GameType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the instant the event starts, in UTC. SQLite carries no kind, so the value is
    /// marked UTC by the context's own value conversion as it is read, not by whoever consumes it.
    /// </summary>
    public DateTime StartDateTime { get; set; }

    /// <summary>
    /// Gets or sets the instant the event ends, in UTC, or <see langword="null"/> when the event has no
    /// scheduled end. Marked UTC on read in the same way as <see cref="StartDateTime"/>.
    /// </summary>
    public DateTime? EndDateTime { get; set; }

    /// <summary>Gets or sets the maximum number of players who may register.</summary>
    public int RegistrationLimit { get; set; }
}
