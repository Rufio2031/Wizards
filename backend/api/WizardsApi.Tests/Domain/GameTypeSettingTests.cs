using Wizards.Domain.Entities;
using Wizards.Domain.Enums;
using Wizards.Domain.Exceptions;

namespace WizardsApi.Tests.Domain;

public sealed class GameTypeSettingTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_KeyIsMissing_ThrowsDomainException(string? key)
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => GameTypeSetting.Create(key!, "Deck size", SettingType.Int, "60"));

        Assert.Equal("A game type setting key is required.", exception.Message);
    }

    [Fact]
    public void Create_KeyIsLongerThanTheMaximum_ThrowsDomainException()
    {
        DomainException exception = Assert.Throws<DomainException>(() => GameTypeSetting.Create(
            new string('a', GameTypeSetting.MaxKeyLength + 1),
            "Deck size",
            SettingType.Int,
            "60"));

        Assert.Equal(
            $"A game type setting key cannot exceed {GameTypeSetting.MaxKeyLength} characters.",
            exception.Message);
    }

    [Fact]
    public void Create_KeyOnlyFitsOnceTrimmed_IsAccepted()
    {
        string key = new('a', GameTypeSetting.MaxKeyLength);

        Assert.Equal(
            key,
            GameTypeSetting.Create($"  {key}  ", "Deck size", SettingType.Int, "60").Key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_LabelIsMissing_ThrowsDomainExceptionNamingTheKey(string? label)
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => GameTypeSetting.Create("deckSize", label!, SettingType.Int, "60"));

        Assert.Equal("A label is required for the 'deckSize' setting.", exception.Message);
    }

    [Fact]
    public void Create_LabelIsLongerThanTheMaximum_ThrowsDomainExceptionNamingTheKey()
    {
        DomainException exception = Assert.Throws<DomainException>(() => GameTypeSetting.Create(
            "deckSize",
            new string('a', GameTypeSetting.MaxLabelLength + 1),
            SettingType.Int,
            "60"));

        Assert.Equal(
            $"The label of the 'deckSize' setting cannot exceed {GameTypeSetting.MaxLabelLength} characters.",
            exception.Message);
    }

    [Fact]
    public void Create_LabelCarriesSurroundingWhitespace_TrimsIt()
    {
        Assert.Equal(
            "Deck size",
            GameTypeSetting.Create("deckSize", "  Deck size  ", SettingType.Int, "60").Label);
    }

    [Fact]
    public void Create_DescriptionIsNotSupplied_LeavesItNull()
    {
        Assert.Null(GameTypeSetting.Create("deckSize", "Deck size", SettingType.Int, "60").Description);
    }

    [Fact]
    public void Create_DescriptionIsLongerThanTheMaximum_ThrowsDomainExceptionNamingTheKey()
    {
        DomainException exception = Assert.Throws<DomainException>(() => GameTypeSetting.Create(
            "deckSize",
            "Deck size",
            SettingType.Int,
            "60",
            description: new string('a', GameTypeSetting.MaxDescriptionLength + 1)));

        Assert.Equal(
            $"The description of the 'deckSize' setting cannot exceed {GameTypeSetting.MaxDescriptionLength} characters.",
            exception.Message);
    }

    [Fact]
    public void Create_DescriptionCarriesSurroundingWhitespace_TrimsIt()
    {
        GameTypeSetting setting = GameTypeSetting.Create(
            "deckSize",
            "Deck size",
            SettingType.Int,
            "60",
            description: "  Cards in the deck.  ");

        Assert.Equal("Cards in the deck.", setting.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_DescriptionIsOnlyWhitespace_LeavesItNull(string description)
    {
        GameTypeSetting setting = GameTypeSetting.Create(
            "deckSize",
            "Deck size",
            SettingType.Int,
            "60",
            description: description);

        Assert.Null(setting.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_DefaultValueIsMissing_ThrowsDomainExceptionNamingTheKey(string? defaultValue)
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => GameTypeSetting.Create("deckSize", "Deck size", SettingType.Int, defaultValue!));

        Assert.Equal("A default value is required for the 'deckSize' setting.", exception.Message);
    }

    [Fact]
    public void Create_DefaultValueIsLongerThanTheMaximum_ThrowsDomainExceptionNamingTheKey()
    {
        DomainException exception = Assert.Throws<DomainException>(() => GameTypeSetting.Create(
            "deckSize",
            "Deck size",
            SettingType.Int,
            new string('1', GameTypeSetting.MaxValueLength + 1)));

        Assert.Equal(
            $"The default value of the 'deckSize' setting cannot exceed {GameTypeSetting.MaxValueLength} characters.",
            exception.Message);
    }

    [Fact]
    public void Create_AnOptionValueIsBlank_ReportsTheOptionBeforeTheSettingsShape()
    {
        DomainException exception = Assert.Throws<DomainException>(() => GameTypeSetting.Create(
            "ranked",
            "Ranked",
            SettingType.Bool,
            "false",
            null,
            null,
            null,
            "   "));

        Assert.Equal("A game type setting option value is required.", exception.Message);
    }

    [Fact]
    public void Create_TypeIsNotADefinedSettingType_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => GameTypeSetting.Create("deckSize", "Deck size", (SettingType)99, "60"));

        Assert.Equal("type", exception.ParamName);
        Assert.StartsWith("The 'deckSize' setting has an unrecognized type.", exception.Message);
    }

    [Fact]
    public void Create_IntSettingHasAMinimumAboveItsMaximum_ThrowsDomainException()
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => GameTypeSetting.Create("deckSize", "Deck size", SettingType.Int, "60", 250, 40));

        Assert.Equal(
            "The minimum value of the 'deckSize' setting cannot exceed its maximum value.",
            exception.Message);
    }

    [Fact]
    public void Create_IntSettingHasEqualBounds_IsAccepted()
    {
        GameTypeSetting setting = GameTypeSetting.Create(
            "deckSize",
            "Deck size",
            SettingType.Int,
            "60",
            60,
            60);

        Assert.Equal(60, setting.MinValue);
        Assert.Equal(60, setting.MaxValue);
    }

    [Theory]
    [InlineData(SettingType.Bool, "false")]
    [InlineData(SettingType.Enum, "Commander")]
    public void Create_NonIntSettingCarriesAMinimum_ThrowsDomainException(
        SettingType type,
        string defaultValue)
    {
        DomainException exception = Assert.Throws<DomainException>(() => GameTypeSetting.Create(
            "setting",
            "Setting",
            type,
            defaultValue,
            minValue: 1));

        Assert.Equal(
            "The 'setting' setting is not a whole number setting and cannot carry bounds.",
            exception.Message);
    }

    [Theory]
    [InlineData(SettingType.Bool, "false")]
    [InlineData(SettingType.Enum, "Commander")]
    public void Create_NonIntSettingCarriesAMaximum_ThrowsDomainException(
        SettingType type,
        string defaultValue)
    {
        DomainException exception = Assert.Throws<DomainException>(() => GameTypeSetting.Create(
            "setting",
            "Setting",
            type,
            defaultValue,
            maxValue: 1));

        Assert.Equal(
            "The 'setting' setting is not a whole number setting and cannot carry bounds.",
            exception.Message);
    }

    [Fact]
    public void Create_EnumSettingListsNoOptions_ThrowsDomainException()
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => GameTypeSetting.Create("format", "Format", SettingType.Enum, "Commander"));

        Assert.Equal(
            "The 'format' setting is a choice setting and must list at least one option.",
            exception.Message);
    }

    [Fact]
    public void Create_EnumSettingListsTheSameOptionIgnoringCase_ThrowsDomainException()
    {
        DomainException exception = Assert.Throws<DomainException>(() => GameTypeSetting.Create(
            "format",
            "Format",
            SettingType.Enum,
            "Commander",
            null,
            null,
            null,
            "Commander",
            "COMMANDER"));

        Assert.Equal("The 'format' setting cannot list the same option twice.", exception.Message);
    }

    [Theory]
    [InlineData(SettingType.Int, "60")]
    [InlineData(SettingType.Bool, "false")]
    public void Create_NonEnumSettingCarriesOptions_ThrowsDomainException(
        SettingType type,
        string defaultValue)
    {
        DomainException exception = Assert.Throws<DomainException>(() => GameTypeSetting.Create(
            "setting",
            "Setting",
            type,
            defaultValue,
            null,
            null,
            null,
            "Commander"));

        Assert.Equal(
            "The 'setting' setting is not a choice setting and cannot carry options.",
            exception.Message);
    }

    [Theory]
    [InlineData("39")]
    [InlineData("251")]
    [InlineData("sixty")]
    [InlineData("7.0")]
    public void Create_IntDefaultValueIsNotAccepted_ThrowsDomainExceptionDescribingTheBounds(
        string defaultValue)
    {
        DomainException exception = Assert.Throws<DomainException>(() => GameTypeSetting.Create(
            "deckSize",
            "Deck size",
            SettingType.Int,
            defaultValue,
            40,
            250));

        Assert.Equal(
            "The default value of the 'deckSize' setting must be a whole number between 40 and 250.",
            exception.Message);
    }

    [Fact]
    public void Create_BoolDefaultValueIsNotAccepted_ThrowsDomainExceptionDescribingTheAllowedValues()
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => GameTypeSetting.Create("ranked", "Ranked", SettingType.Bool, "yes"));

        Assert.Equal(
            "The default value of the 'ranked' setting must be true or false.",
            exception.Message);
    }

    [Fact]
    public void Create_EnumDefaultValueIsNotAnOption_ThrowsDomainExceptionListingTheOptions()
    {
        DomainException exception = Assert.Throws<DomainException>(() => GameTypeSetting.Create(
            "format",
            "Format",
            SettingType.Enum,
            "Pauper",
            null,
            null,
            null,
            "Commander",
            "Standard"));

        Assert.Equal(
            "The default value of the 'format' setting must be one of: Commander, Standard.",
            exception.Message);
    }

    [Fact]
    public void Create_ArgumentsAreValid_ReturnsSettingDescribedByTheArguments()
    {
        GameTypeSetting setting = GameTypeSetting.Create(
            "deckSize",
            "Deck size",
            SettingType.Int,
            "60",
            40,
            250,
            "Cards in the deck.");

        Assert.Equal("deckSize", setting.Key);
        Assert.Equal("Deck size", setting.Label);
        Assert.Equal("Cards in the deck.", setting.Description);
        Assert.Equal(SettingType.Int, setting.Type);
        Assert.Equal(40, setting.MinValue);
        Assert.Equal(250, setting.MaxValue);
        Assert.Equal("60", setting.DefaultValue);
        Assert.Empty(setting.Options);
        Assert.Equal(0, setting.Id);
    }

    [Fact]
    public void Create_EnumSettingListsOptions_KeepsThemInTheOrderSupplied()
    {
        GameTypeSetting setting = EnumSetting();

        Assert.Equal(
            new[] { "Commander", "Modern", "Standard" },
            setting.Options.Select(option => option.Value));
    }

    [Theory]
    [InlineData("007", "7")]
    [InlineData("+7", "7")]
    [InlineData("  60  ", "60")]
    [InlineData("-3", "-3")]
    public void Create_IntDefaultValueIsSpelledLoosely_StoresItInItsCanonicalForm(
        string defaultValue,
        string expected)
    {
        GameTypeSetting setting = GameTypeSetting.Create(
            "deckSize",
            "Deck size",
            SettingType.Int,
            defaultValue,
            -10,
            250);

        Assert.Equal(expected, setting.DefaultValue);
    }

    [Theory]
    [InlineData("TRUE", "true")]
    [InlineData("True", "true")]
    [InlineData("  FaLsE  ", "false")]
    public void Create_BoolDefaultValueIsSpelledDifferently_StoresItInLowerCase(
        string defaultValue,
        string expected)
    {
        Assert.Equal(
            expected,
            GameTypeSetting.Create("ranked", "Ranked", SettingType.Bool, defaultValue).DefaultValue);
    }

    [Fact]
    public void Create_EnumDefaultValueIsSpelledInAnotherCase_StoresTheOptionsOwnCasing()
    {
        GameTypeSetting setting = GameTypeSetting.Create(
            "format",
            "Format",
            SettingType.Enum,
            "sTaNdArD",
            null,
            null,
            null,
            "Commander",
            "Modern",
            "Standard");

        Assert.Equal("Standard", setting.DefaultValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Accepts_ValueIsMissing_ReturnsFalseForEveryKind(string? value)
    {
        Assert.False(IntSetting().Accepts(value));
        Assert.False(BoolSetting().Accepts(value));
        Assert.False(EnumSetting().Accepts(value));
    }

    [Theory]
    [InlineData("60")]
    [InlineData("  60  ")]
    [InlineData("007")]
    [InlineData("+7")]
    [InlineData("-10")]
    public void Accepts_IntValueIsAWholeNumberInRange_ReturnsTrue(string value)
    {
        Assert.True(UnboundedIntSetting().Accepts(value));
    }

    [Theory]
    [InlineData("7.0")]
    [InlineData("1,000")]
    [InlineData("1e3")]
    [InlineData("0x10")]
    [InlineData("sixty")]
    [InlineData("6 0")]
    public void Accepts_IntValueIsNotAWholeNumber_ReturnsFalse(string value)
    {
        Assert.False(UnboundedIntSetting().Accepts(value));
    }

    [Theory]
    [InlineData("2147483648")]
    [InlineData("-2147483649")]
    public void Accepts_IntValueOverflowsTheRangeOfAnInt_ReturnsFalse(string value)
    {
        Assert.False(UnboundedIntSetting().Accepts(value));
    }

    [Theory]
    [InlineData("40", true)]
    [InlineData("250", true)]
    [InlineData("39", false)]
    [InlineData("251", false)]
    public void Accepts_IntSettingIsBoundedAtBothEnds_AcceptsOnlyValuesWithinTheBounds(
        string value,
        bool expected)
    {
        Assert.Equal(expected, IntSetting().Accepts(value));
    }

    [Theory]
    [InlineData("40", true)]
    [InlineData("39", false)]
    [InlineData("2147483647", true)]
    public void Accepts_IntSettingIsBoundedBelowOnly_AcceptsAnythingAtOrAboveTheMinimum(
        string value,
        bool expected)
    {
        GameTypeSetting setting = GameTypeSetting.Create(
            "deckSize",
            "Deck size",
            SettingType.Int,
            "60",
            minValue: 40);

        Assert.Equal(expected, setting.Accepts(value));
    }

    [Theory]
    [InlineData("250", true)]
    [InlineData("251", false)]
    [InlineData("-2147483648", true)]
    public void Accepts_IntSettingIsBoundedAboveOnly_AcceptsAnythingAtOrBelowTheMaximum(
        string value,
        bool expected)
    {
        GameTypeSetting setting = GameTypeSetting.Create(
            "deckSize",
            "Deck size",
            SettingType.Int,
            "60",
            maxValue: 250);

        Assert.Equal(expected, setting.Accepts(value));
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("TRUE")]
    [InlineData("False")]
    [InlineData("  tRuE  ")]
    public void Accepts_BoolValueIsTrueOrFalseInAnyCasing_ReturnsTrue(string value)
    {
        Assert.True(BoolSetting().Accepts(value));
    }

    [Theory]
    [InlineData("yes")]
    [InlineData("no")]
    [InlineData("1")]
    [InlineData("0")]
    [InlineData("t")]
    public void Accepts_BoolValueIsNotTrueOrFalse_ReturnsFalse(string value)
    {
        Assert.False(BoolSetting().Accepts(value));
    }

    [Theory]
    [InlineData("Commander")]
    [InlineData("sTaNdArD")]
    [InlineData("  MODERN  ")]
    public void Accepts_EnumValueMatchesAnOptionIgnoringCase_ReturnsTrue(string value)
    {
        Assert.True(EnumSetting().Accepts(value));
    }

    [Theory]
    [InlineData("Pauper")]
    [InlineData("Comm ander")]
    [InlineData("Commanders")]
    public void Accepts_EnumValueMatchesNoOption_ReturnsFalse(string value)
    {
        Assert.False(EnumSetting().Accepts(value));
    }

    [Fact]
    public void Accepts_TypeIsNotADefinedSettingType_ReturnsFalseForEveryValue()
    {
        GameTypeSetting setting = Rehydrated(type: (SettingType)99, defaultValue: "60");

        Assert.False(setting.Accepts("60"));
        Assert.False(setting.Accepts("true"));
        Assert.False(setting.Accepts("anything"));
    }

    [Fact]
    public void Accepts_EnumSettingWasRehydratedWithoutItsOptions_KnownGapRejectsEveryValue()
    {
        GameTypeSetting setting = Rehydrated(type: SettingType.Enum, defaultValue: "Commander");

        Assert.False(setting.Accepts("Commander"));
        Assert.False(setting.Accepts(setting.DefaultValue));
    }

    [Fact]
    public void Accepts_NonEnumSettingWasRehydratedWithOptions_KnownGapIgnoresThem()
    {
        GameTypeSetting setting = Rehydrated(
            type: SettingType.Bool,
            defaultValue: "false",
            options: [GameTypeSettingOption.Reconstitute(1, "Commander")]);

        Assert.False(setting.Accepts("Commander"));
        Assert.True(setting.Accepts("true"));
    }

    [Fact]
    public void DescribeAllowedValues_IntSettingIsUnbounded_NamesNoBounds()
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => GameType.Create("Magic", [UnboundedIntSetting()])
                .Validate(new Dictionary<string, string> { ["deckSize"] = "sixty" }));

        Assert.Equal("The Magic 'deckSize' setting must be a whole number.", exception.Message);
    }

    [Fact]
    public void DescribeAllowedValues_IntSettingIsBoundedBelowOnly_NamesTheMinimum()
    {
        GameTypeSetting setting = GameTypeSetting.Create(
            "deckSize",
            "Deck size",
            SettingType.Int,
            "60",
            minValue: 40);

        DomainException exception = Assert.Throws<DomainException>(
            () => GameType.Create("Magic", [setting])
                .Validate(new Dictionary<string, string> { ["deckSize"] = "39" }));

        Assert.Equal(
            "The Magic 'deckSize' setting must be a whole number of at least 40.",
            exception.Message);
    }

    [Fact]
    public void DescribeAllowedValues_IntSettingIsBoundedAboveOnly_NamesTheMaximum()
    {
        GameTypeSetting setting = GameTypeSetting.Create(
            "deckSize",
            "Deck size",
            SettingType.Int,
            "60",
            maxValue: 250);

        DomainException exception = Assert.Throws<DomainException>(
            () => GameType.Create("Magic", [setting])
                .Validate(new Dictionary<string, string> { ["deckSize"] = "251" }));

        Assert.Equal(
            "The Magic 'deckSize' setting must be a whole number of at most 250.",
            exception.Message);
    }

    [Fact]
    public void DescribeAllowedValues_TypeIsNotADefinedSettingType_FallsBackToASupportedValue()
    {
        GameTypeSetting setting = Rehydrated(type: (SettingType)99, defaultValue: "60");

        DomainException exception = Assert.Throws<DomainException>(
            () => GameType.Reconstitute(1, Guid.NewGuid(), "Magic", [setting])
                .Validate(new Dictionary<string, string> { ["deckSize"] = "60" }));

        Assert.Equal("The Magic 'deckSize' setting must be a supported value.", exception.Message);
    }

    [Fact]
    public void DescribeAllowedValues_EnumSettingWasRehydratedWithoutItsOptions_KnownGapEndsInABareColon()
    {
        GameTypeSetting setting = Rehydrated(
            key: "format",
            type: SettingType.Enum,
            defaultValue: "Commander");

        DomainException exception = Assert.Throws<DomainException>(
            () => GameType.Reconstitute(1, Guid.NewGuid(), "Magic", [setting])
                .Validate(new Dictionary<string, string> { ["format"] = "Commander" }));

        Assert.Equal("The Magic 'format' setting must be one of: .", exception.Message);
    }

    [Fact]
    public void Reconstitute_StateWouldBeRejectedByCreate_AppliesNoValidation()
    {
        GameTypeSetting rehydrated = GameTypeSetting.Reconstitute(
            42,
            "  ",
            new string('a', GameTypeSetting.MaxLabelLength + 1),
            new string('a', GameTypeSetting.MaxDescriptionLength + 1),
            SettingType.Bool,
            "not a bool",
            250,
            40,
            [GameTypeSettingOption.Reconstitute(7, "Commander")]);

        Assert.Equal(42, rehydrated.Id);
        Assert.Equal("  ", rehydrated.Key);
        Assert.Equal(SettingType.Bool, rehydrated.Type);
        Assert.Equal("not a bool", rehydrated.DefaultValue);
        Assert.Equal(250, rehydrated.MinValue);
        Assert.Equal(40, rehydrated.MaxValue);
        Assert.Single(rehydrated.Options);
    }

    [Fact]
    public void Reconstitute_OptionsAreNotSupplied_ExposesNoOptions()
    {
        Assert.Empty(Rehydrated(type: SettingType.Enum, defaultValue: "Commander").Options);
    }

    private static GameTypeSetting Rehydrated(
        string key = "deckSize",
        SettingType type = SettingType.Int,
        string defaultValue = "60",
        IEnumerable<GameTypeSettingOption>? options = null) =>
        GameTypeSetting.Reconstitute(1, key, "Setting", null, type, defaultValue, null, null, options);

    private static GameTypeSetting IntSetting() =>
        GameTypeSetting.Create("deckSize", "Deck size", SettingType.Int, "60", 40, 250);

    private static GameTypeSetting UnboundedIntSetting() =>
        GameTypeSetting.Create("deckSize", "Deck size", SettingType.Int, "60");

    private static GameTypeSetting BoolSetting() =>
        GameTypeSetting.Create("ranked", "Ranked", SettingType.Bool, "false");

    private static GameTypeSetting EnumSetting() =>
        GameTypeSetting.Create(
            "format",
            "Format",
            SettingType.Enum,
            "Commander",
            null,
            null,
            null,
            "Commander",
            "Modern",
            "Standard");
}
