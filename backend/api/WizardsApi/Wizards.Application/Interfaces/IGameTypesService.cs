using Wizards.Application.DTOs.Responses;

namespace Wizards.Application.Interfaces;

/// <summary>
/// Reads the registered game types and the settings they expose.
/// </summary>
public interface IGameTypesService
{
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
