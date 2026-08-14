using Microsoft.AspNetCore.Mvc;

using Wizards.Api.Extensions;
using Wizards.Application.DTOs.Requests;
using Wizards.Application.Interfaces;
using Wizards.Application.Models;

namespace Wizards.Api.Controllers;

[ApiController]
[Route("events/{eventId:guid}/registrations")]
[Produces("application/json")]
public class RegistrationsController(IRegistrationsService registrationsService) : ControllerBase
{
    /// <summary>
    /// Registers a player for an event.
    /// </summary>
    /// <param name="eventId">The identifier of the event to register for.</param>
    /// <param name="request">The player's details.</param>
    /// <param name="cancellationToken">Cancels the request before it completes.</param>
    /// <returns>An empty success response.</returns>
    /// <response code="200">The player was registered.</response>
    /// <response code="400">
    /// The supplied details failed validation or break a rule about what makes a valid registration.
    /// </response>
    /// <response code="404">
    /// No event carries the supplied identifier.
    /// </response>
    /// <response code="409">
    /// The event has taken every registration it accepts. The request is well formed and resending it
    /// unchanged succeeds once a seat frees up.
    /// </response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> CreateRegistration(
        Guid eventId,
        [FromBody] CreateRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty)
        {
            return this.NotFound();
        }

        ApplicationError? error = await registrationsService.AddRegistration(
            eventId,
            request,
            cancellationToken);

        return error is null ? this.Ok() : this.ToProblem(error);
    }
}
