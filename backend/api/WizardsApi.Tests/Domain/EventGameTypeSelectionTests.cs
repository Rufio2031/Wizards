using Wizards.Domain.Entities;
using Wizards.Domain.Enums;
using Wizards.Domain.Exceptions;

namespace WizardsApi.Tests.Domain;

public sealed class EventGameTypeSelectionTests
{
    [Fact]
    public void Create_SettingIsNull_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => EventGameTypeSelection.Create(null!, "4"));

        Assert.Equal("setting", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ValueIsMissing_ThrowsDomainExceptionKeyedToTheSetting(string? value)
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => EventGameTypeSelection.Create(IntSetting(), value!));

        Assert.Equal("A value is required for the 'playerCount' setting.", exception.Message);
        Assert.Equal("playerCount", exception.Key);
    }

    [Fact]
    public void Create_ValueIsLongerThanTheMaximum_ThrowsDomainExceptionKeyedToTheSetting()
    {
        DomainException exception = Assert.Throws<DomainException>(() => EventGameTypeSelection.Create(
            IntSetting(),
            new string('9', GameTypeSetting.MaxValueLength + 1)));

        Assert.Equal(
            $"The value chosen for the 'playerCount' setting cannot exceed {GameTypeSetting.MaxValueLength} characters.",
            exception.Message);
        Assert.Equal("playerCount", exception.Key);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("9")]
    [InlineData("four")]
    public void Create_SettingDoesNotAcceptTheValue_ThrowsDomainExceptionKeyedToTheSetting(string value)
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => EventGameTypeSelection.Create(IntSetting(), value));

        Assert.Equal(
            "The value chosen for the 'playerCount' setting must be a whole number between 2 and 8.",
            exception.Message);
        Assert.Equal("playerCount", exception.Key);
    }

    [Fact]
    public void Create_EnumValueIsSpelledInAnotherCase_StoresTheOptionsOwnCasing()
    {
        EventGameTypeSelection created = EventGameTypeSelection.Create(EnumSetting(), "sTaNdArD");

        Assert.Equal("Standard", created.Value);
    }

    [Fact]
    public void Create_IntValueCarriesSurroundingWhitespace_StoresItTrimmed()
    {
        EventGameTypeSelection created = EventGameTypeSelection.Create(IntSetting(), "  6  ");

        Assert.Equal("6", created.Value);
    }

    [Fact]
    public void Create_IntValueCarriesLeadingZeros_StoresItAsAPlainNumber()
    {
        EventGameTypeSelection created = EventGameTypeSelection.Create(IntSetting(), "006");

        Assert.Equal("6", created.Value);
    }

    [Fact]
    public void Create_ArgumentsAreValid_ReturnsSelectionCarryingTheSettingItWasChosenFor()
    {
        GameTypeSetting setting = IntSetting();

        EventGameTypeSelection created = EventGameTypeSelection.Create(setting, "6");

        Assert.Same(setting, created.GameTypeSetting);
        Assert.Equal("6", created.Value);
        Assert.Equal(0, created.Id);
    }

    [Fact]
    public void Reconstitute_ValueWouldBeRejectedByCreate_AppliesNoValidation()
    {
        GameTypeSetting setting = IntSetting();

        EventGameTypeSelection rehydrated = EventGameTypeSelection.Reconstitute(42, setting, "  99  ");

        Assert.Equal(42, rehydrated.Id);
        Assert.Same(setting, rehydrated.GameTypeSetting);
        Assert.Equal("  99  ", rehydrated.Value);
    }

    private static GameTypeSetting IntSetting() =>
        GameTypeSetting.Create("playerCount", "Player count", SettingType.Int, "4", 2, 8);

    private static GameTypeSetting EnumSetting() => GameTypeSetting.Create(
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
