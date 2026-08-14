using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using Wizards.Domain.Exceptions;
using Wizards.Domain.Interfaces.Repositories;

namespace Wizards.Infrastructure.Persistence;

internal sealed class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    /// <summary>
    /// The extended result SQLite reports when a trigger aborts a statement, which is how the schema
    /// enforces the rules that span rows and cannot be stated as a column constraint.
    /// </summary>
    private const int SqliteConstraintTrigger = 1811;

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqliteException
                  {
                      SqliteExtendedErrorCode: SqliteConstraintTrigger
                  })
        {
            throw new StoreRuleViolationException(
                "The database refused the commit because a rule it enforces was broken.",
                exception);
        }
    }
}
