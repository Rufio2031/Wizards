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

    /// <summary>
    /// The extended result SQLite reports when a write would duplicate the values a unique index
    /// covers.
    /// </summary>
    private const int SqliteConstraintUnique = 2067;

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
        catch (DbUpdateException exception)
            when (exception.InnerException is SqliteException
                  {
                      SqliteExtendedErrorCode: SqliteConstraintUnique
                  })
        {
            throw new StoreUniquenessViolationException(
                "The database refused the commit because a row already holds the values a unique constraint covers.",
                exception);
        }
    }
}
