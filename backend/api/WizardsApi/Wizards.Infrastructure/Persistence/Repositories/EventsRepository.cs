using Microsoft.EntityFrameworkCore;

using Wizards.Domain.Interfaces.Repositories;
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
            .FirstOrDefaultAsync(storedEvent => storedEvent.PublicId == publicId, cancellationToken);

        return eventRecord?.ToEntity();
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
