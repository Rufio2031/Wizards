using Wizards.Domain.Exceptions;

namespace Wizards.Domain.Entities;

/// <summary>
/// One player's registration for an event.
/// </summary>
/// <remarks>
/// A registration is held against the event it was taken for and cannot exist without one. Whether
/// the event still has room is a rule the event states, so the caller resolves it against
/// <see cref="Event.IsFull"/> before creating one.
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

    private EventRegistration() { }

    /// <summary>
    /// Creates a registration that has never been persisted.
    /// </summary>
    /// <param name="event">The event the player is registering for.</param>
    /// <param name="name">
    /// The name the player registers under. Surrounding whitespace is trimmed before the length is
    /// checked.
    /// </param>
    /// <returns>The new registration.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="event"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="DomainException">
    /// Thrown when <paramref name="name"/> is <see langword="null"/>, empty, whitespace, or too long.
    /// The failure names <see cref="Name"/> as the thing the rule is about, and the message states the
    /// rule that was broken and is safe to report to the originator of the request.
    /// </exception>
    public static EventRegistration Create(Event @event, string name)
    {
        ArgumentNullException.ThrowIfNull(@event);

        return new()
        {
            Event = @event,
            Name = ValidateAndNormalizeName(name)
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
    /// <returns>The rehydrated registration.</returns>
    public static EventRegistration Reconstitute(Event @event, string name) =>
        new()
        {
            Event = @event,
            Name = name
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
