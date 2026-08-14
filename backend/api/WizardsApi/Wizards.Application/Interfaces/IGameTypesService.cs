using Wizards.Application.DTOs.Responses;

namespace Wizards.Application.Interfaces;

public interface IGameTypesService
{
    /// <summary>
    /// Retrieves a single game type by its identifier, together with the settings it exposes.
    /// </summary>
    /// <param name="gameTypeId">
    /// The identifier of the game type to read. Must not be <see cref="Guid.Empty"/>.
    /// </param>
    /// <param name="cancellationToken">Cancels the read before it completes.</param>
    /// <returns>
    /// The matching game type, or <see langword="null"/> when no game type carries that identifier.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="gameTypeId"/> is <see cref="Guid.Empty"/>.</exception>
    Task<GameTypeTemplateResponse?> GetGameType(Guid gameTypeId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves every registered game type, each together with the settings it exposes.
    /// </summary>
    /// <param name="cancellationToken">Cancels the read before it completes.</param>
    /// <returns>
    /// The registered game types, ordered by name, or an empty list when none are registered. Never
    /// <see langword="null"/>.
    /// </returns>
    Task<IReadOnlyList<GameTypeTemplateResponse>> GetGameTypes(CancellationToken cancellationToken);
}
