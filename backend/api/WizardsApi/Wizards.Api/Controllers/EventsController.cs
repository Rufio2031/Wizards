using System.Diagnostics;
using System.Text;

using Microsoft.AspNetCore.Mvc;

using Wizards.Api.Extensions;
using Wizards.Application.DTOs.Requests;
using Wizards.Application.DTOs.Responses;
using Wizards.Application.Interfaces;
using Wizards.Application.Models;

namespace Wizards.Api.Controllers;

[ApiController]
[Route("events")]
public class EventsController(
    IEventsService eventsService,
    ICalendarInviteService calendarInviteService) : ControllerBase
{
    /// <summary>
    /// Retrieves a page of events, ordered as asked and optionally narrowed to a date range.
    /// </summary>
    /// <remarks>
    /// The range bounds an event's start rather than its end, in UTC, and is half open.
    /// </remarks>
    /// <param name="request">
    /// The paging window, ordering and date range to read, with every part optional.
    /// </param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The matching page, empty when nothing falls in the window.</returns>
    /// <response code="200">The page was retrieved.</response>
    /// <response code="400">The paging window, ordering or date range is not valid.</response>
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

    /// <summary>Retrieves a single event by its identifier.</summary>
    /// <param name="eventId">The identifier of the event to retrieve.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The matching event.</returns>
    /// <response code="200">The event was retrieved.</response>
    /// <response code="404">No event carries the supplied identifier.</response>
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

    /// <summary>Downloads an event as a calendar invite.</summary>
    /// <param name="eventId">The identifier of the event to describe.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>
    /// The invite, offered as a download named after the event's identifier that calendar clients
    /// import directly.
    /// </returns>
    /// <response code="200">The invite was built.</response>
    /// <response code="404">No event carries the supplied identifier.</response>
    [HttpGet("{eventId:guid}/calendar.ics", Name = nameof(GetEventCalendarInvite))]
    [ProducesResponseType<byte[]>(StatusCodes.Status200OK, CalendarInvite.MediaType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventCalendarInvite(Guid eventId, CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty)
        {
            return this.NotFound();
        }

        CalendarInvite? invite = await calendarInviteService.GetInvite(eventId, cancellationToken);

        if (invite is null)
        {
            return this.NotFound();
        }

        // Naming the download here is what makes the response an attachment, so a browser saves the
        // invite and hands it to a calendar rather than rendering it as text.
        return this.File(
            Encoding.UTF8.GetBytes(invite.Content),
            invite.ContentType,
            invite.FileName);
    }

    /// <summary>Creates an event.</summary>
    /// <param name="request">The details of the event to create.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The created event, with a location header addressing it.</returns>
    /// <response code="201">The event was created.</response>
    /// <response code="400">
    /// The supplied details are not valid, with each error keyed by the field it is about.
    /// </response>
    [HttpPost]
    [ProducesResponseType<EventResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventResponse>> CreateEvent(
        [FromBody] CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        WriteResult<EventResponse> result = await eventsService.AddEvent(request, cancellationToken);

        return result switch
        {
            { Value: { } createdEvent } => this.CreatedAtRoute(
                nameof(GetEvent),
                new { eventId = createdEvent.EventId },
                createdEvent),
            { Error: { } error } => this.ToProblem(error),
            _ => throw new UnreachableException("Result carried neither an event nor an error.")
        };
    }
}
