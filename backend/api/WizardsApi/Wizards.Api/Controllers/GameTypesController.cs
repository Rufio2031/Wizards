using Microsoft.AspNetCore.Mvc;

using Wizards.Api.Filters;
using Wizards.Application.DTOs.Responses;
using Wizards.Application.Interfaces;

namespace Wizards.Api.Controllers;

[ApiController]
[Route("gametypes")]
public class GameTypesController(IGameTypesService gameTypesService) : ControllerBase
{
    /// <summary>
    /// Retrieves a single game type by its identifier, together with the settings it exposes.
    /// </summary>
    /// <remarks>A successful response is marked cacheable by any cache for 60 seconds.</remarks>
    /// <param name="gameTypeId">The identifier of the game type to retrieve.</param>
    /// <param name="cancellationToken">Cancels the request before it completes.</param>
    /// <returns>The matching game type.</returns>
    /// <response code="200">The game type was retrieved.</response>
    /// <response code="404">
    /// No game type carries the supplied identifier. An empty identifier is never assigned to a game
    /// type and is reported the same way.
    /// </response>
    [HttpGet("{gameTypeId:guid}", Name = nameof(GetGameType))]
    [SuccessResponseCache(durationSeconds: 60)]
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
    /// <remarks>A successful response is marked cacheable by any cache for 60 seconds.</remarks>
    /// <param name="cancellationToken">Cancels the request before it completes.</param>
    /// <returns>The registered game types, ordered by name.</returns>
    /// <response code="200">The game types were retrieved.</response>
    [HttpGet]
    [SuccessResponseCache(durationSeconds: 60)]
    [ProducesResponseType<IReadOnlyList<GameTypeTemplateResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GameTypeTemplateResponse>>> GetGameTypes(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<GameTypeTemplateResponse> gameTypes =
            await gameTypesService.GetGameTypes(cancellationToken);

        return this.Ok(gameTypes);
    }
}
