using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;

using Wizards.Api.Extensions;
using Wizards.Application.DTOs.Requests;
using Wizards.Application.DTOs.Responses;
using Wizards.Application.Interfaces;
using Wizards.Application.Models;

namespace Wizards.Api.Controllers;

[ApiController]
[Route("events")]
[Produces("application/json")]
public class EventsController(IEventsService eventsService) : ControllerBase
{
    /// <summary>
    /// Retrieves a page of events, ordered as asked and optionally narrowed to a date range.
    /// </summary>
    /// <remarks>
    /// The range bounds an event's start, not its end, and is half open: an event starting at exactly
    /// <c>startingOnOrAfter</c> is carried, and one starting at exactly <c>startingBefore</c> is not,
    /// so adjacent ranges tile without overlapping. Both bounds denote instants in UTC, resolved as
    /// <see cref="GetEventsRequest.StartingOnOrAfterUtc"/> describes.
    /// </remarks>
    /// <param name="request">
    /// The paging window, ordering and date range to read. Every part is optional, and omitting it all
    /// reads the first <see cref="GetEventsRequest.DefaultTake"/> events by start date and time,
    /// earliest first, over an unbounded range.
    /// </param>
    /// <param name="cancellationToken">Cancels the request before it completes.</param>
    /// <returns>
    /// The page of events falling in the requested window, carrying the window itself and the size of
    /// the selection in its <see cref="Page{T}.Pagination"/>. The page carries no events when the
    /// window falls past the end, or when nothing falls in the range.
    /// </returns>
    /// <response code="200">The page was retrieved.</response>
    /// <response code="400">
    /// The window skips a negative number of events, takes fewer than one or more than
    /// <see cref="GetEventsRequest.MaxTake"/>, names a sort field or direction that does not exist, or
    /// bounds the range with a start that falls after its end.
    /// </response>
    [HttpGet]
    [ProducesResponseType<Page<EventResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Page<EventResponse>>> GetEvents(
        [FromQuery] GetEventsRequest request,
        CancellationToken cancellationToken)
    {
        Page<EventResponse> page = await eventsService.GetEvents(request, cancellationToken);

        return this.Ok(page);
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

        EventResponse? @event = await eventsService.GetEvent(eventId, cancellationToken);

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
    /// reference a game type that is not registered.
    /// </response>
    [HttpPost]
    [ProducesResponseType<EventResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventResponse>> CreateEvent(
        [FromBody] CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        EventWriteResult result = await eventsService.AddEvent(request, cancellationToken);

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
