namespace Wizards.Domain.Interfaces.Repositories;

/// <summary>
/// Commits, as one atomic unit, every change staged by every repository sharing the current scope.
/// </summary>
/// <remarks>
/// Repositories stage work but never persist it, so a change is not durable until this is called.
/// Implementations are scoped alongside the repositories they commit for and are not safe to share
/// across threads or concurrent requests.
/// </remarks>
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
