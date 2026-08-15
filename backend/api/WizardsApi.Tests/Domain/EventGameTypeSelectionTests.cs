using Wizards.Domain.Entities;
using Wizards.Domain.Exceptions;

namespace WizardsApi.Tests.Domain;

public sealed class EventGameTypeSelectionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_KeyIsMissing_ThrowsUnkeyedDomainException(string? key)
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => EventGameTypeSelection.Create(key!, "4"));

        Assert.Equal("A game type setting key is required.", exception.Message);
        Assert.Null(exception.Key);
    }

    [Fact]
    public void Create_KeyIsLongerThanTheMaximum_ThrowsUnkeyedDomainException()
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => EventGameTypeSelection.Create(new string('a', GameTypeSetting.MaxKeyLength + 1), "4"));

        Assert.Equal(
            $"A game type setting key cannot exceed {GameTypeSetting.MaxKeyLength} characters.",
            exception.Message);
        Assert.Null(exception.Key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ValueIsMissing_ThrowsDomainExceptionKeyedToTheTrimmedKey(string? value)
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => EventGameTypeSelection.Create("  playerCount  ", value!));

        Assert.Equal("A value is required for the 'playerCount' setting.", exception.Message);
        Assert.Equal("playerCount", exception.Key);
    }

    [Fact]
    public void Create_ValueIsLongerThanTheMaximum_ThrowsDomainExceptionKeyedToTheSetting()
    {
        DomainException exception = Assert.Throws<DomainException>(() => EventGameTypeSelection.Create(
            "playerCount",
            new string('a', GameTypeSetting.MaxValueLength + 1)));

        Assert.Equal(
            $"The value chosen for the 'playerCount' setting cannot exceed {GameTypeSetting.MaxValueLength} characters.",
            exception.Message);
        Assert.Equal("playerCount", exception.Key);
    }

    [Fact]
    public void Create_KeyAndValueOnlyFitOnceTrimmed_IsAccepted()
    {
        string key = new('a', GameTypeSetting.MaxKeyLength);
        string value = new('b', GameTypeSetting.MaxValueLength);

        EventGameTypeSelection created = EventGameTypeSelection.Create($"  {key}  ", $"  {value}  ");

        Assert.Equal(key, created.Key);
        Assert.Equal(value, created.Value);
    }

    [Fact]
    public void Create_KeyAndValueCarrySurroundingWhitespace_TrimsThem()
    {
        EventGameTypeSelection created = EventGameTypeSelection.Create("  playerCount  ", "  4  ");

        Assert.Equal("playerCount", created.Key);
        Assert.Equal("4", created.Value);
    }

    [Fact]
    public void Create_ArgumentsAreValid_ReturnsSelectionCarryingThem()
    {
        EventGameTypeSelection created = EventGameTypeSelection.Create("playerCount", "4");

        Assert.Equal("playerCount", created.Key);
        Assert.Equal("4", created.Value);
        Assert.Equal(0, created.Id);
    }
}
