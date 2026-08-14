using Wizards.Domain.Entities;

namespace Wizards.Application.DTOs.Responses;

/// <summary>
/// An event as returned to API callers.
/// </summary>
/// <param name="EventId">The identifier assigned to the event when it was created.</param>
/// <param name="Name">The event's display name.</param>
/// <param name="Description">
/// The event's long-form description, or <see langword="null"/> when the organizer has not supplied one.
/// </param>
/// <param name="StartDateTime">
/// The instant the event begins, always in UTC and always serialized with a trailing <c>Z</c>.
/// </param>
/// <param name="EndDateTime">
/// The instant the event ends, always in UTC and always serialized with a trailing <c>Z</c>.
/// </param>
/// <param name="GameType">The type of game the event is for.</param>
/// <param name="Selections">
/// The settings the organizer settled for the event, keyed by the setting's key. Carries one entry
/// per setting the game type exposed when the event was created, including the ones left at their
/// default.
/// </param>
public record EventResponse(
    Guid EventId,
    string Name,
    string? Description,
    DateTime StartDateTime,
    DateTime EndDateTime,
    GameTypeResponse GameType,
    IReadOnlyDictionary<string, string> Selections)
{
    /// <summary>
    /// Projects an event onto the shape returned to API callers.
    /// </summary>
    /// <param name="event">
    /// The event to project. Must not be <see langword="null"/>, and must have been loaded with its
    /// <see cref="Event.GameType"/> populated.
    /// </param>
    public EventResponse(Event @event)
        : this(
            @event.PublicId,
            @event.Name,
            @event.Description,
            @event.StartDateTime,
            @event.EndDateTime,
            new GameTypeResponse(@event.GameType),
            @event.Selections.ToDictionary(selection => selection.Key, selection => selection.Value))
    {
    }
}
