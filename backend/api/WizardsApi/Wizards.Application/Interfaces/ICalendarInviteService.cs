using Wizards.Application.Models;

namespace Wizards.Application.Interfaces;

/// <summary>
/// Serves the calendar invites events are offered as.
/// </summary>
public interface ICalendarInviteService
{
    /// <summary>
    /// Retrieves an event and builds its calendar invite.
    /// </summary>
    /// <remarks>
    /// The invite is serialized rather than joined from text, so an event's own details cannot be read
    /// as calendar syntax. The identifier it carries is derived from the event's, so importing the same
    /// invite twice updates one calendar entry rather than adding a second. Nothing is stored, so an
    /// invite always states the event as it stands.
    /// </remarks>
    /// <param name="eventId">
    /// The identifier of the event to describe. Must not be <see cref="Guid.Empty"/>.
    /// </param>
    /// <param name="cancellationToken">Cancels the read before it completes.</param>
    /// <returns>
    /// The invite, carrying the name to offer it under and the media type to serve it as, or
    /// <see langword="null"/> when no event carries that identifier.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="eventId"/> is <see cref="Guid.Empty"/>.
    /// </exception>
    Task<CalendarInvite?> GetInvite(Guid eventId, CancellationToken cancellationToken);
}
