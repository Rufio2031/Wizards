using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Wizards.Application.DTOs.Requests;
using Wizards.Application.DTOs.Responses;
using Wizards.Application.Enums;
using Wizards.Application.Models;
using Wizards.Application.Services;
using Wizards.Domain.Entities;
using Wizards.Domain.Exceptions;
using Wizards.Domain.Interfaces.Repositories;

namespace WizardsApi.Tests.Services;

public sealed class RegistrationsServiceTests
{
    private static readonly Guid IdempotencyKey = new("6b1f9f0e-3a2c-4d5b-8e7f-1a2b3c4d5e6f");

    private readonly IEventsRepository eventsRepository = Substitute.For<IEventsRepository>();
    private readonly IEventRegistrationsRepository registrationsRepository =
        Substitute.For<IEventRegistrationsRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RegistrationsService registrationsService;

    public RegistrationsServiceTests()
    {
        this.registrationsService = new RegistrationsService(
            this.eventsRepository,
            this.registrationsRepository,
            this.unitOfWork);
    }

    [Fact]
    public async Task AddRegistration_RequestIsNull_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => this.registrationsService.AddRegistration(
            Guid.CreateVersion7(),
            null!,
            CancellationToken.None));
    }

    [Fact]
    public async Task AddRegistration_RequestIsNull_NamesTheRequestWithoutReadingTheEvent()
    {
        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => this.registrationsService.AddRegistration(
                Guid.CreateVersion7(),
                null!,
                CancellationToken.None));

        Assert.Equal("request", exception.ParamName);

        await this.eventsRepository.DidNotReceiveWithAnyArgs().GetEventByPublicIdAsync(default, default);
    }

    [Fact]
    public async Task AddRegistration_EventIdIsEmpty_ThrowsArgumentExceptionWithoutReadingTheEvent()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => this.registrationsService.AddRegistration(
            Guid.Empty,
            BuildRequest(),
            CancellationToken.None));

        await this.eventsRepository.DidNotReceiveWithAnyArgs().GetEventByPublicIdAsync(default, default);
    }

    [Fact]
    public async Task AddRegistration_EventIdIsEmpty_NamesTheEventIdentifier()
    {
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => this.registrationsService.AddRegistration(
                Guid.Empty,
                BuildRequest(),
                CancellationToken.None));

        Assert.Equal("eventId", exception.ParamName);
    }

    [Fact]
    public async Task AddRegistration_NoEventCarriesTheIdentifier_ReturnsEventNotFoundWithoutWriting()
    {
        Guid eventId = Guid.CreateVersion7();

        this.eventsRepository
            .GetEventByPublicIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns((Event?)null);

        WriteResult<RegistrationResponse> result = await this.registrationsService.AddRegistration(
            eventId,
            BuildRequest(),
            CancellationToken.None);

        Assert.Null(result.Value);
        Assert.Equal(RegistrationErrors.EventNotFound, result.Error);

        await this.registrationsRepository.DidNotReceiveWithAnyArgs().AddRegistrationAsync(default!, default);
        await this.unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task AddRegistration_EventAlreadyHoldsTheKey_ReturnsTheOriginalRegistrationWithoutWriting()
    {
        Event @event = this.ScheduleEvent();

        this.HoldRegistration(@event, "Ada Lovelace", IdempotencyKey);

        WriteResult<RegistrationResponse> result = await this.registrationsService.AddRegistration(
            @event.PublicId,
            BuildRequest(),
            CancellationToken.None);

        Assert.Null(result.Error);
        Assert.Equal(new RegistrationResponse("Ada Lovelace"), result.Value);

        await this.registrationsRepository.DidNotReceiveWithAnyArgs().AddRegistrationAsync(default!, default);
        await this.unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task AddRegistration_EventIsFullAndAlreadyHoldsTheKey_ReturnsTheOriginalRegistrationRatherThanReportingTheEventFull()
    {
        Event @event = this.ScheduleEvent(registrationLimit: 4);

        this.HoldRegistration(@event, "Ada Lovelace", IdempotencyKey);

        this.registrationsRepository
            .CountRegistrationsAsync(@event, Arg.Any<CancellationToken>())
            .Returns(4);

        WriteResult<RegistrationResponse> result = await this.registrationsService.AddRegistration(
            @event.PublicId,
            BuildRequest(),
            CancellationToken.None);

        Assert.Null(result.Error);
        Assert.Equal(new RegistrationResponse("Ada Lovelace"), result.Value);
    }

    [Fact]
    public async Task AddRegistration_EventHasBegunAndAlreadyHoldsTheKey_ReturnsTheOriginalRegistrationRatherThanReportingRegistrationClosed()
    {
        Event @event = this.ScheduleStartedEvent();

        this.HoldRegistration(@event, "Ada Lovelace", IdempotencyKey);

        WriteResult<RegistrationResponse> result = await this.registrationsService.AddRegistration(
            @event.PublicId,
            BuildRequest(),
            CancellationToken.None);

        Assert.Null(result.Error);
        Assert.Equal(new RegistrationResponse("Ada Lovelace"), result.Value);

        await this.registrationsRepository.DidNotReceiveWithAnyArgs().AddRegistrationAsync(default!, default);
        await this.unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task AddRegistration_EventHasBegunAndIsFullAndAlreadyHoldsTheKey_ReturnsTheOriginalRegistrationRatherThanReportingEitherConflict()
    {
        Event @event = this.ScheduleStartedEvent(registrationLimit: 4);

        this.HoldRegistration(@event, "Ada Lovelace", IdempotencyKey);

        this.registrationsRepository
            .CountRegistrationsAsync(@event, Arg.Any<CancellationToken>())
            .Returns(4);

        WriteResult<RegistrationResponse> result = await this.registrationsService.AddRegistration(
            @event.PublicId,
            BuildRequest(),
            CancellationToken.None);

        Assert.Null(result.Error);
        Assert.Equal(new RegistrationResponse("Ada Lovelace"), result.Value);
    }

    [Fact]
    public async Task AddRegistration_ReplayCarriesACorrectedName_ReturnsTheOriginalNameAndWritesNothing()
    {
        Event @event = this.ScheduleEvent();

        this.HoldRegistration(@event, "Ada Lovelace", IdempotencyKey);

        WriteResult<RegistrationResponse> result = await this.registrationsService.AddRegistration(
            @event.PublicId,
            BuildRequest("Augusta Ada King"),
            CancellationToken.None);

        Assert.Null(result.Error);
        Assert.Equal(new RegistrationResponse("Ada Lovelace"), result.Value);

        await this.registrationsRepository.DidNotReceiveWithAnyArgs().AddRegistrationAsync(default!, default);
        await this.unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task AddRegistration_KeyIsHeldAgainstAnotherEvent_TakesANewRegistrationForThisOne()
    {
        Event keyHolder = this.ScheduleEvent();
        Event otherEvent = this.ScheduleEvent();

        this.HoldRegistration(keyHolder, "Ada Lovelace", IdempotencyKey);

        WriteResult<RegistrationResponse> result = await this.registrationsService.AddRegistration(
            otherEvent.PublicId,
            BuildRequest("Grace Hopper"),
            CancellationToken.None);

        Assert.Null(result.Error);
        Assert.Equal(new RegistrationResponse("Grace Hopper"), result.Value);

        await this.registrationsRepository.Received(1).AddRegistrationAsync(
            Arg.Is<EventRegistration>(added =>
                added.Event == otherEvent && added.IdempotencyKey == IdempotencyKey),
            Arg.Any<CancellationToken>());
        await this.unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddRegistration_EventIsFull_ReturnsEventFullWithoutWriting()
    {
        Event @event = this.ScheduleEvent(registrationLimit: 4);

        this.registrationsRepository
            .CountRegistrationsAsync(@event, Arg.Any<CancellationToken>())
            .Returns(4);

        WriteResult<RegistrationResponse> result = await this.registrationsService.AddRegistration(
            @event.PublicId,
            BuildRequest(),
            CancellationToken.None);

        Assert.Null(result.Value);
        Assert.Equal(RegistrationErrors.EventFull, result.Error);

        await this.registrationsRepository.DidNotReceiveWithAnyArgs().AddRegistrationAsync(default!, default);
        await this.unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task AddRegistration_IdempotencyKeyIsEmpty_ReturnsFailureKeyedToTheIdempotencyKey()
    {
        Event @event = this.ScheduleEvent();

        WriteResult<RegistrationResponse> result = await this.registrationsService.AddRegistration(
            @event.PublicId,
            new CreateRegistrationRequest("Ada Lovelace", Guid.Empty),
            CancellationToken.None);

        Assert.Null(result.Value);
        Assert.NotNull(result.Error);
        Assert.Equal(ErrorKind.Invalid, result.Error.Kind);
        Assert.Equal("IdempotencyKey", result.Error.Key);
        Assert.Equal("An idempotency key is required to register.", result.Error.Message);

        await this.unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task AddRegistration_NameIsMissing_ReturnsFailureKeyedToTheName()
    {
        Event @event = this.ScheduleEvent();

        WriteResult<RegistrationResponse> result = await this.registrationsService.AddRegistration(
            @event.PublicId,
            BuildRequest("   "),
            CancellationToken.None);

        Assert.Null(result.Value);
        Assert.NotNull(result.Error);
        Assert.Equal(ErrorKind.Invalid, result.Error.Kind);
        Assert.Equal("Name", result.Error.Key);

        await this.unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task AddRegistration_EventHasBegunAndTheNameIsMissing_ReportsTheInvalidNameRatherThanRegistrationClosed()
    {
        Event @event = this.ScheduleStartedEvent();

        WriteResult<RegistrationResponse> result = await this.registrationsService.AddRegistration(
            @event.PublicId,
            BuildRequest("   "),
            CancellationToken.None);

        Assert.Null(result.Value);
        Assert.NotNull(result.Error);
        Assert.Equal(ErrorKind.Invalid, result.Error.Kind);
        Assert.Equal("Name", result.Error.Key);
    }

    [Fact]
    public async Task AddRegistration_EventHasBegun_ReturnsRegistrationClosedWithoutWriting()
    {
        Event @event = this.ScheduleStartedEvent();

        WriteResult<RegistrationResponse> result = await this.registrationsService.AddRegistration(
            @event.PublicId,
            BuildRequest(),
            CancellationToken.None);

        Assert.Null(result.Value);
        Assert.Equal(RegistrationErrors.RegistrationClosed, result.Error);

        await this.registrationsRepository.DidNotReceiveWithAnyArgs().AddRegistrationAsync(default!, default);
        await this.unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task AddRegistration_EventHasBegunWithRoomToSpare_StillReturnsRegistrationClosed()
    {
        Event @event = this.ScheduleStartedEvent(registrationLimit: 8);

        this.registrationsRepository
            .CountRegistrationsAsync(@event, Arg.Any<CancellationToken>())
            .Returns(0);

        WriteResult<RegistrationResponse> result = await this.registrationsService.AddRegistration(
            @event.PublicId,
            BuildRequest(),
            CancellationToken.None);

        Assert.Null(result.Value);
        Assert.Equal(RegistrationErrors.RegistrationClosed, result.Error);
    }

    [Fact]
    public async Task AddRegistration_EventHasBegunAndIsFull_ReportsRegistrationClosedWithoutCountingRegistrations()
    {
        Event @event = this.ScheduleStartedEvent(registrationLimit: 4);

        this.registrationsRepository
            .CountRegistrationsAsync(@event, Arg.Any<CancellationToken>())
            .Returns(4);

        WriteResult<RegistrationResponse> result = await this.registrationsService.AddRegistration(
            @event.PublicId,
            BuildRequest(),
            CancellationToken.None);

        Assert.Null(result.Value);
        Assert.Equal(RegistrationErrors.RegistrationClosed, result.Error);

        await this.registrationsRepository.DidNotReceiveWithAnyArgs().CountRegistrationsAsync(default!, default);
    }

    [Fact]
    public async Task AddRegistration_StoreRefusedTheCommitOnARule_ReturnsEventFull()
    {
        Event @event = this.ScheduleEvent();

        this.unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new StoreRuleViolationException("The database refused the commit."));

        WriteResult<RegistrationResponse> result = await this.registrationsService.AddRegistration(
            @event.PublicId,
            BuildRequest(),
            CancellationToken.None);

        Assert.Null(result.Value);
        Assert.Equal(RegistrationErrors.EventFull, result.Error);
    }

    [Fact]
    public async Task AddRegistration_StoreRefusedTheCommitOnARuleAndTheKeyIsAlreadyHeld_ReturnsTheRegistrationTheKeyAlreadyHoldsRatherThanReportingTheEventFull()
    {
        Event @event = this.ScheduleEvent();

        this.registrationsRepository
            .GetRegistrationByIdempotencyKeyAsync(@event, IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(
                (EventRegistration?)null,
                EventRegistration.Reconstitute(@event, "Ada Lovelace", IdempotencyKey));

        this.unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new StoreRuleViolationException("The database refused the commit."));

        WriteResult<RegistrationResponse> result = await this.registrationsService.AddRegistration(
            @event.PublicId,
            BuildRequest("Augusta Ada King"),
            CancellationToken.None);

        Assert.Null(result.Error);
        Assert.Equal(new RegistrationResponse("Ada Lovelace"), result.Value);
    }

    [Fact]
    public async Task AddRegistration_StoreRefusedTheCommitOnUniqueness_ReturnsTheRegistrationTheKeyAlreadyHolds()
    {
        Event @event = this.ScheduleEvent();

        this.registrationsRepository
            .GetRegistrationByIdempotencyKeyAsync(@event, IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(
                (EventRegistration?)null,
                EventRegistration.Reconstitute(@event, "Ada Lovelace", IdempotencyKey));

        this.unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new StoreUniquenessViolationException("The database refused the commit."));

        WriteResult<RegistrationResponse> result = await this.registrationsService.AddRegistration(
            @event.PublicId,
            BuildRequest("Augusta Ada King"),
            CancellationToken.None);

        Assert.Null(result.Error);
        Assert.Equal(new RegistrationResponse("Ada Lovelace"), result.Value);
    }

    [Fact]
    public async Task AddRegistration_StoreRefusedTheCommitOnUniquenessAndNoRegistrationHoldsTheKey_Rethrows()
    {
        Event @event = this.ScheduleEvent();

        this.unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new StoreUniquenessViolationException("The database refused the commit."));

        await Assert.ThrowsAsync<StoreUniquenessViolationException>(
            () => this.registrationsService.AddRegistration(
                @event.PublicId,
                BuildRequest(),
                CancellationToken.None));
    }

    [Fact]
    public async Task AddRegistration_DetailsAreValid_AddsTheRegistrationSavesAndReturnsIt()
    {
        Event @event = this.ScheduleEvent();

        WriteResult<RegistrationResponse> result = await this.registrationsService.AddRegistration(
            @event.PublicId,
            BuildRequest(),
            CancellationToken.None);

        Assert.Null(result.Error);
        Assert.Equal(new RegistrationResponse("Ada Lovelace"), result.Value);

        await this.registrationsRepository.Received(1).AddRegistrationAsync(
            Arg.Is<EventRegistration>(added =>
                added.Event == @event
                && added.Name == "Ada Lovelace"
                && added.IdempotencyKey == IdempotencyKey),
            Arg.Any<CancellationToken>());
        await this.unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRegistrations_EventIdIsEmpty_ThrowsArgumentExceptionWithoutReadingTheEvent()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => this.registrationsService.GetRegistrations(Guid.Empty, CancellationToken.None));

        await this.eventsRepository.DidNotReceiveWithAnyArgs().GetEventByPublicIdAsync(default, default);
    }

    [Fact]
    public async Task GetRegistrations_NoEventCarriesTheIdentifier_ReturnsNull()
    {
        Guid eventId = Guid.CreateVersion7();

        this.eventsRepository
            .GetEventByPublicIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns((Event?)null);

        Assert.Null(await this.registrationsService.GetRegistrations(eventId, CancellationToken.None));
    }

    [Fact]
    public async Task GetRegistrations_EventCarriesTheIdentifier_ProjectsTheRegistrationsItHolds()
    {
        Event @event = this.ScheduleEvent();

        this.registrationsRepository
            .GetRegistrationsAsync(@event, Arg.Any<CancellationToken>())
            .Returns([
                EventRegistration.Reconstitute(@event, "Ada Lovelace", IdempotencyKey),
                EventRegistration.Reconstitute(@event, "Grace Hopper", Guid.CreateVersion7())
            ]);

        IReadOnlyList<RegistrationResponse>? registrations =
            await this.registrationsService.GetRegistrations(@event.PublicId, CancellationToken.None);

        Assert.NotNull(registrations);
        Assert.Equal(
            [new RegistrationResponse("Ada Lovelace"), new RegistrationResponse("Grace Hopper")],
            registrations);
    }

    private Event ScheduleEvent(int registrationLimit = 8)
    {
        DateTime start = DateTime.UtcNow.AddDays(7);

        Event @event = Event.Create(
            "Friday Night Magic",
            null,
            "The Back Room",
            GameType.Create("Magic: The Gathering"),
            start,
            start.AddHours(3),
            registrationLimit);

        this.eventsRepository
            .GetEventByPublicIdAsync(@event.PublicId, Arg.Any<CancellationToken>())
            .Returns(@event);

        return @event;
    }

    // Create refuses a start that has passed, so an event that has already begun is rehydrated.
    private Event ScheduleStartedEvent(int registrationLimit = 8)
    {
        DateTime start = DateTime.UtcNow.AddHours(-1);

        Event @event = Event.Reconstitute(
            1,
            Guid.CreateVersion7(),
            "Friday Night Magic",
            null,
            "The Back Room",
            start,
            start.AddHours(3),
            GameType.Create("Magic: The Gathering"),
            registrationLimit);

        this.eventsRepository
            .GetEventByPublicIdAsync(@event.PublicId, Arg.Any<CancellationToken>())
            .Returns(@event);

        return @event;
    }

    private void HoldRegistration(Event @event, string name, Guid idempotencyKey)
    {
        this.registrationsRepository
            .GetRegistrationByIdempotencyKeyAsync(@event, idempotencyKey, Arg.Any<CancellationToken>())
            .Returns(EventRegistration.Reconstitute(@event, name, idempotencyKey));
    }

    private static CreateRegistrationRequest BuildRequest(string name = "Ada Lovelace") =>
        new(name, IdempotencyKey);
}
