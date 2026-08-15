using Wizards.Domain.Entities;
using Wizards.Domain.Exceptions;

namespace WizardsApi.Tests.Domain;

public sealed class GameTypeSettingOptionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ValueIsMissing_ThrowsDomainException(string? value)
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => GameTypeSettingOption.Create(value!));

        Assert.Equal("A game type setting option value is required.", exception.Message);
    }

    [Fact]
    public void Create_ValueIsLongerThanTheMaximum_ThrowsDomainException()
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => GameTypeSettingOption.Create(new string('a', GameTypeSetting.MaxValueLength + 1)));

        Assert.Equal(
            $"A game type setting option value cannot exceed {GameTypeSetting.MaxValueLength} characters.",
            exception.Message);
    }

    [Fact]
    public void Create_ValueOnlyFitsOnceTrimmed_IsAccepted()
    {
        string value = new('a', GameTypeSetting.MaxValueLength);

        Assert.Equal(value, GameTypeSettingOption.Create($"  {value}  ").Value);
    }

    [Fact]
    public void Create_ValueCarriesSurroundingWhitespace_TrimsIt()
    {
        Assert.Equal("Commander", GameTypeSettingOption.Create("  Commander  ").Value);
    }

    [Fact]
    public void Create_ValueIsValid_KeepsItsCasingAndCarriesNoPrimaryKey()
    {
        GameTypeSettingOption option = GameTypeSettingOption.Create("cEDH");

        Assert.Equal("cEDH", option.Value);
        Assert.Equal(0, option.Id);
    }

    [Fact]
    public void Reconstitute_StateWouldBeRejectedByCreate_AppliesNoValidation()
    {
        GameTypeSettingOption rehydrated = GameTypeSettingOption.Reconstitute(42, "   ");

        Assert.Equal(42, rehydrated.Id);
        Assert.Equal("   ", rehydrated.Value);
    }
}
