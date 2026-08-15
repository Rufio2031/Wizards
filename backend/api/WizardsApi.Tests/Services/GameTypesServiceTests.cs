using NSubstitute;

using Wizards.Application.DTOs.Responses;
using Wizards.Application.Services;
using Wizards.Domain.Entities;
using Wizards.Domain.Enums;
using Wizards.Domain.Interfaces.Repositories;

namespace WizardsApi.Tests.Services;

public sealed class GameTypesServiceTests
{
    private static readonly Guid GameTypeId = new("2c9e8f1a-7b3d-4c5e-9a1f-8d7c6b5a4e3f");

    private readonly IGameTypesRepository gameTypesRepository = Substitute.For<IGameTypesRepository>();

    private readonly GameTypesService gameTypesService;

    public GameTypesServiceTests()
    {
        this.gameTypesService = new GameTypesService(this.gameTypesRepository);
    }

    [Fact]
    public async Task GetGameType_GameTypeIdIsEmpty_ThrowsArgumentExceptionWithoutReadingIt()
    {
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => this.gameTypesService.GetGameType(Guid.Empty, CancellationToken.None));

        Assert.Equal("gameTypeId", exception.ParamName);
        await this.gameTypesRepository.DidNotReceiveWithAnyArgs()
            .GetGameTypeByPublicIdAsync(default, default);
    }

    [Fact]
    public async Task GetGameType_NoGameTypeCarriesTheIdentifier_ReturnsNull()
    {
        this.gameTypesRepository
            .GetGameTypeByPublicIdAsync(GameTypeId, Arg.Any<CancellationToken>())
            .Returns((GameType?)null);

        Assert.Null(await this.gameTypesService.GetGameType(GameTypeId, CancellationToken.None));
    }

    [Fact]
    public async Task GetGameType_GameTypeCarriesTheIdentifier_ProjectsItWithItsSettings()
    {
        GameType gameType = GameType.Reconstitute(1, GameTypeId, "Magic", [FormatSetting()]);

        this.gameTypesRepository
            .GetGameTypeByPublicIdAsync(GameTypeId, Arg.Any<CancellationToken>())
            .Returns(gameType);

        GameTypeTemplateResponse? response = await this.gameTypesService.GetGameType(
            GameTypeId,
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(GameTypeId, response.GameTypeId);
        Assert.Equal("Magic", response.Name);
        Assert.Equal("format", Assert.Single(response.Settings).Key);
    }

    [Fact]
    public async Task GetGameTypes_NoneAreRegistered_ReturnsAnEmptyList()
    {
        this.gameTypesRepository
            .GetGameTypesAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        Assert.Empty(await this.gameTypesService.GetGameTypes(CancellationToken.None));
    }

    [Fact]
    public async Task GetGameTypes_SomeAreRegistered_ProjectsThemInTheOrderRead()
    {
        this.gameTypesRepository
            .GetGameTypesAsync(Arg.Any<CancellationToken>())
            .Returns([
                GameType.Reconstitute(1, GameTypeId, "Magic"),
                GameType.Reconstitute(2, Guid.NewGuid(), "Pokemon")
            ]);

        IReadOnlyList<GameTypeTemplateResponse> responses =
            await this.gameTypesService.GetGameTypes(CancellationToken.None);

        Assert.Equal(new[] { "Magic", "Pokemon" }, responses.Select(response => response.Name));
    }

    private static GameTypeSetting FormatSetting() =>
        GameTypeSetting.Create(
            "format",
            "Format",
            SettingType.Enum,
            "Commander",
            null,
            null,
            null,
            "Commander",
            "Standard");
}
