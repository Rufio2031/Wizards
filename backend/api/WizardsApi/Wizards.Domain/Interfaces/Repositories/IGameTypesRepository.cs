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
    /// Retrieves a single game type by its display name.
    /// </summary>
    /// <param name="name">
    /// The display name to match. Matching ignores case, so a caller does not need to reproduce the
    /// registered casing.
    /// </param>
    /// <param name="cancellationToken">Cancels the read before it completes.</param>
    /// <returns>
    /// The matching game type, or <see langword="null"/> when no game type is registered under that
    /// name.
    /// </returns>
    Task<GameType?> GetGameTypeByNameAsync(string name, CancellationToken cancellationToken);
}
