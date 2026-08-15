using Microsoft.EntityFrameworkCore;

using Wizards.Domain.Interfaces.Repositories;
using Wizards.Infrastructure.Extensions;

namespace Wizards.Infrastructure.Persistence.Repositories;

internal sealed class EventRegistrationsRepository(AppDbContext dbContext) : IEventRegistrationsRepository
{
    /// <inheritdoc />
    public Task<int> CountRegistrationsAsync(
        Domain.Entities.Event @event,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);

        return dbContext.EventRegistrations
            .AsNoTracking()
            .CountAsync(registration => registration.EventId == @event.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Domain.Entities.EventRegistration>> GetRegistrationsAsync(
        Domain.Entities.Event @event,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);

        List<Records.EventRegistration> registrationRecords = await dbContext.EventRegistrations
            .AsNoTracking()
            .Where(registration => registration.EventId == @event.Id)
            .OrderBy(registration => registration.Id)
            .ToListAsync(cancellationToken);

        return registrationRecords
            .Select(registrationRecord => registrationRecord.ToEntity(@event))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<Domain.Entities.EventRegistration?> GetRegistrationByIdempotencyKeyAsync(
        Domain.Entities.Event @event,
        Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);

        Records.EventRegistration? registrationRecord = await dbContext.EventRegistrations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                registration => registration.EventId == @event.Id
                                && registration.IdempotencyKey == idempotencyKey,
                cancellationToken);

        return registrationRecord?.ToEntity(@event);
    }

    /// <inheritdoc />
    public Task AddRegistrationAsync(
        Domain.Entities.EventRegistration registration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(registration);
        cancellationToken.ThrowIfCancellationRequested();

        dbContext.EventRegistrations.Add(registration.ToRecord());

        return Task.CompletedTask;
    }
}
