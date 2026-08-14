using Microsoft.AspNetCore.Mvc;

using Wizards.Application.DTOs.Requests;

namespace Wizards.Api.Controllers;

/// <summary>
/// Serves the registrations held against a single event.
/// </summary>
[ApiController]
[Route("events/{eventId:guid}/registrations")]
[Produces("application/json")]
public class RegistrationsController : ControllerBase
{
    /// <summary>
    /// Registers a player for an event.
    /// </summary>
    /// <remarks>
    /// A scaffold. The request is bound and validated, but nothing is persisted and the event's
    /// registration limit is not enforced, so a full event still accepts a player. Becomes a 201
    /// addressing the created registration once there is one to address.
    /// </remarks>
    /// <param name="eventId">The identifier of the event to register for.</param>
    /// <param name="request">The player's details.</param>
    /// <returns>An empty success response.</returns>
    /// <response code="200">The registration was accepted.</response>
    /// <response code="400">The supplied details failed validation.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult CreateRegistration(Guid eventId, [FromBody] CreateRegistrationRequest request)
    {
        return this.Ok();
    }
}
