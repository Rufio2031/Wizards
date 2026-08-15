using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;

using Wizards.Api.Extensions;
using Wizards.Application.DTOs.Requests;
using Wizards.Application.DTOs.Responses;
using Wizards.Application.Interfaces;
using Wizards.Application.Models;

namespace Wizards.Api.Controllers;

[ApiController]
[Route("events/{eventId:guid}/registrations")]
public class RegistrationsController(IRegistrationsService registrationsService) : ControllerBase
{
    /// <summary>
    /// Retrieves the players registered for an event, in the order they registered.
    /// </summary>
    /// <param name="eventId">The identifier of the event to read registrations for.</param>
    /// <param name="cancellationToken">Cancels the request before it completes.</param>
    /// <returns>The registrations held against the event.</returns>
    /// <response code="200">
    /// The registrations were retrieved. An event nobody has registered for carries none.
    /// </response>
    /// <response code="404">
    /// No event carries the supplied identifier. An empty identifier is never assigned to an event and
    /// is reported the same way.
    /// </response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RegistrationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RegistrationResponse>>> GetRegistrations(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty)
        {
            return this.NotFound();
        }

        IReadOnlyList<RegistrationResponse>? registrations =
            await registrationsService.GetRegistrations(eventId, cancellationToken);

        if (registrations is null)
        {
            return this.NotFound();
        }

        return this.Ok(registrations);
    }

    /// <summary>
    /// Registers a player for an event.
    /// </summary>
    /// <param name="eventId">The identifier of the event to register for.</param>
    /// <param name="request">The player's details.</param>
    /// <param name="cancellationToken">Cancels the request before it completes.</param>
    /// <returns>The registration the player holds against the event.</returns>
    /// <response code="200">
    /// The player is registered. Repeating an idempotency key returns the registration it first took,
    /// unchanged, rather than taking another.
    /// </response>
    /// <response code="400">
    /// The supplied details failed validation, are missing an idempotency key, or break a rule about
    /// what makes a valid registration.
    /// </response>
    /// <response code="404">
    /// No event carries the supplied identifier.
    /// </response>
    /// <response code="409">
    /// The event has taken every registration it accepts. The request is well formed and resending it
    /// unchanged succeeds once a seat frees up.
    /// </response>
    [HttpPost]
    [ProducesResponseType<RegistrationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegistrationResponse>> CreateRegistration(
        Guid eventId,
        [FromBody] CreateRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty)
        {
            return this.NotFound();
        }

        WriteResult<RegistrationResponse> result = await registrationsService.AddRegistration(
            eventId,
            request,
            cancellationToken);

        return result switch
        {
            { Value: { } registration } => this.Ok(registration),
            { Error: { } error } => this.ToProblem(error),
            _ => throw new UnreachableException("Result carried neither a registration nor an error.")
        };
    }
}
