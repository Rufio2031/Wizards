using Microsoft.AspNetCore.Mvc;

using Wizards.Application.DTOs.Responses;
using Wizards.Application.Interfaces;

namespace Wizards.Api.Controllers;

/// <summary>
/// Serves the game types resource.
/// </summary>
/// <param name="gameTypesService">
/// The service backing every action on this controller. Supplied by dependency injection; never
/// <see langword="null"/>.
/// </param>
[ApiController]
[Route("gametypes")]
[Produces("application/json")]
public class GameTypesController(IGameTypesService gameTypesService) : ControllerBase
{
    /// <summary>
    /// Retrieves a single game type by its identifier, together with the settings it exposes.
    /// </summary>
    /// <remarks>
    /// Cached for 60 seconds on the same terms as the collection, so a client that reads one game type
    /// sees an edit to it no sooner than a client that reads them all.
    /// </remarks>
    /// <param name="gameTypeId">The identifier of the game type to retrieve.</param>
    /// <param name="cancellationToken">Cancels the request before it completes.</param>
    /// <returns>The matching game type.</returns>
    /// <response code="200">The game type was retrieved.</response>
    /// <response code="404">
    /// No game type carries the supplied identifier. An empty identifier is never assigned to a game
    /// type and is reported the same way.
    /// </response>
    [HttpGet("{gameTypeId:guid}", Name = nameof(GetGameType))]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType<GameTypeTemplateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameTypeTemplateResponse>> GetGameType(
        Guid gameTypeId,
        CancellationToken cancellationToken)
    {
        if (gameTypeId == Guid.Empty)
        {
            return this.NotFound();
        }

        GameTypeTemplateResponse? gameType = await gameTypesService.GetGameType(
            gameTypeId,
            cancellationToken);

        if (gameType is null)
        {
            return this.NotFound();
        }

        return this.Ok(gameType);
    }

    /// <summary>
    /// Retrieves every registered game type, each together with the settings it exposes.
    /// </summary>
    /// <remarks>
    /// The game types are cached for 60 seconds, so repeated requests within that time window may return the same result even if the underlying data has changed.
    /// This is a rudimentary cache that is not invalidated when the underlying data changes.
    /// </remarks>
    /// <param name="cancellationToken">Cancels the request before it completes.</param>
    /// <returns>The registered game types, ordered by name.</returns>
    /// <response code="200">The game types were retrieved.</response>
    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType<IReadOnlyList<GameTypeTemplateResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GameTypeTemplateResponse>>> GetGameTypes(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<GameTypeTemplateResponse> gameTypes =
            await gameTypesService.GetGameTypes(cancellationToken);

        return this.Ok(gameTypes);
    }
}
