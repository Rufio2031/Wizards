using Wizards.Domain.Exceptions;

namespace Wizards.Domain.Entities;

/// <summary>
/// Represents an event in the system.
/// </summary>
public class Event
{
    /// <summary>The maximum length of an event's name.</summary>
    public const int MaxNameLength = 100;

    /// <summary>The maximum length of an event's description.</summary>
    public const int MaxDescriptionLength = 2000;

    /// <summary>The maximum number of players who may register for an event.</summary>
    private const int MaxRegistrationLimit = 30;

    /// <summary>Gets the primary key of the event.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the unique identifier of the event.</summary>
    public Guid PublicId { get; private set; }

    /// <summary>The name of the event.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The long-form description of the event, or <see langword="null"/> when the organizer has not
    /// supplied one.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// The instant the event begins, in UTC. Always carries <see cref="DateTimeKind.Utc"/>.
    /// </summary>
    public DateTime StartDateTime { get; private set; }

    /// <summary>
    /// The instant the event ends, in UTC, or <see langword="null"/> when the event has no scheduled
    /// end. When a value is present it always carries <see cref="DateTimeKind.Utc"/> and falls strictly
    /// after <see cref="StartDateTime"/>.
    /// </summary>
    public DateTime? EndDateTime { get; private set; }

    /// <summary>The game type of the event.</summary>
    public GameType GameType { get; private set; } = null!;

    /// <summary>The registration limit for the event.</summary>
    public int RegistrationLimit { get; private set; }

    private Event() { }

    /// <summary>
    /// Creates an event that has never been persisted, assigning it a new identifier and the standard
    /// registration limit.
    /// </summary>
    /// <param name="name">
    /// The display name of the event. Surrounding whitespace is trimmed before the length is checked.
    /// </param>
    /// <param name="description">
    /// The long-form description of the event, trimmed when supplied, or <see langword="null"/> for an
    /// event without one.
    /// </param>
    /// <param name="gameType">The game the event is played with.</param>
    /// <param name="startDateTime">
    /// The instant the event begins, which must be UTC and must not already have passed.
    /// </param>
    /// <param name="endDateTime">
    /// The instant the event ends, which must be UTC and fall strictly after
    /// <paramref name="startDateTime"/>, or <see langword="null"/> for an event with no scheduled end.
    /// </param>
    /// <returns>The new event, carrying its assigned identifier and no primary key.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="gameType"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when either instant is not <see cref="DateTimeKind.Utc"/>.
    /// </exception>
    /// <exception cref="DomainException">
    /// Thrown when <paramref name="name"/> is <see langword="null"/>, empty, whitespace, or too long,
    /// when <paramref name="description"/> is too long, when <paramref name="startDateTime"/> is in the
    /// past, or when <paramref name="endDateTime"/> does not fall after
    /// <paramref name="startDateTime"/>. The message states the rule that was broken and is safe to
    /// report to the originator of the request.
    /// </exception>
    public static Event Create(
        string name,
        string? description,
        GameType gameType,
        DateTime startDateTime,
        DateTime? endDateTime = null)
    {
        ArgumentNullException.ThrowIfNull(gameType);

        name = ValidateAndNormalizeName(name);
        description = ValidateAndNormalizeDescription(description);

        ValidateSchedule(startDateTime, endDateTime);

        return new()
        {
            PublicId = Guid.CreateVersion7(),
            Name = name,
            Description = description,
            GameType = gameType,
            StartDateTime = startDateTime,
            EndDateTime = endDateTime,
            RegistrationLimit = MaxRegistrationLimit
        };
    }

    /// <summary>
    /// Replaces every caller-supplied detail of the event in full, so a <see langword="null"/> optional
    /// value clears the value it replaces rather than leaving it untouched.
    /// </summary>
    /// <remarks>
    /// The start date and time must not already have passed, exactly as it must not for
    /// <see cref="Create"/>. Because every detail is replaced in full, an event whose start has already
    /// passed cannot be updated at all without moving its start into the future.
    /// </remarks>
    /// <param name="name">
    /// The replacement display name. Surrounding whitespace is trimmed before the length is checked.
    /// </param>
    /// <param name="description">
    /// The replacement description, trimmed when supplied, or <see langword="null"/> to clear it.
    /// </param>
    /// <param name="gameType">The replacement game the event is played with.</param>
    /// <param name="startDateTime">
    /// The replacement instant the event begins, which must be UTC and must not already have passed.
    /// </param>
    /// <param name="endDateTime">
    /// The replacement instant the event ends, which must be UTC and fall strictly after
    /// <paramref name="startDateTime"/>, or <see langword="null"/> to clear the scheduled end.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="gameType"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when either instant is not <see cref="DateTimeKind.Utc"/>.
    /// </exception>
    /// <exception cref="DomainException">
    /// Thrown when <paramref name="name"/> is <see langword="null"/>, empty, whitespace, or too long,
    /// when <paramref name="description"/> is too long, when <paramref name="startDateTime"/> is in the
    /// past, or when <paramref name="endDateTime"/> does not fall after
    /// <paramref name="startDateTime"/>. The message states the rule that was broken and is safe to
    /// report to the originator of the request.
    /// </exception>
    public void Update(
        string name,
        string? description,
        GameType gameType,
        DateTime startDateTime,
        DateTime? endDateTime)
    {
        ArgumentNullException.ThrowIfNull(gameType);

        name = ValidateAndNormalizeName(name);
        description = ValidateAndNormalizeDescription(description);

        ValidateSchedule(startDateTime, endDateTime);

        this.Name = name;
        this.Description = description;
        this.GameType = gameType;
        this.StartDateTime = startDateTime;
        this.EndDateTime = endDateTime;
    }

    /// <summary>
    /// Rebuilds an event from already-persisted state, applying no validation.
    /// </summary>
    /// <remarks>
    /// This is for persistence mapping only. Callers creating an event for the first time must use
    /// <see cref="Create"/>, which enforces the entity's invariants.
    /// </remarks>
    /// <param name="id">The stored primary key of the event.</param>
    /// <param name="publicId">The stored identifier of the event.</param>
    /// <param name="name">The stored display name of the event.</param>
    /// <param name="description">The stored description of the event, if any.</param>
    /// <param name="startDateTime">
    /// The stored instant the event begins, which the caller must already have marked as UTC.
    /// </param>
    /// <param name="endDateTime">
    /// The stored instant the event ends, if any, which the caller must already have marked as UTC.
    /// </param>
    /// <param name="gameType">The game type the stored event references, already rehydrated.</param>
    /// <param name="registrationLimit">The stored registration limit of the event.</param>
    /// <returns>The rehydrated event.</returns>
    public static Event Reconstitute(
        int id,
        Guid publicId,
        string name,
        string? description,
        DateTime startDateTime,
        DateTime? endDateTime,
        GameType gameType,
        int registrationLimit) =>
        new()
        {
            Id = id,
            PublicId = publicId,
            Name = name,
            Description = description,
            StartDateTime = startDateTime,
            EndDateTime = endDateTime,
            GameType = gameType,
            RegistrationLimit = registrationLimit
        };

    private static string ValidateAndNormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Event name is required.");
        }

        name = name.Trim();

        if (name.Length > MaxNameLength)
        {
            throw new DomainException($"Event name cannot exceed {MaxNameLength} characters.");
        }

        return name;
    }

    private static string? ValidateAndNormalizeDescription(string? description)
    {
        if (description is null)
        {
            return null;
        }

        description = description.Trim();

        if (description.Length > MaxDescriptionLength)
        {
            throw new DomainException($"Event description cannot exceed {MaxDescriptionLength} characters.");
        }

        return description;
    }

    private static void ValidateSchedule(DateTime startDateTime, DateTime? endDateTime)
    {
        // Comparing instants only means anything once both are known to be UTC, so the kind is checked
        // before any comparison is.
        if (startDateTime.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Event start date and time must be UTC.", nameof(startDateTime));
        }

        if (endDateTime.HasValue && endDateTime.Value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Event end date and time must be UTC.", nameof(endDateTime));
        }

        if (startDateTime < DateTime.UtcNow)
        {
            throw new DomainException("Event start date and time cannot be in the past.");
        }

        if (endDateTime.HasValue && startDateTime >= endDateTime.Value)
        {
            throw new DomainException("Event start date and time must be before the end date and time.");
        }
    }
}
