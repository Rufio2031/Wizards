using System.Diagnostics;
using System.Text;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using NSubstitute;

using Wizards.Api.Controllers;
using Wizards.Application.DTOs.Requests;
using Wizards.Application.DTOs.Responses;
using Wizards.Application.Interfaces;
using Wizards.Application.Models;

namespace WizardsApi.Tests.Controllers;

public sealed class EventsControllerTests
{
    private static readonly Guid EventId = new("3f2f6d7e-6f4a-4f2b-9d1a-9c5a4b3c2d1e");

    private static readonly Guid GameTypeId = new("8a1c0b2d-4e5f-4a6b-8c7d-9e0f1a2b3c4d");

    private readonly IEventsService eventsService = Substitute.For<IEventsService>();
    private readonly ICalendarInviteService calendarInviteService = Substitute.For<ICalendarInviteService>();

    private readonly EventsController controller;

    public EventsControllerTests()
    {
        this.controller = new EventsController(this.eventsService, this.calendarInviteService);
    }

    [Fact]
    public async Task GetEvents_ServiceReturnsAPage_ReturnsThatPage()
    {
        Page<EventResponse> page = new([BuildEvent()], new PaginationMeta(0, 50, 1));
        GetEventsRequest request = new();

        this.eventsService.GetEvents(request, Arg.Any<CancellationToken>()).Returns(page);

        ActionResult<Page<EventResponse>> result = await this.controller.GetEvents(
            request,
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(page, ok.Value);
    }

    [Fact]
    public async Task GetEvent_IdentifierIsEmpty_ReturnsNotFoundWithoutReadingTheEvent()
    {
        ActionResult<EventResponse> result = await this.controller.GetEvent(
            Guid.Empty,
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        await this.eventsService.DidNotReceiveWithAnyArgs().GetEvent(default, default);
    }

    [Fact]
    public async Task GetEvent_NoEventCarriesTheIdentifier_ReturnsNotFound()
    {
        this.eventsService.GetEvent(EventId, Arg.Any<CancellationToken>()).Returns((EventResponse?)null);

        ActionResult<EventResponse> result = await this.controller.GetEvent(EventId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetEvent_EventCarriesTheIdentifier_ReturnsThatEvent()
    {
        EventResponse @event = BuildEvent();

        this.eventsService.GetEvent(EventId, Arg.Any<CancellationToken>()).Returns(@event);

        ActionResult<EventResponse> result = await this.controller.GetEvent(EventId, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(@event, ok.Value);
    }

    [Fact]
    public async Task GetEventCalendarInvite_IdentifierIsEmpty_ReturnsNotFoundWithoutBuildingAnInvite()
    {
        IActionResult result = await this.controller.GetEventCalendarInvite(
            Guid.Empty,
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        await this.calendarInviteService.DidNotReceiveWithAnyArgs().GetInvite(default, default);
    }

    [Fact]
    public async Task GetEventCalendarInvite_NoEventCarriesTheIdentifier_ReturnsNotFound()
    {
        this.calendarInviteService.GetInvite(EventId, Arg.Any<CancellationToken>())
            .Returns((CalendarInvite?)null);

        IActionResult result = await this.controller.GetEventCalendarInvite(EventId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetEventCalendarInvite_InviteWasBuilt_ReturnsItAsANamedDownload()
    {
        CalendarInvite invite = new("game-night.ics", CalendarInvite.MediaType, "BEGIN:VCALENDAR");

        this.calendarInviteService.GetInvite(EventId, Arg.Any<CancellationToken>()).Returns(invite);

        IActionResult result = await this.controller.GetEventCalendarInvite(EventId, CancellationToken.None);

        FileContentResult file = Assert.IsType<FileContentResult>(result);
        Assert.Equal(invite.FileName, file.FileDownloadName);
        Assert.Equal(invite.ContentType, file.ContentType.ToString());
        Assert.Equal(Encoding.UTF8.GetBytes(invite.Content), file.FileContents);
    }

    [Fact]
    public async Task CreateEvent_EventWasCreated_ReturnsItAddressedByTheGetEventRoute()
    {
        EventResponse created = BuildEvent();
        CreateEventRequest request = BuildRequest();

        this.eventsService.AddEvent(request, Arg.Any<CancellationToken>())
            .Returns(WriteResult<EventResponse>.Success(created));

        ActionResult<EventResponse> result = await this.controller.CreateEvent(
            request,
            CancellationToken.None);

        CreatedAtRouteResult createdAtRoute = Assert.IsType<CreatedAtRouteResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, createdAtRoute.StatusCode);
        Assert.Equal(nameof(EventsController.GetEvent), createdAtRoute.RouteName);
        Assert.True(createdAtRoute.RouteValues!.TryGetValue("eventId", out object? routeEventId));
        Assert.Equal(created.EventId, routeEventId);
        Assert.Same(created, createdAtRoute.Value);
    }

    [Fact]
    public async Task CreateEvent_WriteReportedAFailure_ReturnsTheProblemForThatFailure()
    {
        CreateEventRequest request = BuildRequest();

        this.eventsService.AddEvent(request, Arg.Any<CancellationToken>())
            .Returns(WriteResult<EventResponse>.Failure(EventErrors.GameTypeNotFound));

        ActionResult<EventResponse> result = await this.controller.CreateEvent(
            request,
            CancellationToken.None);

        ObjectResult problem = Assert.IsType<BadRequestObjectResult>(result.Result);
        ValidationProblemDetails details = Assert.IsType<ValidationProblemDetails>(problem.Value);
        Assert.Equal(
            [EventErrors.GameTypeNotFound.Message],
            details.Errors[EventErrors.GameTypeNotFound.Key]);
    }

    [Fact]
    public async Task CreateEvent_ResultCarriesNeitherAnEventNorAnError_ThrowsUnreachableException()
    {
        CreateEventRequest request = BuildRequest();

        this.eventsService.AddEvent(request, Arg.Any<CancellationToken>())
            .Returns(new WriteResult<EventResponse>(null, null));

        await Assert.ThrowsAsync<UnreachableException>(
            () => this.controller.CreateEvent(request, CancellationToken.None));
    }

    private static EventResponse BuildEvent()
    {
        return new EventResponse(
            EventId,
            "Game Night",
            null,
            "The Basement",
            new DateTime(2026, 9, 1, 18, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 22, 0, 0, DateTimeKind.Utc),
            8,
            new GameTypeResponse(GameTypeId, "Wizards"),
            new Dictionary<string, string>());
    }

    private static CreateEventRequest BuildRequest()
    {
        return new CreateEventRequest(
            "Game Night",
            null,
            "The Basement",
            new DateTime(2026, 9, 1, 18, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 22, 0, 0, DateTimeKind.Utc),
            8,
            new EventGameTypeRequest(GameTypeId, new Dictionary<string, string>()));
    }
}
