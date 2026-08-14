using Wizards.Application.DTOs.Responses;
using Wizards.Application.Interfaces;
using Wizards.Domain.Entities;
using Wizards.Domain.Interfaces.Repositories;

namespace Wizards.Application.Services;

internal sealed class GameTypesService(IGameTypesRepository gameTypesRepository) : IGameTypesService
{
    /// <inheritdoc />
    public async Task<GameTypeTemplateResponse?> GetGameType(
        Guid gameTypeId,
        CancellationToken cancellationToken)
    {
        if (gameTypeId == Guid.Empty)
        {
            throw new ArgumentException("Game type identifier cannot be empty.", nameof(gameTypeId));
        }

        GameType? gameType = await gameTypesRepository.GetGameTypeByPublicIdAsync(
            gameTypeId,
            cancellationToken);

        return gameType is null ? null : new GameTypeTemplateResponse(gameType);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GameTypeTemplateResponse>> GetGameTypes(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<GameType> gameTypes = await gameTypesRepository.GetGameTypesAsync(cancellationToken);

        return gameTypes.Select(gameType => new GameTypeTemplateResponse(gameType)).ToList();
    }
}
