using Microsoft.AspNetCore.Mvc;

using Wizards.Application.DTOs.Responses;
using Wizards.Application.Interfaces;

namespace Wizards.Api.Controllers;

/// <summary>
/// Serves the game types resource.
/// </summary>
[ApiController]
[Route("gametypes")]
[Produces("application/json")]
public class GameTypesController : ControllerBase
{
    private readonly IGameTypesService gameTypesService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameTypesController"/> class.
    /// </summary>
    /// <param name="gameTypesService">
    /// The service backing every action on this controller. Supplied by dependency injection; never
    /// <see langword="null"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="gameTypesService"/> is <see langword="null"/>.
    /// </exception>
    public GameTypesController(IGameTypesService gameTypesService)
    {
        ArgumentNullException.ThrowIfNull(gameTypesService);

        this.gameTypesService = gameTypesService;
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
            await this.gameTypesService.GetGameTypes(cancellationToken);

        return this.Ok(gameTypes);
    }
}
