using Wizards.Domain.Interfaces.Repositories;

namespace Wizards.Infrastructure.Persistence;

/// <summary>
/// Commits the changes tracked by the scope's <see cref="AppDbContext"/>.
/// </summary>
/// <param name="dbContext">
/// The context whose tracked changes are committed. Must be the same scoped instance the
/// repositories stage their work against, or their changes will not be seen.
/// </param>
internal sealed class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
