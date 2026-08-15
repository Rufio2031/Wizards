using NSubstitute;

using Wizards.Application.DTOs.Requests;
using Wizards.Application.DTOs.Responses;
using Wizards.Application.Enums;
using Wizards.Application.Models;
using Wizards.Application.Services;
using Wizards.Domain.Entities;
using Wizards.Domain.Enums;
using Wizards.Domain.Interfaces.Repositories;
using Wizards.Domain.Models;

namespace WizardsApi.Tests.Services;

public sealed class EventsServiceTests
{
    private readonly IEventsRepository eventsRepository = Substitute.For<IEventsRepository>();
    private readonly IGameTypesRepository gameTypesRepository = Substitute.For<IGameTypesRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly EventsService eventsService;

    public EventsServiceTests()
    {
        this.eventsService = new EventsService(
            this.eventsRepository,
            this.gameTypesRepository,
            this.unitOfWork);
    }

    [Fact]
    public async Task GetEvents_RequestIsNull_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => this.eventsService.GetEvents(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetEvents_RequestCarriesAWindowAndRange_ReadsOverThatQuery()
    {
        DateTime lowerBound = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

        GetEventsRequest request = new(
            Skip: 20,
            Take: 10,
            SortBy: EventSortField.StartDateTime,
            SortDirection: SortDirection.Descending,
            StartingOnOrAfter: lowerBound,
            StartingBefore: lowerBound.AddDays(7));

        this.eventsRepository
            .GetEventsAsync(Arg.Any<EventQuery>(), Arg.Any<CancellationToken>())
            .Returns(new EventPage([], 0));

        await this.eventsService.GetEvents(request, CancellationToken.None);

        await this.eventsRepository.Received(1).GetEventsAsync(
            new EventQuery(
                20,
                10,
                EventSortField.StartDateTime,
                SortDirection.Descending,
                lowerBound,
                lowerBound.AddDays(7)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetEvents_RepositoryReturnsAPage_ProjectsItAndReportsTheRequestedWindow()
    {
        Event @event = CreateEvent();

        this.eventsRepository
            .GetEventsAsync(Arg.Any<EventQuery>(), Arg.Any<CancellationToken>())
            .Returns(new EventPage([@event], 137));

        Page<EventResponse> page = await this.eventsService.GetEvents(
            new GetEventsRequest(Skip: 20, Take: 10),
            CancellationToken.None);

        EventResponse projected = Assert.Single(page.Items);

        Assert.Equal(@event.PublicId, projected.EventId);
        Assert.Equal(20, page.Pagination.Skip);
        Assert.Equal(10, page.Pagination.Take);
        Assert.Equal(137, page.Pagination.TotalCount);
    }

    [Fact]
    public async Task GetEvent_EventIdIsEmpty_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => this.eventsService.GetEvent(Guid.Empty, CancellationToken.None));

        await this.eventsRepository.DidNotReceive().GetEventByPublicIdAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetEvent_NoEventCarriesTheIdentifier_ReturnsNull()
    {
        Guid eventId = Guid.CreateVersion7();

        this.eventsRepository
            .GetEventByPublicIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns((Event?)null);

        EventResponse? response = await this.eventsService.GetEvent(eventId, CancellationToken.None);

        Assert.Null(response);
    }

    [Fact]
    public async Task GetEvent_EventCarriesTheIdentifier_ReturnsItProjectedOntoAnEventResponse()
    {
        Event @event = CreateEvent();

        this.eventsRepository
            .GetEventByPublicIdAsync(@event.PublicId, Arg.Any<CancellationToken>())
            .Returns(@event);

        EventResponse? response = await this.eventsService.GetEvent(
            @event.PublicId,
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(@event.PublicId, response.EventId);
        Assert.Equal(@event.Name, response.Name);
        Assert.Equal(@event.Description, response.Description);
        Assert.Equal(@event.Location, response.Location);
        Assert.Equal(@event.StartDateTime, response.StartDateTime);
        Assert.Equal(@event.EndDateTime, response.EndDateTime);
        Assert.Equal(@event.RegistrationLimit, response.RegistrationLimit);
        Assert.Equal(@event.GameType.PublicId, response.GameType.GameTypeId);
    }

    [Fact]
    public async Task AddEvent_RequestIsNull_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => this.eventsService.AddEvent(null!, CancellationToken.None));
    }

    [Fact]
    public async Task AddEvent_GameTypeIsNull_ThrowsArgumentNullExceptionWithoutReadingTheGameType()
    {
        CreateEventRequest request = CreateRequest() with { GameType = null! };

        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => this.eventsService.AddEvent(request, CancellationToken.None));

        Assert.Equal("request.GameType", exception.ParamName);

        await this.gameTypesRepository.DidNotReceive().GetGameTypeByPublicIdAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddEvent_GameTypeIsNotRegistered_ReturnsGameTypeNotFoundAndWritesNothing()
    {
        this.gameTypesRepository
            .GetGameTypeByPublicIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((GameType?)null);

        WriteResult<EventResponse> result = await this.eventsService.AddEvent(
            CreateRequest(),
            CancellationToken.None);

        Assert.Null(result.Value);
        Assert.Equal(EventErrors.GameTypeNotFound, result.Error);

        await this.eventsRepository.DidNotReceive().AddEventAsync(
            Arg.Any<Event>(),
            Arg.Any<CancellationToken>());
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddEvent_SelectionNamesNoSetting_ReturnsFailureKeyedToTheSelectionsAsAWhole()
    {
        GameType gameType = this.RegisterGameType();

        CreateEventRequest request = CreateRequest(
            gameType.PublicId,
            selections: new Dictionary<string, string> { ["   "] = "Commander" });

        WriteResult<EventResponse> result = await this.eventsService.AddEvent(request, CancellationToken.None);

        Assert.Null(result.Value);
        Assert.NotNull(result.Error);
        Assert.Equal(ErrorKind.Invalid, result.Error.Kind);
        Assert.Equal("gameType.selections", result.Error.Key);
        Assert.Equal("A game type setting key is required.", result.Error.Message);

        await this.eventsRepository.DidNotReceive().AddEventAsync(
            Arg.Any<Event>(),
            Arg.Any<CancellationToken>());
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddEvent_SelectionKeyIsLongerThanTheMaximum_ReturnsFailureKeyedToTheSelectionsAsAWholeWithoutEchoingTheKey()
    {
        GameType gameType = this.RegisterGameType();

        string key = new('a', GameTypeSetting.MaxKeyLength + 1);

        CreateEventRequest request = CreateRequest(
            gameType.PublicId,
            selections: new Dictionary<string, string> { [key] = "Commander" });

        WriteResult<EventResponse> result = await this.eventsService.AddEvent(request, CancellationToken.None);

        Assert.Null(result.Value);
        Assert.NotNull(result.Error);
        Assert.Equal(ErrorKind.Invalid, result.Error.Kind);
        Assert.Equal("gameType.selections", result.Error.Key);
        Assert.Equal(
            $"A game type setting key cannot exceed {GameTypeSetting.MaxKeyLength} characters.",
            result.Error.Message);

        await this.eventsRepository.DidNotReceive().AddEventAsync(
            Arg.Any<Event>(),
            Arg.Any<CancellationToken>());
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddEvent_SelectionNamesASettingTheGameTypeDoesNotExpose_ReturnsFailureKeyedToThatSelection()
    {
        GameType gameType = this.RegisterGameType();

        CreateEventRequest request = CreateRequest(
            gameType.PublicId,
            selections: new Dictionary<string, string> { ["format"] = "Commander" });

        WriteResult<EventResponse> result = await this.eventsService.AddEvent(request, CancellationToken.None);

        Assert.Null(result.Value);
        Assert.NotNull(result.Error);
        Assert.Equal(ErrorKind.Invalid, result.Error.Kind);
        Assert.Equal("gameType.selections.format", result.Error.Key);
        Assert.Equal($"{gameType.Name} has no 'format' setting.", result.Error.Message);

        await this.eventsRepository.DidNotReceive().AddEventAsync(
            Arg.Any<Event>(),
            Arg.Any<CancellationToken>());
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddEvent_StartDateTimeHasAlreadyPassed_ReturnsFailureKeyedToTheStartDateTimeField()
    {
        GameType gameType = this.RegisterGameType();

        CreateEventRequest request = CreateRequest(
            gameType.PublicId,
            startDateTime: DateTime.UtcNow.AddDays(-1));

        WriteResult<EventResponse> result = await this.eventsService.AddEvent(request, CancellationToken.None);

        Assert.Null(result.Value);
        Assert.NotNull(result.Error);
        Assert.Equal(ErrorKind.Invalid, result.Error.Kind);
        Assert.Equal("StartDateTime", result.Error.Key);
        Assert.Equal("Event start date and time cannot be in the past.", result.Error.Message);

        await this.eventsRepository.DidNotReceive().AddEventAsync(
            Arg.Any<Event>(),
            Arg.Any<CancellationToken>());
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddEvent_EndDateTimeDoesNotFallAfterTheStart_ReturnsFailureKeyedToTheEndDateTimeField()
    {
        GameType gameType = this.RegisterGameType();

        CreateEventRequest request = CreateRequest(gameType.PublicId);

        request = request with { EndDateTime = request.StartDateTime };

        WriteResult<EventResponse> result = await this.eventsService.AddEvent(request, CancellationToken.None);

        Assert.Null(result.Value);
        Assert.NotNull(result.Error);
        Assert.Equal(ErrorKind.Invalid, result.Error.Kind);
        Assert.Equal("EndDateTime", result.Error.Key);
        Assert.Equal(
            "Event start date and time must be before the end date and time.",
            result.Error.Message);

        await this.eventsRepository.DidNotReceive().AddEventAsync(
            Arg.Any<Event>(),
            Arg.Any<CancellationToken>());
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddEvent_RegistrationLimitBreaksADomainRule_ReturnsFailureKeyedToTheRegistrationLimitField()
    {
        GameType gameType = this.RegisterGameType();

        CreateEventRequest request = CreateRequest(gameType.PublicId) with
        {
            RegistrationLimit = Event.MaxRegistrationLimit + 1
        };

        WriteResult<EventResponse> result = await this.eventsService.AddEvent(request, CancellationToken.None);

        Assert.Null(result.Value);
        Assert.NotNull(result.Error);
        Assert.Equal(ErrorKind.Invalid, result.Error.Kind);
        Assert.Equal("RegistrationLimit", result.Error.Key);
        Assert.Equal(
            $"An event cannot accept more than {Event.MaxRegistrationLimit} players.",
            result.Error.Message);

        await this.eventsRepository.DidNotReceive().AddEventAsync(
            Arg.Any<Event>(),
            Arg.Any<CancellationToken>());
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddEvent_RequestIsValid_AddsTheEventSavesChangesAndReturnsIt()
    {
        GameType gameType = this.RegisterGameType();

        CreateEventRequest request = CreateRequest(gameType.PublicId);

        WriteResult<EventResponse> result = await this.eventsService.AddEvent(request, CancellationToken.None);

        Assert.Null(result.Error);
        Assert.NotNull(result.Value);

        EventResponse createdEvent = result.Value;

        Assert.NotEqual(Guid.Empty, createdEvent.EventId);
        Assert.Equal(request.Name, createdEvent.Name);
        Assert.Equal(request.Description, createdEvent.Description);
        Assert.Equal(request.Location, createdEvent.Location);
        Assert.Equal(request.StartDateTime, createdEvent.StartDateTime);
        Assert.Equal(request.EndDateTime, createdEvent.EndDateTime);
        Assert.Equal(request.RegistrationLimit, createdEvent.RegistrationLimit);
        Assert.Equal(gameType.PublicId, createdEvent.GameType.GameTypeId);

        await this.eventsRepository.Received(1).AddEventAsync(
            Arg.Is<Event>(added => added.PublicId == createdEvent.EventId),
            Arg.Any<CancellationToken>());
        await this.unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddEvent_TextFieldsOnlyFitOnceTrimmed_CreatesTheEventCarryingTheTrimmedValues()
    {
        GameType gameType = this.RegisterGameType();

        string name = new('a', Event.MaxNameLength);
        string description = new('b', Event.MaxDescriptionLength);
        string location = new('c', Event.MaxLocationLength);

        CreateEventRequest request = CreateRequest(gameType.PublicId) with
        {
            Name = $"{name}   ",
            Description = $"{description}   ",
            Location = $"{location}   "
        };

        WriteResult<EventResponse> result = await this.eventsService.AddEvent(request, CancellationToken.None);

        Assert.Null(result.Error);
        Assert.NotNull(result.Value);
        Assert.Equal(name, result.Value.Name);
        Assert.Equal(description, result.Value.Description);
        Assert.Equal(location, result.Value.Location);
    }

    [Fact]
    public async Task AddEvent_DescriptionIsOnlyWhitespace_CreatesTheEventWithoutOne()
    {
        GameType gameType = this.RegisterGameType();

        CreateEventRequest request = CreateRequest(gameType.PublicId) with { Description = "   " };

        WriteResult<EventResponse> result = await this.eventsService.AddEvent(request, CancellationToken.None);

        Assert.Null(result.Error);
        Assert.NotNull(result.Value);
        Assert.Null(result.Value.Description);
    }

    private GameType RegisterGameType()
    {
        GameType gameType = GameType.Create("Magic: The Gathering");

        this.gameTypesRepository
            .GetGameTypeByPublicIdAsync(gameType.PublicId, Arg.Any<CancellationToken>())
            .Returns(gameType);

        return gameType;
    }

    private static Event CreateEvent()
    {
        DateTime start = DateTime.UtcNow.AddDays(1);

        return Event.Create(
            "Friday Night Magic",
            "A weekly casual tournament.",
            "Store Front Room",
            GameType.Create("Magic: The Gathering"),
            start,
            start.AddHours(3),
            8);
    }

    private static CreateEventRequest CreateRequest(
        Guid? gameTypeId = null,
        DateTime? startDateTime = null,
        IReadOnlyDictionary<string, string>? selections = null)
    {
        DateTime start = startDateTime ?? DateTime.UtcNow.AddDays(1);

        return new CreateEventRequest(
            "Friday Night Magic",
            "A weekly casual tournament.",
            "Store Front Room",
            start,
            start.AddHours(3),
            8,
            new EventGameTypeRequest(gameTypeId ?? Guid.NewGuid(), selections));
    }
}
