using Wizards.Domain.Exceptions;

namespace Wizards.Domain.Entities;

/// <summary>
/// One player's registration for an event.
/// </summary>
/// <remarks>
/// A registration is held against the event it was taken for and cannot exist without one. Whether
/// the event will take one at all is a rule the event states, resolved against
/// <see cref="Event.IsFull"/> and <see cref="Event.IsRegistrationClosed"/>.
/// <para>
/// It carries no identifier of its own, because nothing addresses a single registration yet. The
/// store keys the row it is written to, and one is added here once there is something to address it
/// with.
/// </para>
/// </remarks>
public class EventRegistration
{
    /// <summary>The maximum length of the name a player registers under.</summary>
    public const int MaxNameLength = 100;

    /// <summary>Gets the event the player registered for.</summary>
    public Event Event { get; private set; } = null!;

    /// <summary>Gets the name the player registered under.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the key the player supplied to identify this registration attempt.</summary>
    public Guid IdempotencyKey { get; private set; }

    private EventRegistration() { }

    /// <summary>
    /// Creates a registration that has never been persisted.
    /// </summary>
    /// <param name="event">The event the player is registering for.</param>
    /// <param name="name">
    /// The name the player registers under. Surrounding whitespace is trimmed before the length is
    /// checked.
    /// </param>
    /// <param name="idempotencyKey">
    /// The key identifying this registration attempt, unique within the event.
    /// </param>
    /// <returns>The new registration.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="event"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="DomainException">
    /// Thrown when <paramref name="name"/> is <see langword="null"/>, empty, whitespace, or too long,
    /// or when <paramref name="idempotencyKey"/> is <see cref="Guid.Empty"/>. The failure names the
    /// property the rule is about, and the message states the rule that was broken and is safe to
    /// report to the originator of the request.
    /// </exception>
    public static EventRegistration Create(Event @event, string name, Guid idempotencyKey)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (idempotencyKey == Guid.Empty)
        {
            throw new DomainException("An idempotency key is required to register.")
            {
                Key = nameof(IdempotencyKey)
            };
        }

        return new()
        {
            Event = @event,
            Name = ValidateAndNormalizeName(name),
            IdempotencyKey = idempotencyKey
        };
    }

    /// <summary>
    /// Rebuilds a registration from already-persisted state, applying no validation.
    /// </summary>
    /// <remarks>
    /// This is for persistence mapping only. Callers taking a registration for the first time must use
    /// <see cref="Create"/>, which enforces the entity's invariants.
    /// </remarks>
    /// <param name="event">The stored event the registration is held against, already rehydrated.</param>
    /// <param name="name">The stored name the player registered under.</param>
    /// <param name="idempotencyKey">The stored key the registration was taken under.</param>
    /// <returns>The rehydrated registration.</returns>
    public static EventRegistration Reconstitute(Event @event, string name, Guid idempotencyKey) =>
        new()
        {
            Event = @event,
            Name = name,
            IdempotencyKey = idempotencyKey
        };

    private static string ValidateAndNormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("A name is required to register.") { Key = nameof(Name) };
        }

        name = name.Trim();

        if (name.Length > MaxNameLength)
        {
            throw new DomainException($"A name cannot exceed {MaxNameLength} characters.")
            {
                Key = nameof(Name)
            };
        }

        return name;
    }
}
