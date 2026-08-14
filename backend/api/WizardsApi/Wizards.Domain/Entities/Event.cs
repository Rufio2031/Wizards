using Wizards.Domain.Exceptions;

namespace Wizards.Domain.Entities;

public class Event
{
    /// <summary>The maximum length of an event's name.</summary>
    public const int MaxNameLength = 100;

    /// <summary>The maximum length of an event's description.</summary>
    public const int MaxDescriptionLength = 2000;

    /// <summary>The maximum length of an event's location.</summary>
    public const int MaxLocationLength = 200;

    /// <summary>The most players an event may accept.</summary>
    public const int MaxRegistrationLimit = 30;

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
    /// Where the event is held, as the organizer wrote it. Stated on the calendar invite, so an event
    /// always carries one.
    /// </summary>
    public string Location { get; private set; } = string.Empty;

    /// <summary>
    /// The instant the event begins, in UTC. Always carries <see cref="DateTimeKind.Utc"/>.
    /// </summary>
    public DateTime StartDateTime { get; private set; }

    /// <summary>
    /// The instant the event ends, in UTC. Always carries <see cref="DateTimeKind.Utc"/> and falls
    /// strictly after <see cref="StartDateTime"/>.
    /// </summary>
    public DateTime EndDateTime { get; private set; }

    /// <summary>The game type of the event.</summary>
    public GameType GameType { get; private set; } = null!;

    /// <summary>
    /// The settings the organizer settled for this event, one per setting the game type exposed when
    /// the event was created.
    /// </summary>
    public IReadOnlyList<EventGameTypeSelection> Selections => this.selections;

    /// <summary>The registration limit for the event.</summary>
    public int RegistrationLimit { get; private set; }

    private List<EventGameTypeSelection> selections = [];

    private Event() { }

    /// <summary>
    /// Creates an event that has never been persisted, assigning it a new identifier.
    /// </summary>
    /// <param name="name">
    /// The display name of the event. Surrounding whitespace is trimmed before the length is checked.
    /// </param>
    /// <param name="description">
    /// The long-form description of the event, trimmed when supplied, or <see langword="null"/> for an
    /// event without one.
    /// </param>
    /// <param name="location">
    /// Where the event is held. Required, and trimmed before the length is checked.
    /// </param>
    /// <param name="gameType">The game the event is played with.</param>
    /// <param name="startDateTime">
    /// The instant the event begins, which must be UTC and must not already have passed.
    /// </param>
    /// <param name="endDateTime">
    /// The instant the event ends, which must be UTC and fall strictly after
    /// <paramref name="startDateTime"/>.
    /// </param>
    /// <returns>The new event, carrying its assigned identifier and no primary key.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="gameType"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when either instant is not <see cref="DateTimeKind.Utc"/>, or when
    /// <paramref name="selections"/> contains a null entry.
    /// </exception>
    /// <param name="registrationLimit">
    /// How many players the event accepts, at least one and no more than
    /// <see cref="MaxRegistrationLimit"/>, or <see langword="null"/> for an event that accepts as many
    /// as one can.
    /// </param>
    /// <param name="selections">
    /// The settings settled for the event, stored as given. Whether they satisfy the game type is a
    /// rule the game type states, so the caller resolves it and calls
    /// <see cref="GameType.Validate"/> before reaching here.
    /// </param>
    /// <exception cref="DomainException">
    /// Thrown when <paramref name="name"/> is <see langword="null"/>, empty, whitespace, or too long,
    /// when <paramref name="description"/> is too long, when <paramref name="location"/> is
    /// <see langword="null"/>, empty, whitespace, or too long, when <paramref name="startDateTime"/> is
    /// in the past, when <paramref name="endDateTime"/> does not fall after
    /// <paramref name="startDateTime"/>, when <paramref name="registrationLimit"/> falls outside the
    /// allowed range, or when <paramref name="selections"/> carry two values for the same setting. The
    /// message states the rule that was broken and is safe to report to the originator of the request.
    /// </exception>
    public static Event Create(
        string name,
        string? description,
        string location,
        GameType gameType,
        DateTime startDateTime,
        DateTime endDateTime,
        int? registrationLimit = null,
        IEnumerable<EventGameTypeSelection>? selections = null)
    {
        ArgumentNullException.ThrowIfNull(gameType);

        name = ValidateAndNormalizeName(name);
        description = ValidateAndNormalizeDescription(description);
        location = ValidateAndNormalizeLocation(location);

        ValidateSchedule(startDateTime, endDateTime);

        int limit = registrationLimit ?? MaxRegistrationLimit;

        ValidateRegistrationLimit(limit);

        List<EventGameTypeSelection> eventSelections = selections?.ToList() ?? [];

        ValidateSelections(eventSelections);

        return new()
        {
            PublicId = Guid.CreateVersion7(),
            Name = name,
            Description = description,
            Location = location,
            GameType = gameType,
            StartDateTime = startDateTime,
            EndDateTime = endDateTime,
            RegistrationLimit = limit,
            selections = eventSelections
        };
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
    /// <param name="location">The stored location of the event.</param>
    /// <param name="startDateTime">
    /// The stored instant the event begins, which the caller must already have marked as UTC.
    /// </param>
    /// <param name="endDateTime">
    /// The stored instant the event ends, which the caller must already have marked as UTC.
    /// </param>
    /// <param name="gameType">The game type the stored event references, already rehydrated.</param>
    /// <param name="registrationLimit">The stored registration limit of the event.</param>
    /// <param name="selections">The stored settings of the event, already rehydrated.</param>
    /// <returns>The rehydrated event.</returns>
    public static Event Reconstitute(
        int id,
        Guid publicId,
        string name,
        string? description,
        string location,
        DateTime startDateTime,
        DateTime endDateTime,
        GameType gameType,
        int registrationLimit,
        IEnumerable<EventGameTypeSelection>? selections = null) =>
        new()
        {
            Id = id,
            PublicId = publicId,
            Name = name,
            Description = description,
            Location = location,
            StartDateTime = startDateTime,
            EndDateTime = endDateTime,
            GameType = gameType,
            RegistrationLimit = registrationLimit,
            selections = selections?.ToList() ?? []
        };

    /// <summary>
    /// Reports whether the event has taken every registration it accepts.
    /// </summary>
    /// <remarks>
    /// The count is supplied rather than held, because an event is read without its registrations and
    /// the number of them is only ever true for the instant it was counted. A caller acting on the
    /// answer races anything registering alongside it, so the store enforces the same limit as the
    /// last word.
    /// </remarks>
    /// <param name="registrationCount">How many players are registered for the event.</param>
    /// <returns>
    /// <see langword="true"/> when the event will accept no further registrations.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="registrationCount"/> is negative.
    /// </exception>
    public bool IsFull(int registrationCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(registrationCount);

        return registrationCount >= this.RegistrationLimit;
    }

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

    private static string ValidateAndNormalizeLocation(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new DomainException("Event location is required.");
        }

        location = location.Trim();

        if (location.Length > MaxLocationLength)
        {
            throw new DomainException($"Event location cannot exceed {MaxLocationLength} characters.");
        }

        return location;
    }

    private static void ValidateRegistrationLimit(int registrationLimit)
    {
        if (registrationLimit < 1)
        {
            throw new DomainException("An event must accept at least one player.");
        }

        if (registrationLimit > MaxRegistrationLimit)
        {
            throw new DomainException(
                $"An event cannot accept more than {MaxRegistrationLimit} players.");
        }
    }

    private static void ValidateSelections(List<EventGameTypeSelection> selections)
    {
        if (selections.Any(selection => selection is null))
        {
            throw new ArgumentException("A selection cannot be null.", nameof(selections));
        }

        HashSet<string> seenKeys = new(StringComparer.OrdinalIgnoreCase);

        foreach (EventGameTypeSelection selection in selections)
        {
            if (!seenKeys.Add(selection.Key))
            {
                throw new DomainException(
                    $"An event cannot carry two values for the '{selection.Key}' setting.")
                {
                    Key = selection.Key
                };
            }
        }
    }

    private static void ValidateSchedule(DateTime startDateTime, DateTime endDateTime)
    {
        // Comparing instants only means anything once both are known to be UTC, so the kind is checked
        // before any comparison is.
        if (startDateTime.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Event start date and time must be UTC.", nameof(startDateTime));
        }

        if (endDateTime.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Event end date and time must be UTC.", nameof(endDateTime));
        }

        if (startDateTime < DateTime.UtcNow)
        {
            throw new DomainException("Event start date and time cannot be in the past.");
        }

        if (startDateTime >= endDateTime)
        {
            throw new DomainException("Event start date and time must be before the end date and time.");
        }
    }
}
