using Wizards.Domain.Entities;

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
    /// Stages the insertion of a new event.
    /// </summary>
    /// <param name="eventEntity">The event to insert. Its game type must already exist.</param>
    /// <param name="cancellationToken">Cancels the staging before it completes.</param>
    /// <returns>A task that completes once the insertion is staged.</returns>
    Task AddEventAsync(Event eventEntity, CancellationToken cancellationToken);
}
