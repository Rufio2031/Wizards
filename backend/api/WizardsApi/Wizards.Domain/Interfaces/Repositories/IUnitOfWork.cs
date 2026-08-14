namespace Wizards.Domain.Interfaces.Repositories;

public interface IUnitOfWork
{
    /// <summary>
    /// Persists every staged change, or persists none of them if any one of them fails.
    /// </summary>
    /// <param name="cancellationToken">
    /// Cancels the commit before it completes. Cancellation is only honored up to the point the
    /// commit is handed to the database, so a canceled call may still have persisted its changes.
    /// </param>
    /// <returns>A task that completes once the staged changes are durable.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
