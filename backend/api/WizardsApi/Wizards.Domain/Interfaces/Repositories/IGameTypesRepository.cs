using Wizards.Domain.Entities;

namespace Wizards.Domain.Interfaces.Repositories;

/// <summary>
/// Reads the registered game types.
/// </summary>
/// <remarks>
/// Game types are reference data. They are resolved by callers, never created by them, so this
/// exposes no writes. Implementations are scoped and are not safe to share across threads or
/// concurrent requests.
/// </remarks>
public interface IGameTypesRepository
{
    /// <summary>
    /// Retrieves a single game type by its identifier, together with the settings it exposes.
    /// </summary>
    /// <param name="publicId">The identifier of the game type to read.</param>
    /// <param name="cancellationToken">Cancels the read before it completes.</param>
    /// <returns>
    /// The matching game type, or <see langword="null"/> when no game type carries that identifier.
    /// </returns>
    Task<GameType?> GetGameTypeByPublicIdAsync(Guid publicId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves every registered game type, each together with the settings it exposes.
    /// </summary>
    /// <param name="cancellationToken">Cancels the read before it completes.</param>
    /// <returns>
    /// The registered game types, ordered by name, or an empty list when none are registered.
    /// </returns>
    Task<IReadOnlyList<GameType>> GetGameTypesAsync(CancellationToken cancellationToken);
}
