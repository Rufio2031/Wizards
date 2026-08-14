using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using Wizards.Domain.Enums;
using Wizards.Domain.Interfaces.Repositories;
using Wizards.Domain.Models;
using Wizards.Infrastructure.Extensions;

namespace Wizards.Infrastructure.Persistence.Repositories;

/// <summary>
/// Reads events from the Wizards database and stages changes to them.
/// </summary>
/// <remarks>
/// Reads are untracked, so a returned entity is a detached snapshot and later edits to it are only
/// persisted by handing it back to one of the staging methods.
/// </remarks>
/// <param name="dbContext">
/// The context to read and stage against. Must be the same scoped instance the unit of work commits,
/// or staged work will not be persisted.
/// </param>
internal sealed class EventsRepository(AppDbContext dbContext) : IEventsRepository
{
    /// <inheritdoc />
    public async Task<Domain.Entities.Event?> GetEventByPublicIdAsync(
        Guid publicId,
        CancellationToken cancellationToken)
    {
        Records.Event? eventRecord = await dbContext.Events
            .AsNoTracking()
            .Include(storedEvent => storedEvent.GameType)
            .Include(storedEvent => storedEvent.Selections)
            .FirstOrDefaultAsync(storedEvent => storedEvent.PublicId == publicId, cancellationToken);

        return eventRecord?.ToEntity();
    }

    /// <inheritdoc />
    public async Task<EventPage> GetEventsAsync(
        EventQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfNegative(query.Skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(query.Take);

        Expression<Func<Records.Event, DateTime>> sortKey = query.SortField switch
        {
            EventSortField.StartDateTime => storedEvent => storedEvent.StartDateTime,
            _ => throw new ArgumentOutOfRangeException(
                nameof(query),
                query.SortField,
                "Events cannot be ordered by that field.")
        };

        IQueryable<Records.Event> events = dbContext.Events
            .AsNoTracking();

        if (query.StartingOnOrAfter is DateTime startingOnOrAfter)
        {
            events = events.Where(storedEvent => storedEvent.StartDateTime >= startingOnOrAfter);
        }

        if (query.StartingBefore is DateTime startingBefore)
        {
            events = events.Where(storedEvent => storedEvent.StartDateTime < startingBefore);
        }

        int totalCount = await events.CountAsync(cancellationToken);

        IOrderedQueryable<Records.Event> orderedEvents =
            query.SortDirection == SortDirection.Descending
                ? events.OrderByDescending(sortKey).ThenByDescending(storedEvent => storedEvent.Id)
                : events.OrderBy(sortKey).ThenBy(storedEvent => storedEvent.Id);

        List<Records.Event> eventRecords = await orderedEvents
            .Include(storedEvent => storedEvent.GameType)
            .Include(storedEvent => storedEvent.Selections)
            .Skip(query.Skip)
            .Take(query.Take)
            .ToListAsync(cancellationToken);

        return new EventPage(
            eventRecords.Select(eventRecord => eventRecord.ToEntity()).ToList(),
            totalCount);
    }

    /// <inheritdoc />
    public Task AddEventAsync(Domain.Entities.Event eventEntity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(eventEntity);
        cancellationToken.ThrowIfCancellationRequested();

        dbContext.Events.Add(eventEntity.ToRecord());

        return Task.CompletedTask;
    }
}
