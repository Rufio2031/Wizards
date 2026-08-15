using Microsoft.AspNetCore.Mvc;

using NSubstitute;

using Wizards.Api.Controllers;
using Wizards.Application.DTOs.Responses;
using Wizards.Application.Interfaces;

namespace WizardsApi.Tests.Controllers;

public sealed class GameTypesControllerTests
{
    private static readonly Guid GameTypeId = new("2c9e8f1a-7b3d-4c5e-9a1f-8d7c6b5a4e3f");

    private readonly IGameTypesService gameTypesService = Substitute.For<IGameTypesService>();

    private readonly GameTypesController controller;

    public GameTypesControllerTests()
    {
        this.controller = new GameTypesController(this.gameTypesService);
    }

    [Fact]
    public async Task GetGameType_GameTypeIdIsEmpty_ReturnsNotFoundWithoutReadingIt()
    {
        ActionResult<GameTypeTemplateResponse> result =
            await this.controller.GetGameType(Guid.Empty, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        await this.gameTypesService.DidNotReceiveWithAnyArgs().GetGameType(default, default);
    }

    [Fact]
    public async Task GetGameType_NoGameTypeCarriesTheIdentifier_ReturnsNotFound()
    {
        this.gameTypesService
            .GetGameType(GameTypeId, Arg.Any<CancellationToken>())
            .Returns((GameTypeTemplateResponse?)null);

        ActionResult<GameTypeTemplateResponse> result =
            await this.controller.GetGameType(GameTypeId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetGameType_GameTypeCarriesTheIdentifier_ReturnsIt()
    {
        GameTypeTemplateResponse gameType = new(GameTypeId, "Magic", []);

        this.gameTypesService
            .GetGameType(GameTypeId, Arg.Any<CancellationToken>())
            .Returns(gameType);

        ActionResult<GameTypeTemplateResponse> result =
            await this.controller.GetGameType(GameTypeId, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(gameType, ok.Value);
    }

    [Fact]
    public async Task GetGameTypes_NoneAreRegistered_ReturnsAnEmptyList()
    {
        this.gameTypesService
            .GetGameTypes(Arg.Any<CancellationToken>())
            .Returns([]);

        ActionResult<IReadOnlyList<GameTypeTemplateResponse>> result =
            await this.controller.GetGameTypes(CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<GameTypeTemplateResponse>>(ok.Value));
    }

    [Fact]
    public async Task GetGameTypes_SomeAreRegistered_ReturnsThem()
    {
        IReadOnlyList<GameTypeTemplateResponse> gameTypes = [new GameTypeTemplateResponse(GameTypeId, "Magic", [])];

        this.gameTypesService
            .GetGameTypes(Arg.Any<CancellationToken>())
            .Returns(gameTypes);

        ActionResult<IReadOnlyList<GameTypeTemplateResponse>> result =
            await this.controller.GetGameTypes(CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(gameTypes, ok.Value);
    }
}
