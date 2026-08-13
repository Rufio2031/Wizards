using Wizards.Domain.Entities;
using Wizards.Domain.Models;

namespace Wizards.Domain.Interfaces.Repositories;

/// <summary>
/// Reads events and stages changes to them.
/// </summary>
/// <remarks>
/// Writes are staged only and are not durable until <see cref="IUnitOfWork.SaveChangesAsync"/> is
/// called on the same scope. Implementations are scoped and are not safe to share across threads or
/// concurrent requests.
/// </remarks>
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
    /// Retrieves a page of events, ordered by when they start and broken ties by primary key so the
    /// same window always yields the same events.
    /// </summary>
    /// <remarks>
    /// The window is applied by the store and is served by an index over the ordering, so a page reads
    /// and transfers the page rather than the collection. Establishing the total the page reports may
    /// cost a second read.
    /// </remarks>
    /// <param name="skip">The number of events to pass over before the page begins. Zero or greater.</param>
    /// <param name="take">The maximum number of events the page carries. Greater than zero.</param>
    /// <param name="cancellationToken">Cancels the read before it completes.</param>
    /// <returns>
    /// The page of events falling in the window, each with its <see cref="Event.GameType"/> populated.
    /// The page carries no events when the window falls past the end. Never <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="skip"/> is negative or <paramref name="take"/> is zero or negative.
    /// </exception>
    Task<EventPage> GetEventsAsync(int skip, int take, CancellationToken cancellationToken);

    /// <summary>
    /// Stages the insertion of a new event.
    /// </summary>
    /// <param name="eventEntity">The event to insert. Its game type must already exist.</param>
    /// <param name="cancellationToken">Cancels the staging before it completes.</param>
    /// <returns>A task that completes once the insertion is staged.</returns>
    Task AddEventAsync(Event eventEntity, CancellationToken cancellationToken);

    /// <summary>
    /// Stages the replacement of an existing event's stored state.
    /// </summary>
    /// <param name="eventEntity">
    /// The event to update, carrying the primary key it was loaded with.
    /// </param>
    /// <param name="cancellationToken">Cancels the staging before it completes.</param>
    /// <returns>A task that completes once the replacement is staged.</returns>
    Task UpdateEventAsync(Event eventEntity, CancellationToken cancellationToken);

    /// <summary>
    /// Stages the removal of an existing event.
    /// </summary>
    /// <param name="eventEntity">
    /// The event to delete, carrying the primary key it was loaded with.
    /// </param>
    /// <param name="cancellationToken">Cancels the staging before it completes.</param>
    /// <returns>A task that completes once the removal is staged.</returns>
    Task DeleteEventAsync(Event eventEntity, CancellationToken cancellationToken);
}
