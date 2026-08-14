using Wizards.Domain.Entities;
using Wizards.Domain.Enums;
using Wizards.Domain.Models;

namespace Wizards.Domain.Interfaces.Repositories;

public interface IEventsRepository
{
    /// <summary>
    /// Retrieves a single event by its identifier.
    /// </summary>
    /// <param name="publicId">The identifier of the event to retrieve.</param>
    /// <param name="cancellationToken">Cancels the read before it completes.</param>
    /// <returns>
    /// The matching event with its <see cref="Event.GameType"/> populated, or <see langword="null"/>
    /// when no event carries that identifier.
    /// </returns>
    Task<Event?> GetEventByPublicIdAsync(Guid publicId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a page of the events falling in a date range, ordered as the query asks.
    /// </summary>
    /// <remarks>
    /// Ties are broken by primary key in the requested direction, so the same window always yields the
    /// same events and neighbouring pages neither repeat nor skip one.
    /// </remarks>
    /// <param name="query">
    /// The window, ordering and date range to read over, with bounds already in UTC as
    /// <see cref="EventQuery"/> requires.
    /// </param>
    /// <param name="cancellationToken">Cancels the read before it completes.</param>
    /// <returns>
    /// The page of events falling in the window, each with its <see cref="Event.GameType"/> populated,
    /// carrying no events when the window falls past the end of the range.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="query"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <see cref="EventQuery.Skip"/> is negative, <see cref="EventQuery.Take"/> is zero or
    /// negative, or <see cref="EventQuery.SortField"/> names a field the implementation cannot order
    /// by, the last guarding a member added to <see cref="EventSortField"/> and left unmapped, and so
    /// unreachable from a request, whose sort field is checked at the API boundary.
    /// </exception>
    Task<EventPage> GetEventsAsync(EventQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Stages the insertion of a new event.
    /// </summary>
    /// <param name="eventEntity">The event to insert. Its game type must already exist.</param>
    /// <param name="cancellationToken">Cancels the staging before it completes.</param>
    /// <returns>A task that completes once the insertion is staged.</returns>
    Task AddEventAsync(Event eventEntity, CancellationToken cancellationToken);
}
