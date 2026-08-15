using System.Diagnostics;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using NSubstitute;

using Wizards.Api.Controllers;
using Wizards.Application.DTOs.Requests;
using Wizards.Application.DTOs.Responses;
using Wizards.Application.Interfaces;
using Wizards.Application.Models;

namespace WizardsApi.Tests.Controllers;

public sealed class RegistrationsControllerTests
{
    private static readonly Guid EventId = new("3f2f6d7e-6f4a-4f2b-9d1a-9c5a4b3c2d1e");

    private static readonly Guid IdempotencyKey = new("6b1f9f0e-3a2c-4d5b-8e7f-1a2b3c4d5e6f");

    private readonly IRegistrationsService registrationsService = Substitute.For<IRegistrationsService>();

    private readonly RegistrationsController controller;

    public RegistrationsControllerTests()
    {
        this.controller = new RegistrationsController(this.registrationsService);
    }

    [Fact]
    public async Task GetRegistrations_EventIdIsEmpty_ReturnsNotFoundWithoutReadingThem()
    {
        ActionResult<IReadOnlyList<RegistrationResponse>> result =
            await this.controller.GetRegistrations(Guid.Empty, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        await this.registrationsService.DidNotReceiveWithAnyArgs().GetRegistrations(default, default);
    }

    [Fact]
    public async Task GetRegistrations_NoEventCarriesTheIdentifier_ReturnsNotFound()
    {
        this.registrationsService
            .GetRegistrations(EventId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<RegistrationResponse>?)null);

        ActionResult<IReadOnlyList<RegistrationResponse>> result =
            await this.controller.GetRegistrations(EventId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetRegistrations_EventCarriesTheIdentifier_ReturnsThem()
    {
        IReadOnlyList<RegistrationResponse> registrations = [new RegistrationResponse("Ada Lovelace")];

        this.registrationsService
            .GetRegistrations(EventId, Arg.Any<CancellationToken>())
            .Returns(registrations);

        ActionResult<IReadOnlyList<RegistrationResponse>> result =
            await this.controller.GetRegistrations(EventId, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(registrations, ok.Value);
    }

    [Fact]
    public async Task CreateRegistration_EventIdIsEmpty_ReturnsNotFoundWithoutWriting()
    {
        ActionResult<RegistrationResponse> result = await this.controller.CreateRegistration(
            Guid.Empty,
            BuildRequest(),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        await this.registrationsService.DidNotReceiveWithAnyArgs().AddRegistration(default, default!, default);
    }

    [Fact]
    public async Task CreateRegistration_RegistrationWasTaken_ReturnsItWithOkRatherThanCreated()
    {
        RegistrationResponse registration = new("Ada Lovelace");
        CreateRegistrationRequest request = BuildRequest();

        this.registrationsService
            .AddRegistration(EventId, request, Arg.Any<CancellationToken>())
            .Returns(WriteResult<RegistrationResponse>.Success(registration));

        ActionResult<RegistrationResponse> result = await this.controller.CreateRegistration(
            EventId,
            request,
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        Assert.Same(registration, ok.Value);
    }

    [Fact]
    public async Task CreateRegistration_EventIsFull_ReturnsConflict()
    {
        CreateRegistrationRequest request = BuildRequest();

        this.registrationsService
            .AddRegistration(EventId, request, Arg.Any<CancellationToken>())
            .Returns(WriteResult<RegistrationResponse>.Failure(RegistrationErrors.EventFull));

        ActionResult<RegistrationResponse> result = await this.controller.CreateRegistration(
            EventId,
            request,
            CancellationToken.None);

        ConflictObjectResult conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        ValidationProblemDetails details = Assert.IsType<ValidationProblemDetails>(conflict.Value);
        Assert.Equal(StatusCodes.Status409Conflict, details.Status);
        Assert.Equal(
            [RegistrationErrors.EventFull.Message],
            details.Errors[RegistrationErrors.EventFull.Key]);
    }

    [Fact]
    public async Task CreateRegistration_RegistrationHasClosed_ReturnsConflict()
    {
        CreateRegistrationRequest request = BuildRequest();

        this.registrationsService
            .AddRegistration(EventId, request, Arg.Any<CancellationToken>())
            .Returns(WriteResult<RegistrationResponse>.Failure(RegistrationErrors.RegistrationClosed));

        ActionResult<RegistrationResponse> result = await this.controller.CreateRegistration(
            EventId,
            request,
            CancellationToken.None);

        ConflictObjectResult conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        ValidationProblemDetails details = Assert.IsType<ValidationProblemDetails>(conflict.Value);
        Assert.Equal(StatusCodes.Status409Conflict, details.Status);
        Assert.Equal(
            [RegistrationErrors.RegistrationClosed.Message],
            details.Errors[RegistrationErrors.RegistrationClosed.Key]);
    }

    [Fact]
    public async Task CreateRegistration_NoEventCarriesTheIdentifier_ReturnsNotFound()
    {
        CreateRegistrationRequest request = BuildRequest();

        this.registrationsService
            .AddRegistration(EventId, request, Arg.Any<CancellationToken>())
            .Returns(WriteResult<RegistrationResponse>.Failure(RegistrationErrors.EventNotFound));

        ActionResult<RegistrationResponse> result = await this.controller.CreateRegistration(
            EventId,
            request,
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateRegistration_DetailsBrokeADomainRule_ReturnsTheProblemForThatFailure()
    {
        CreateRegistrationRequest request = BuildRequest();

        this.registrationsService
            .AddRegistration(EventId, request, Arg.Any<CancellationToken>())
            .Returns(WriteResult<RegistrationResponse>.Failure(RegistrationErrors.Invalid(
                "An idempotency key is required to register.",
                "IdempotencyKey")));

        ActionResult<RegistrationResponse> result = await this.controller.CreateRegistration(
            EventId,
            request,
            CancellationToken.None);

        ObjectResult problem = Assert.IsType<BadRequestObjectResult>(result.Result);
        ValidationProblemDetails details = Assert.IsType<ValidationProblemDetails>(problem.Value);
        Assert.Equal(
            ["An idempotency key is required to register."],
            details.Errors["IdempotencyKey"]);
    }

    [Fact]
    public async Task CreateRegistration_ResultCarriesNeitherARegistrationNorAnError_ThrowsUnreachableException()
    {
        CreateRegistrationRequest request = BuildRequest();

        this.registrationsService
            .AddRegistration(EventId, request, Arg.Any<CancellationToken>())
            .Returns(new WriteResult<RegistrationResponse>(null, null));

        await Assert.ThrowsAsync<UnreachableException>(
            () => this.controller.CreateRegistration(EventId, request, CancellationToken.None));
    }

    private static CreateRegistrationRequest BuildRequest() => new("Ada Lovelace", IdempotencyKey);
}
