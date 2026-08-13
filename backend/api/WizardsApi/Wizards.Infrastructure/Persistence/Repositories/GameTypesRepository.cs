using Microsoft.EntityFrameworkCore;

using Wizards.Domain.Interfaces.Repositories;
using Wizards.Infrastructure.Extensions;

namespace Wizards.Infrastructure.Persistence.Repositories;

/// <summary>
/// Reads the game types registered in the Wizards database.
/// </summary>
/// <param name="dbContext">The context to read against.</param>
internal sealed class GameTypesRepository(AppDbContext dbContext) : IGameTypesRepository
{
    /// <inheritdoc />
    public async Task<Domain.Entities.GameType?> GetGameTypeByNameAsync(
        string name,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // The name column is declared NOCASE, so an ordinary equality comparison is both
        // case-insensitive and able to seek the unique index on it.
        Records.GameType? gameTypeRecord = await dbContext.GameTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(gameType => gameType.Name == name, cancellationToken);

        return gameTypeRecord?.ToEntity();
    }
}
