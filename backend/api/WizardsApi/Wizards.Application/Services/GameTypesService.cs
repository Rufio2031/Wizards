using Wizards.Application.DTOs.Responses;
using Wizards.Application.Interfaces;
using Wizards.Domain.Entities;
using Wizards.Domain.Interfaces.Repositories;

namespace Wizards.Application.Services;

/// <summary>
/// Reads the registered game types and the settings they expose.
/// </summary>
/// <remarks>
/// Game types are reference data, so this reads and never writes. Instances are scoped alongside the
/// repository they read through and are not safe to share across threads or concurrent requests.
/// </remarks>
/// <param name="gameTypesRepository">The repository game types are read from.</param>
internal sealed class GameTypesService(IGameTypesRepository gameTypesRepository) : IGameTypesService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<GameTypeTemplateResponse>> GetGameTypes(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<GameType> gameTypes = await gameTypesRepository.GetGameTypesAsync(cancellationToken);

        return gameTypes.Select(gameType => new GameTypeTemplateResponse(gameType)).ToList();
    }
}
