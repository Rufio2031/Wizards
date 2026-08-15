using Wizards.Domain.Entities;
using Wizards.Domain.Enums;
using Wizards.Domain.Exceptions;

namespace WizardsApi.Tests.Domain;

public sealed class GameTypeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NameIsMissing_ThrowsUnkeyedDomainException(string? name)
    {
        DomainException exception = Assert.Throws<DomainException>(() => GameType.Create(name!));

        Assert.Equal("Game type name is required.", exception.Message);
        Assert.Null(exception.Key);
    }

    [Fact]
    public void Create_NameIsLongerThanTheMaximum_ThrowsUnkeyedDomainException()
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => GameType.Create(new string('a', GameType.MaxNameLength + 1)));

        Assert.Equal(
            $"Game type name cannot exceed {GameType.MaxNameLength} characters.",
            exception.Message);
        Assert.Null(exception.Key);
    }

    [Fact]
    public void Create_NameOnlyFitsOnceTrimmed_IsAccepted()
    {
        string name = new('a', GameType.MaxNameLength);

        Assert.Equal(name, GameType.Create($"   {name}   ").Name);
    }

    [Fact]
    public void Create_NameCarriesSurroundingWhitespace_TrimsIt()
    {
        Assert.Equal("Magic", GameType.Create("  Magic  ").Name);
    }

    [Fact]
    public void Create_SettingsAreNotSupplied_ExposesNoSettings()
    {
        Assert.Empty(GameType.Create("Magic").Settings);
    }

    [Fact]
    public void Create_SettingsContainANullEntry_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => GameType.Create("Magic", [null!]));

        Assert.Equal("settings", exception.ParamName);
        Assert.StartsWith("A game type setting cannot be null.", exception.Message);
    }

    [Fact]
    public void Create_SettingsNameTheSameKeyIgnoringCase_ThrowsUnkeyedDomainException()
    {
        DomainException exception = Assert.Throws<DomainException>(() => GameType.Create(
            "Magic",
            [IntSetting(), IntSetting("PLAYERCOUNT")]));

        Assert.Equal(
            "Game type 'Magic' cannot expose the 'PLAYERCOUNT' setting twice.",
            exception.Message);
        Assert.Null(exception.Key);
    }

    [Fact]
    public void Create_ArgumentsAreValid_ReturnsGameTypeDescribedByTheArguments()
    {
        GameTypeSetting playerCount = IntSetting();
        GameTypeSetting ranked = BoolSetting();

        GameType created = GameType.Create("Magic", [playerCount, ranked]);

        Assert.Equal("Magic", created.Name);
        Assert.Equal(new[] { playerCount, ranked }, created.Settings);
        Assert.NotEqual(Guid.Empty, created.PublicId);
        Assert.Equal(0, created.Id);
    }

    [Fact]
    public void Create_TwoGameTypesAreCreated_AssignsEachADistinctIdentifier()
    {
        Assert.NotEqual(GameType.Create("Magic").PublicId, GameType.Create("Magic").PublicId);
    }

    [Fact]
    public void Reconstitute_StateWouldBeRejectedByCreate_AppliesNoValidation()
    {
        GameType rehydrated = GameType.Reconstitute(
            42,
            Guid.Empty,
            new string('a', GameType.MaxNameLength + 1),
            [IntSetting(), IntSetting()]);

        Assert.Equal(42, rehydrated.Id);
        Assert.Equal(Guid.Empty, rehydrated.PublicId);
        Assert.Equal(GameType.MaxNameLength + 1, rehydrated.Name.Length);
        Assert.Equal(2, rehydrated.Settings.Count);
    }

    [Fact]
    public void Validate_SelectionsAreNull_ReturnsEverySettingAtItsDefault()
    {
        GameTypeSetting playerCount = IntSetting();
        GameTypeSetting ranked = BoolSetting();
        GameTypeSetting format = EnumSetting();

        GameType gameType = GameType.Create("Magic", [playerCount, ranked, format]);

        IReadOnlyList<EventGameTypeSelection> validated = gameType.Validate(null);

        Assert.Equal(
            new[] { playerCount, ranked, format },
            validated.Select(selection => selection.GameTypeSetting));
        Assert.Equal(
            new[] { "4", "false", "Commander" },
            validated.Select(selection => selection.Value));
    }

    [Fact]
    public void Validate_SelectionsAreEmpty_ReturnsEverySettingAtItsDefault()
    {
        GameTypeSetting playerCount = IntSetting();

        GameType gameType = GameType.Create("Magic", [playerCount]);

        EventGameTypeSelection validated = Assert.Single(
            gameType.Validate(new Dictionary<string, string>()));

        Assert.Same(playerCount, validated.GameTypeSetting);
        Assert.Equal("4", validated.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ChosenKeyIsBlank_ThrowsUnkeyedDomainException(string key)
    {
        GameType gameType = GameType.Create("Magic", [IntSetting()]);

        DomainException exception = Assert.Throws<DomainException>(
            () => gameType.Validate(new Dictionary<string, string> { [key] = "6" }));

        Assert.Equal("A game type setting key is required.", exception.Message);
        Assert.Null(exception.Key);
    }

    [Fact]
    public void Validate_ChosenKeyIsLongerThanTheMaximum_ThrowsUnkeyedDomainException()
    {
        GameType gameType = GameType.Create("Magic", [IntSetting()]);

        string key = new('a', GameTypeSetting.MaxKeyLength + 1);

        DomainException exception = Assert.Throws<DomainException>(
            () => gameType.Validate(new Dictionary<string, string> { [key] = "6" }));

        Assert.Equal(
            $"A game type setting key cannot exceed {GameTypeSetting.MaxKeyLength} characters.",
            exception.Message);
        Assert.Null(exception.Key);
    }

    [Fact]
    public void Validate_AKeyIsBlankAndAnotherValueIsRejected_ReportsTheBlankKeyFirst()
    {
        GameType gameType = GameType.Create("Magic", [IntSetting()]);

        DomainException exception = Assert.Throws<DomainException>(
            () => gameType.Validate(new Dictionary<string, string>
            {
                ["playerCount"] = "99",
                ["   "] = "6"
            }));

        Assert.Equal("A game type setting key is required.", exception.Message);
    }

    [Fact]
    public void Validate_ChosenKeyCarriesSurroundingWhitespace_MatchesTheSetting()
    {
        GameTypeSetting playerCount = IntSetting();

        GameType gameType = GameType.Create("Magic", [playerCount]);

        EventGameTypeSelection validated = Assert.Single(
            gameType.Validate(new Dictionary<string, string> { ["  playerCount  "] = "6" }));

        Assert.Same(playerCount, validated.GameTypeSetting);
        Assert.Equal("6", validated.Value);
    }

    [Fact]
    public void Validate_ChosenValueIsNull_ThrowsDomainExceptionKeyedToTheSetting()
    {
        GameType gameType = GameType.Create("Magic", [IntSetting()]);

        DomainException exception = Assert.Throws<DomainException>(
            () => gameType.Validate(new Dictionary<string, string> { ["playerCount"] = null! }));

        Assert.Equal(
            "The Magic 'playerCount' setting must be a whole number between 2 and 8.",
            exception.Message);
        Assert.Equal("playerCount", exception.Key);
    }

    [Fact]
    public void Validate_OnlySomeSettingsAreChosen_FillsTheRestWithTheirDefaults()
    {
        GameTypeSetting playerCount = IntSetting();
        GameTypeSetting ranked = BoolSetting();

        GameType gameType = GameType.Create("Magic", [playerCount, ranked]);

        IReadOnlyList<EventGameTypeSelection> validated = gameType.Validate(
            new Dictionary<string, string> { ["ranked"] = "true" });

        Assert.Equal(
            new[] { playerCount, ranked },
            validated.Select(selection => selection.GameTypeSetting));
        Assert.Equal(new[] { "4", "true" }, validated.Select(selection => selection.Value));
    }

    [Fact]
    public void Validate_TwoSettingsAcceptTheSameValue_PairsEachValueWithTheSettingItWasChosenFor()
    {
        GameTypeSetting minimumPlayers = IntSetting("minimumPlayers");
        GameTypeSetting maximumPlayers = IntSetting("maximumPlayers");

        GameType gameType = GameType.Create("Magic", [minimumPlayers, maximumPlayers]);

        IReadOnlyList<EventGameTypeSelection> validated = gameType.Validate(
            new Dictionary<string, string>
            {
                ["maximumPlayers"] = "8",
                ["minimumPlayers"] = "2"
            });

        Assert.Collection(
            validated,
            selection =>
            {
                Assert.Same(minimumPlayers, selection.GameTypeSetting);
                Assert.Equal("2", selection.Value);
            },
            selection =>
            {
                Assert.Same(maximumPlayers, selection.GameTypeSetting);
                Assert.Equal("8", selection.Value);
            });
    }

    [Theory]
    [InlineData("1")]
    [InlineData("9")]
    [InlineData("four")]
    public void Validate_IntValueIsOutsideWhatTheSettingAllows_ThrowsDomainExceptionKeyedToTheSetting(
        string chosen)
    {
        GameType gameType = GameType.Create("Magic", [IntSetting()]);

        DomainException exception = Assert.Throws<DomainException>(
            () => gameType.Validate(new Dictionary<string, string> { ["playerCount"] = chosen }));

        Assert.Equal(
            "The Magic 'playerCount' setting must be a whole number between 2 and 8.",
            exception.Message);
        Assert.Equal("playerCount", exception.Key);
    }

    [Fact]
    public void Validate_BoolValueIsNotTrueOrFalse_ThrowsDomainExceptionKeyedToTheSetting()
    {
        GameType gameType = GameType.Create("Magic", [BoolSetting()]);

        DomainException exception = Assert.Throws<DomainException>(
            () => gameType.Validate(new Dictionary<string, string> { ["ranked"] = "yes" }));

        Assert.Equal("The Magic 'ranked' setting must be true or false.", exception.Message);
        Assert.Equal("ranked", exception.Key);
    }

    [Fact]
    public void Validate_EnumValueIsNotAnOption_ThrowsDomainExceptionKeyedToTheSetting()
    {
        GameType gameType = GameType.Create("Magic", [EnumSetting()]);

        DomainException exception = Assert.Throws<DomainException>(
            () => gameType.Validate(new Dictionary<string, string> { ["format"] = "Pauper" }));

        Assert.Equal(
            "The Magic 'format' setting must be one of: Commander, Standard.",
            exception.Message);
        Assert.Equal("format", exception.Key);
    }

    [Fact]
    public void Validate_ChosenKeyNamesASettingIgnoringCase_MatchesTheSetting()
    {
        GameTypeSetting playerCount = IntSetting();

        GameType gameType = GameType.Create("Magic", [playerCount]);

        EventGameTypeSelection validated = Assert.Single(
            gameType.Validate(new Dictionary<string, string> { ["PLAYERCOUNT"] = "6" }));

        Assert.Same(playerCount, validated.GameTypeSetting);
        Assert.Equal("6", validated.Value);
    }

    [Fact]
    public void Validate_IntValueCarriesLeadingZeros_ReturnsItInTheStoredForm()
    {
        GameType gameType = GameType.Create("Magic", [IntSetting()]);

        EventGameTypeSelection validated = Assert.Single(
            gameType.Validate(new Dictionary<string, string> { ["playerCount"] = "006" }));

        Assert.Equal("6", validated.Value);
    }

    [Theory]
    [InlineData("TRUE", "true")]
    [InlineData("False", "false")]
    public void Validate_BoolValueIsSpelledDifferently_ReturnsItInTheStoredForm(
        string chosen,
        string expected)
    {
        GameType gameType = GameType.Create("Magic", [BoolSetting()]);

        EventGameTypeSelection validated = Assert.Single(
            gameType.Validate(new Dictionary<string, string> { ["ranked"] = chosen }));

        Assert.Equal(expected, validated.Value);
    }

    [Fact]
    public void Validate_EnumValueIsSpelledInAnotherCase_ReturnsTheOptionsOwnCasing()
    {
        GameType gameType = GameType.Create("Magic", [EnumSetting()]);

        EventGameTypeSelection validated = Assert.Single(
            gameType.Validate(new Dictionary<string, string> { ["format"] = "sTaNdArD" }));

        Assert.Equal("Standard", validated.Value);
    }

    [Fact]
    public void Validate_ChosenKeyNamesASettingTheGameTypeDoesNotExpose_ThrowsDomainExceptionKeyedToIt()
    {
        GameType gameType = GameType.Create("Magic", [IntSetting()]);

        DomainException exception = Assert.Throws<DomainException>(
            () => gameType.Validate(new Dictionary<string, string>
            {
                ["playerCount"] = "6",
                ["deckSize"] = "60"
            }));

        Assert.Equal("Magic has no 'deckSize' setting.", exception.Message);
        Assert.Equal("deckSize", exception.Key);
    }

    [Fact]
    public void Validate_GameTypeExposesNoSettingsAndOneIsChosen_ThrowsDomainExceptionKeyedToIt()
    {
        GameType gameType = GameType.Create("Magic");

        DomainException exception = Assert.Throws<DomainException>(
            () => gameType.Validate(new Dictionary<string, string> { ["deckSize"] = "60" }));

        Assert.Equal("Magic has no 'deckSize' setting.", exception.Message);
        Assert.Equal("deckSize", exception.Key);
    }

    [Fact]
    public void Validate_RejectedValueAndUnknownKeyAreBothPresent_ReportsTheRejectedValue()
    {
        GameType gameType = GameType.Create("Magic", [IntSetting()]);

        DomainException exception = Assert.Throws<DomainException>(
            () => gameType.Validate(new Dictionary<string, string>
            {
                ["playerCount"] = "99",
                ["deckSize"] = "60"
            }));

        Assert.Equal("playerCount", exception.Key);
    }

    [Fact]
    public void Validate_GameTypeExposesNoSettingsAndNoneAreChosen_ReturnsNothing()
    {
        Assert.Empty(GameType.Create("Magic").Validate(null));
    }

    [Fact]
    public void Validate_EverySettingIsChosen_ReturnsThemInTheOrderTheGameTypeExposesThem()
    {
        GameTypeSetting playerCount = IntSetting();
        GameTypeSetting ranked = BoolSetting();
        GameTypeSetting format = EnumSetting();

        GameType gameType = GameType.Create("Magic", [playerCount, ranked, format]);

        IReadOnlyList<EventGameTypeSelection> validated = gameType.Validate(
            new Dictionary<string, string>
            {
                ["format"] = "Standard",
                ["ranked"] = "true",
                ["playerCount"] = "6"
            });

        Assert.Equal(
            new[] { playerCount, ranked, format },
            validated.Select(selection => selection.GameTypeSetting));
        Assert.Equal(
            new[] { "6", "true", "Standard" },
            validated.Select(selection => selection.Value));
    }

    private static GameTypeSetting IntSetting(string key = "playerCount") =>
        GameTypeSetting.Create(key, "Player count", SettingType.Int, "4", 2, 8);

    private static GameTypeSetting BoolSetting(string key = "ranked") =>
        GameTypeSetting.Create(key, "Ranked", SettingType.Bool, "false");

    private static GameTypeSetting EnumSetting(string key = "format") =>
        GameTypeSetting.Create(
            key,
            "Format",
            SettingType.Enum,
            "Commander",
            null,
            null,
            null,
            "Commander",
            "Standard");
}
