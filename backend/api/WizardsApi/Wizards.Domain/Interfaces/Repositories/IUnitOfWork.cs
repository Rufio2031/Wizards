using Wizards.Domain.Exceptions;

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
    /// <exception cref="StoreRuleViolationException">
    /// Thrown when the store refuses the commit because a rule it enforces itself, such as the number
    /// of registrations one event accepts, is broken. Nothing staged is persisted.
    /// </exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
