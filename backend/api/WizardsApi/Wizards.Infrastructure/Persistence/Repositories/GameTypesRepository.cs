using Microsoft.EntityFrameworkCore;

using Wizards.Domain.Interfaces.Repositories;
using Wizards.Infrastructure.Extensions;

namespace Wizards.Infrastructure.Persistence.Repositories;

internal sealed class GameTypesRepository(AppDbContext dbContext) : IGameTypesRepository
{
    /// <inheritdoc />
    public async Task<Domain.Entities.GameType?> GetGameTypeByPublicIdAsync(
        Guid publicId,
        CancellationToken cancellationToken)
    {
        Records.GameType? gameTypeRecord = await dbContext.GameTypes
            .AsNoTracking()
            .Include(gameType => gameType.Settings)
                .ThenInclude(setting => setting.Options)
            .FirstOrDefaultAsync(gameType => gameType.PublicId == publicId, cancellationToken);

        return gameTypeRecord?.ToEntity();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Domain.Entities.GameType>> GetGameTypesAsync(
        CancellationToken cancellationToken)
    {
        List<Records.GameType> gameTypeRecords = await dbContext.GameTypes
            .AsNoTracking()
            .Include(gameType => gameType.Settings)
                .ThenInclude(setting => setting.Options)
            .OrderBy(gameType => gameType.Name)
            .ToListAsync(cancellationToken);

        return gameTypeRecords.Select(gameTypeRecord => gameTypeRecord.ToEntity()).ToList();
    }
}
