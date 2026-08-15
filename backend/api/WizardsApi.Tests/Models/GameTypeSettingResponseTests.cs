using Wizards.Application.DTOs.Responses;
using Wizards.Domain.Entities;
using Wizards.Domain.Enums;

namespace WizardsApi.Tests.Models;

public sealed class GameTypeSettingResponseTests
{
    [Fact]
    public void Constructor_SettingIsAnIntSetting_CopiesItsBoundsAndPublishesNoOptions()
    {
        GameTypeSettingResponse response = new(GameTypeSetting.Create(
            "deckSize",
            "Deck size",
            SettingType.Int,
            "60",
            40,
            250,
            "Cards in the deck."));

        Assert.Equal("deckSize", response.Key);
        Assert.Equal("Deck size", response.Label);
        Assert.Equal("Cards in the deck.", response.Description);
        Assert.Equal(SettingType.Int, response.Type);
        Assert.Equal(40, response.MinValue);
        Assert.Equal(250, response.MaxValue);
        Assert.Equal("60", response.DefaultValue);
        Assert.Empty(response.Options);
    }

    [Fact]
    public void Constructor_SettingIsAnEnumSetting_PublishesItsOptionsInOrderAndNoBounds()
    {
        GameTypeSettingResponse response = new(GameTypeSetting.Create(
            "format",
            "Format",
            SettingType.Enum,
            "Commander",
            null,
            null,
            null,
            "Commander",
            "Modern",
            "Standard"));

        Assert.Null(response.MinValue);
        Assert.Null(response.MaxValue);
        Assert.Equal(new[] { "Commander", "Modern", "Standard" }, response.Options);
    }

    [Fact]
    public void Constructor_SettingHasNoDescription_PublishesItAsNull()
    {
        GameTypeSettingResponse response = new(
            GameTypeSetting.Create("ranked", "Ranked", SettingType.Bool, "false"));

        Assert.Null(response.Description);
    }

    [Fact]
    public void Constructor_NonEnumSettingWasRehydratedWithOptions_KnownGapPublishesThemAnyway()
    {
        GameTypeSettingResponse response = new(GameTypeSetting.Reconstitute(
            1,
            "ranked",
            "Ranked",
            null,
            SettingType.Bool,
            "false",
            null,
            null,
            [GameTypeSettingOption.Reconstitute(1, "Commander")]));

        Assert.Equal(SettingType.Bool, response.Type);
        Assert.Equal(["Commander"], response.Options);
    }

    [Fact]
    public void Constructor_EnumSettingWasRehydratedWithoutItsOptions_KnownGapPublishesNoChoices()
    {
        GameTypeSettingResponse response = new(GameTypeSetting.Reconstitute(
            1,
            "format",
            "Format",
            null,
            SettingType.Enum,
            "Commander",
            null,
            null));

        Assert.Equal(SettingType.Enum, response.Type);
        Assert.Empty(response.Options);
    }
}
