using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;

using Wizards.Api.Extensions;
using Wizards.Application.DTOs.Requests;
using Wizards.Application.DTOs.Responses;
using Wizards.Application.Interfaces;
using Wizards.Application.Models;

namespace Wizards.Api.Controllers;

/// <summary>
/// Serves the events resource.
/// </summary>
[ApiController]
[Route("events")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly IEventsService eventsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventsController"/> class.
    /// </summary>
    /// <param name="eventsService">
    /// The service backing every action on this controller. Supplied by dependency injection; never
    /// <see langword="null"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="eventsService"/> is <see langword="null"/>.
    /// </exception>
    public EventsController(IEventsService eventsService)
    {
        ArgumentNullException.ThrowIfNull(eventsService);

        this.eventsService = eventsService;
    }

    /// <summary>
    /// Retrieves a single event by its identifier.
    /// </summary>
    /// <param name="eventId">The identifier of the event to retrieve.</param>
    /// <param name="cancellationToken">Cancels the request before it completes.</param>
    /// <returns>The matching event.</returns>
    /// <response code="200">The event was retrieved.</response>
    /// <response code="404">
    /// No event carries the supplied identifier. An empty identifier is never assigned to an event and
    /// is reported the same way.
    /// </response>
    [HttpGet("{eventId:guid}", Name = nameof(GetEvent))]
    [ProducesResponseType<EventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetEvent(Guid eventId, CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty)
        {
            return this.NotFound();
        }

        EventResponse? @event = await this.eventsService.GetEvent(eventId, cancellationToken);

        if (@event is null)
        {
            return this.NotFound();
        }

        return this.Ok(@event);
    }

    /// <summary>
    /// Creates an event.
    /// </summary>
    /// <param name="request">The details of the event to create.</param>
    /// <param name="cancellationToken">Cancels the request before it completes.</param>
    /// <returns>The created event, with a location header addressing it.</returns>
    /// <response code="201">The event was created.</response>
    /// <response code="400">
    /// The supplied details failed validation, break a rule about what makes a valid event, such as a
    /// start date and time that has already passed or an end that does not fall after the start, or
    /// name a game type that is not registered.
    /// </response>
    [HttpPost]
    [ProducesResponseType<EventResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventResponse>> CreateEvent(
        [FromBody] CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        EventWriteResult result = await this.eventsService.AddEvent(request, cancellationToken);

        return result switch
        {
            { Event: { } createdEvent } => this.CreatedAtRoute(
                nameof(GetEvent),
                new { eventId = createdEvent.EventId },
                createdEvent),
            { Error: { } error } => this.ToProblem(error),
            _ => throw new UnreachableException("Result carried neither an event nor an error.")
        };
    }
}
