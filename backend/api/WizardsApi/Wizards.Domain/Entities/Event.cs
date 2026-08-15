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
    /// The long-form description of the event, never empty, or null when the organizer supplied none.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>Where the event is held, as the organizer wrote it.</summary>
    public string Location { get; private set; } = string.Empty;

    /// <summary>The instant the event begins, in UTC.</summary>
    public DateTime StartDateTime { get; private set; }

    /// <summary>
    /// The instant the event ends, in UTC, falling strictly after <see cref="StartDateTime"/>.
    /// </summary>
    public DateTime EndDateTime { get; private set; }

    /// <summary>The game type of the event.</summary>
    public GameType GameType { get; private set; } = null!;

    /// <summary>
    /// The settings the organizer settled for this event, one per setting the game type exposed.
    /// </summary>
    public IReadOnlyList<EventGameTypeSelection> Selections => this.selections;

    /// <summary>The registration limit for the event.</summary>
    public int RegistrationLimit { get; private set; }

    /// <summary>
    /// Gets whether the event has begun, after which it accepts no further registrations. Read against
    /// the current instant, so it turns true on its own once <see cref="StartDateTime"/> passes.
    /// </summary>
    public bool IsRegistrationClosed => DateTime.UtcNow >= this.StartDateTime;

    private List<EventGameTypeSelection> selections = [];

    private Event() { }

    /// <summary>Creates an event that has never been persisted and assigns it a new identifier.</summary>
    /// <param name="name">The display name of the event, trimmed before its length is checked.</param>
    /// <param name="description">
    /// The long-form description of the event, trimmed when supplied, where null or whitespace leaves
    /// the event without one.
    /// </param>
    /// <param name="location">
    /// Where the event is held, required and trimmed before its length is checked.
    /// </param>
    /// <param name="gameType">The game the event is played with.</param>
    /// <param name="startDateTime">
    /// The instant the event begins, which must be UTC and must not already have passed.
    /// </param>
    /// <param name="endDateTime">
    /// The instant the event ends, which must be UTC and fall strictly after
    /// <paramref name="startDateTime"/>.
    /// </param>
    /// <param name="registrationLimit">
    /// How many players the event accepts, at least one and no more than
    /// <see cref="MaxRegistrationLimit"/>.
    /// </param>
    /// <param name="selections">The settings settled for the event, stored as given.</param>
    /// <returns>The new event, carrying its assigned identifier and no primary key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the game type is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when either instant is not UTC or a selection is null.
    /// </exception>
    /// <exception cref="DomainException">
    /// Thrown when any supplied detail breaks a rule about what makes a valid event.
    /// </exception>
    public static Event Create(
        string name,
        string? description,
        string location,
        GameType gameType,
        DateTime startDateTime,
        DateTime endDateTime,
        int registrationLimit,
        IEnumerable<EventGameTypeSelection>? selections = null)
    {
        ArgumentNullException.ThrowIfNull(gameType);

        name = ValidateAndNormalizeName(name);
        description = ValidateAndNormalizeDescription(description);
        location = ValidateAndNormalizeLocation(location);

        ValidateSchedule(startDateTime, endDateTime);

        ValidateRegistrationLimit(registrationLimit);

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
            RegistrationLimit = registrationLimit,
            selections = eventSelections
        };
    }

    /// <summary>Rebuilds an event from already-persisted state, applying no validation.</summary>
    /// <remarks>
    /// This is for persistence mapping only, and a new event must come from <see cref="Create"/>.
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

    /// <summary>Reports whether the event has taken every registration it accepts.</summary>
    /// <param name="registrationCount">How many players are registered for the event.</param>
    /// <returns>True when the event will accept no further registrations.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the registration count is negative.
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
            throw new DomainException("Event name is required.") { Key = nameof(Name) };
        }

        name = name.Trim();

        if (name.Length > MaxNameLength)
        {
            throw new DomainException($"Event name cannot exceed {MaxNameLength} characters.")
            {
                Key = nameof(Name)
            };
        }

        return name;
    }

    private static string? ValidateAndNormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        description = description.Trim();

        if (description.Length > MaxDescriptionLength)
        {
            throw new DomainException($"Event description cannot exceed {MaxDescriptionLength} characters.")
            {
                Key = nameof(Description)
            };
        }

        return description;
    }

    private static string ValidateAndNormalizeLocation(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new DomainException("Event location is required.") { Key = nameof(Location) };
        }

        location = location.Trim();

        if (location.Length > MaxLocationLength)
        {
            throw new DomainException($"Event location cannot exceed {MaxLocationLength} characters.")
            {
                Key = nameof(Location)
            };
        }

        return location;
    }

    private static void ValidateRegistrationLimit(int registrationLimit)
    {
        if (registrationLimit < 1)
        {
            throw new DomainException("An event must accept at least one player.")
            {
                Key = nameof(RegistrationLimit)
            };
        }

        if (registrationLimit > MaxRegistrationLimit)
        {
            throw new DomainException(
                $"An event cannot accept more than {MaxRegistrationLimit} players.")
            {
                Key = nameof(RegistrationLimit)
            };
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
            if (!seenKeys.Add(selection.GameTypeSetting.Key))
            {
                throw new DomainException(
                    $"An event cannot carry two values for the '{selection.GameTypeSetting.Key}' setting.");
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
            throw new DomainException("Event start date and time cannot be in the past.")
            {
                Key = nameof(StartDateTime)
            };
        }

        if (startDateTime >= endDateTime)
        {
            throw new DomainException("Event start date and time must be before the end date and time.")
            {
                Key = nameof(EndDateTime)
            };
        }
    }
}
