using Wizards.Domain.Entities;
using Wizards.Domain.Enums;
using Wizards.Domain.Exceptions;

namespace WizardsApi.Tests.Domain;

public sealed class EventTests
{
    private static readonly DateTime FutureStart = DateTime.UtcNow.AddDays(7);

    private static readonly DateTime FutureEnd = FutureStart.AddHours(3);

    [Fact]
    public void Create_GameTypeIsNull_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Event.Create(
            "Friday Night Magic",
            null,
            "The Back Room",
            null!,
            FutureStart,
            FutureEnd,
            8));

        Assert.Equal("gameType", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NameIsMissing_ThrowsDomainExceptionKeyedToTheName(string? name)
    {
        DomainException exception = Assert.Throws<DomainException>(() => Event.Create(
            name!,
            null,
            "The Back Room",
            AnyGameType(),
            FutureStart,
            FutureEnd,
            8));

        Assert.Equal("Event name is required.", exception.Message);
        Assert.Equal("Name", exception.Key);
    }

    [Fact]
    public void Create_NameIsLongerThanTheMaximum_ThrowsDomainExceptionKeyedToTheName()
    {
        DomainException exception = Assert.Throws<DomainException>(() => Event.Create(
            new string('a', Event.MaxNameLength + 1),
            null,
            "The Back Room",
            AnyGameType(),
            FutureStart,
            FutureEnd,
            8));

        Assert.Equal($"Event name cannot exceed {Event.MaxNameLength} characters.", exception.Message);
        Assert.Equal("Name", exception.Key);
    }

    [Fact]
    public void Create_NameOnlyFitsOnceTrimmed_IsAccepted()
    {
        string name = new('a', Event.MaxNameLength);

        Event created = Event.Create(
            $"   {name}   ",
            null,
            "The Back Room",
            AnyGameType(),
            FutureStart,
            FutureEnd,
            8);

        Assert.Equal(name, created.Name);
    }

    [Fact]
    public void Create_DescriptionIsNull_LeavesTheDescriptionUnset()
    {
        Event created = Event.Create(
            "Friday Night Magic",
            null,
            "The Back Room",
            AnyGameType(),
            FutureStart,
            FutureEnd,
            8);

        Assert.Null(created.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_DescriptionIsOnlyWhitespace_LeavesTheDescriptionUnsetRatherThanEmpty(
        string description)
    {
        Event created = Event.Create(
            "Friday Night Magic",
            description,
            "The Back Room",
            AnyGameType(),
            FutureStart,
            FutureEnd,
            8);

        Assert.Null(created.Description);
    }

    [Fact]
    public void Create_DescriptionIsLongerThanTheMaximum_ThrowsDomainExceptionKeyedToTheDescription()
    {
        DomainException exception = Assert.Throws<DomainException>(() => Event.Create(
            "Friday Night Magic",
            new string('a', Event.MaxDescriptionLength + 1),
            "The Back Room",
            AnyGameType(),
            FutureStart,
            FutureEnd,
            8));

        Assert.Equal(
            $"Event description cannot exceed {Event.MaxDescriptionLength} characters.",
            exception.Message);
        Assert.Equal("Description", exception.Key);
    }

    [Fact]
    public void Create_DescriptionOnlyFitsOnceTrimmed_IsAccepted()
    {
        string description = new('a', Event.MaxDescriptionLength);

        Event created = Event.Create(
            "Friday Night Magic",
            $"   {description}   ",
            "The Back Room",
            AnyGameType(),
            FutureStart,
            FutureEnd,
            8);

        Assert.Equal(description, created.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_LocationIsMissing_ThrowsDomainExceptionKeyedToTheLocation(string? location)
    {
        DomainException exception = Assert.Throws<DomainException>(() => Event.Create(
            "Friday Night Magic",
            null,
            location!,
            AnyGameType(),
            FutureStart,
            FutureEnd,
            8));

        Assert.Equal("Event location is required.", exception.Message);
        Assert.Equal("Location", exception.Key);
    }

    [Fact]
    public void Create_LocationIsLongerThanTheMaximum_ThrowsDomainExceptionKeyedToTheLocation()
    {
        DomainException exception = Assert.Throws<DomainException>(() => Event.Create(
            "Friday Night Magic",
            null,
            new string('a', Event.MaxLocationLength + 1),
            AnyGameType(),
            FutureStart,
            FutureEnd,
            8));

        Assert.Equal(
            $"Event location cannot exceed {Event.MaxLocationLength} characters.",
            exception.Message);
        Assert.Equal("Location", exception.Key);
    }

    [Fact]
    public void Create_LocationOnlyFitsOnceTrimmed_IsAccepted()
    {
        string location = new('a', Event.MaxLocationLength);

        Event created = Event.Create(
            "Friday Night Magic",
            null,
            $"   {location}   ",
            AnyGameType(),
            FutureStart,
            FutureEnd,
            8);

        Assert.Equal(location, created.Location);
    }

    [Fact]
    public void Create_NameDescriptionAndLocationCarrySurroundingWhitespace_TrimsThem()
    {
        Event created = Event.Create(
            "  Friday Night Magic  ",
            "  A weekly casual tournament.  ",
            "  The Back Room  ",
            AnyGameType(),
            FutureStart,
            FutureEnd,
            8);

        Assert.Equal("Friday Night Magic", created.Name);
        Assert.Equal("A weekly casual tournament.", created.Description);
        Assert.Equal("The Back Room", created.Location);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Create_StartDateTimeIsNotUtc_ThrowsArgumentException(DateTimeKind kind)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Event.Create(
            "Friday Night Magic",
            null,
            "The Back Room",
            AnyGameType(),
            DateTime.SpecifyKind(FutureStart, kind),
            FutureEnd,
            8));

        Assert.Equal("startDateTime", exception.ParamName);
        Assert.StartsWith("Event start date and time must be UTC.", exception.Message);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Create_EndDateTimeIsNotUtc_ThrowsArgumentException(DateTimeKind kind)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Event.Create(
            "Friday Night Magic",
            null,
            "The Back Room",
            AnyGameType(),
            FutureStart,
            DateTime.SpecifyKind(FutureEnd, kind),
            8));

        Assert.Equal("endDateTime", exception.ParamName);
        Assert.StartsWith("Event end date and time must be UTC.", exception.Message);
    }

    [Fact]
    public void Create_StartDateTimeIsInThePast_ThrowsDomainExceptionKeyedToTheStartDateTime()
    {
        DateTime start = DateTime.UtcNow.AddHours(-1);

        DomainException exception = Assert.Throws<DomainException>(() => Event.Create(
            "Friday Night Magic",
            null,
            "The Back Room",
            AnyGameType(),
            start,
            start.AddHours(3),
            8));

        Assert.Equal("Event start date and time cannot be in the past.", exception.Message);
        Assert.Equal("StartDateTime", exception.Key);
    }

    [Fact]
    public void Create_EndDateTimeEqualsStartDateTime_ThrowsDomainExceptionKeyedToTheEndDateTime()
    {
        DomainException exception = Assert.Throws<DomainException>(() => Event.Create(
            "Friday Night Magic",
            null,
            "The Back Room",
            AnyGameType(),
            FutureStart,
            FutureStart,
            8));

        Assert.Equal(
            "Event start date and time must be before the end date and time.",
            exception.Message);
        Assert.Equal("EndDateTime", exception.Key);
    }

    [Fact]
    public void Create_EndDateTimeIsBeforeStartDateTime_ThrowsDomainExceptionKeyedToTheEndDateTime()
    {
        DomainException exception = Assert.Throws<DomainException>(() => Event.Create(
            "Friday Night Magic",
            null,
            "The Back Room",
            AnyGameType(),
            FutureStart,
            FutureStart.AddMinutes(-1),
            8));

        Assert.Equal(
            "Event start date and time must be before the end date and time.",
            exception.Message);
        Assert.Equal("EndDateTime", exception.Key);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_RegistrationLimitIsBelowOne_ThrowsDomainExceptionKeyedToTheRegistrationLimit(
        int registrationLimit)
    {
        DomainException exception = Assert.Throws<DomainException>(() => Event.Create(
            "Friday Night Magic",
            null,
            "The Back Room",
            AnyGameType(),
            FutureStart,
            FutureEnd,
            registrationLimit));

        Assert.Equal("An event must accept at least one player.", exception.Message);
        Assert.Equal("RegistrationLimit", exception.Key);
    }

    [Fact]
    public void Create_RegistrationLimitExceedsTheMaximum_ThrowsDomainExceptionKeyedToTheRegistrationLimit()
    {
        DomainException exception = Assert.Throws<DomainException>(() => Event.Create(
            "Friday Night Magic",
            null,
            "The Back Room",
            AnyGameType(),
            FutureStart,
            FutureEnd,
            Event.MaxRegistrationLimit + 1));

        Assert.Equal(
            $"An event cannot accept more than {Event.MaxRegistrationLimit} players.",
            exception.Message);
        Assert.Equal("RegistrationLimit", exception.Key);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(Event.MaxRegistrationLimit)]
    public void Create_RegistrationLimitIsOnTheBoundary_IsAccepted(int registrationLimit)
    {
        Event created = Event.Create(
            "Friday Night Magic",
            null,
            "The Back Room",
            AnyGameType(),
            FutureStart,
            FutureEnd,
            registrationLimit);

        Assert.Equal(registrationLimit, created.RegistrationLimit);
    }

    [Fact]
    public void Create_SelectionsAreNotSupplied_CarriesNoSelections()
    {
        Assert.Empty(AnEvent().Selections);
    }

    [Fact]
    public void Create_SelectionsContainANullEntry_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Event.Create(
            "Friday Night Magic",
            null,
            "The Back Room",
            AnyGameType(),
            FutureStart,
            FutureEnd,
            8,
            selections: [null!]));

        Assert.Equal("selections", exception.ParamName);
        Assert.StartsWith("A selection cannot be null.", exception.Message);
    }

    [Fact]
    public void Create_SelectionsCarryTwoSettingsSharingAKeyIgnoringCase_ThrowsDomainExceptionWithoutAKey()
    {
        DomainException exception = Assert.Throws<DomainException>(() => Event.Create(
            "Friday Night Magic",
            null,
            "The Back Room",
            AnyGameType(),
            FutureStart,
            FutureEnd,
            8,
            selections:
            [
                EventGameTypeSelection.Create(PlayerCountSetting(), "4"),
                EventGameTypeSelection.Create(PlayerCountSetting("PLAYERCOUNT"), "6")
            ]));

        Assert.Equal(
            "An event cannot carry two values for the 'PLAYERCOUNT' setting.",
            exception.Message);
        Assert.Null(exception.Key);
    }

    [Fact]
    public void Create_SelectionsNameDifferentSettings_KeepsThemInTheOrderGiven()
    {
        GameTypeSetting playerCount = PlayerCountSetting();
        GameTypeSetting ranked = RankedSetting();

        Event created = Event.Create(
            "Friday Night Magic",
            null,
            "The Back Room",
            AnyGameType(),
            FutureStart,
            FutureEnd,
            8,
            selections:
            [
                EventGameTypeSelection.Create(playerCount, "4"),
                EventGameTypeSelection.Create(ranked, "true")
            ]);

        Assert.Equal(
            new[] { playerCount, ranked },
            created.Selections.Select(selection => selection.GameTypeSetting));
    }

    [Fact]
    public void Create_ArgumentsAreValid_ReturnsEventDescribedByTheArguments()
    {
        GameType gameType = AnyGameType();
        EventGameTypeSelection selection = EventGameTypeSelection.Create(PlayerCountSetting(), "4");

        Event created = Event.Create(
            "Friday Night Magic",
            "A weekly casual tournament.",
            "The Back Room",
            gameType,
            FutureStart,
            FutureEnd,
            12,
            selections: [selection]);

        Assert.Equal("Friday Night Magic", created.Name);
        Assert.Equal("A weekly casual tournament.", created.Description);
        Assert.Equal("The Back Room", created.Location);
        Assert.Same(gameType, created.GameType);
        Assert.Equal(FutureStart, created.StartDateTime);
        Assert.Equal(FutureEnd, created.EndDateTime);
        Assert.Equal(12, created.RegistrationLimit);
        Assert.Same(selection, Assert.Single(created.Selections));
    }

    [Fact]
    public void Create_ArgumentsAreValid_AssignsANonEmptyIdentifierAndNoPrimaryKey()
    {
        Event created = AnEvent();

        Assert.NotEqual(Guid.Empty, created.PublicId);
        Assert.Equal(0, created.Id);
    }

    [Fact]
    public void Create_TwoEventsAreCreated_AssignsEachADistinctIdentifier()
    {
        Assert.NotEqual(AnEvent().PublicId, AnEvent().PublicId);
    }

    [Fact]
    public void Reconstitute_SelectionsAreNotSupplied_CarriesNoSelections()
    {
        Event rehydrated = Event.Reconstitute(
            42,
            Guid.CreateVersion7(),
            "Friday Night Magic",
            null,
            "The Back Room",
            FutureStart,
            FutureEnd,
            AnyGameType(),
            12);

        Assert.Empty(rehydrated.Selections);
    }

    [Fact]
    public void Reconstitute_StateWouldBeRejectedByCreate_AppliesNoValidation()
    {
        DateTime pastStart = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Unspecified);

        Event rehydrated = Event.Reconstitute(
            42,
            Guid.Empty,
            "   ",
            new string('a', Event.MaxDescriptionLength + 1),
            string.Empty,
            pastStart,
            pastStart.AddDays(-1),
            AnyGameType(),
            Event.MaxRegistrationLimit + 1);

        Assert.Equal("   ", rehydrated.Name);
        Assert.Equal(string.Empty, rehydrated.Location);
        Assert.Equal(pastStart, rehydrated.StartDateTime);
        Assert.True(rehydrated.EndDateTime < rehydrated.StartDateTime);
        Assert.Equal(Event.MaxRegistrationLimit + 1, rehydrated.RegistrationLimit);
    }

    [Fact]
    public void Reconstitute_StateIsSupplied_ReturnsEventCarryingItUnchanged()
    {
        GameType gameType = AnyGameType();
        EventGameTypeSelection selection = EventGameTypeSelection.Create(PlayerCountSetting(), "4");
        Guid publicId = Guid.CreateVersion7();

        Event rehydrated = Event.Reconstitute(
            42,
            publicId,
            "Friday Night Magic",
            "A weekly casual tournament.",
            "The Back Room",
            FutureStart,
            FutureEnd,
            gameType,
            12,
            [selection]);

        Assert.Equal(42, rehydrated.Id);
        Assert.Equal(publicId, rehydrated.PublicId);
        Assert.Equal("Friday Night Magic", rehydrated.Name);
        Assert.Equal("A weekly casual tournament.", rehydrated.Description);
        Assert.Equal("The Back Room", rehydrated.Location);
        Assert.Equal(FutureStart, rehydrated.StartDateTime);
        Assert.Equal(FutureEnd, rehydrated.EndDateTime);
        Assert.Same(gameType, rehydrated.GameType);
        Assert.Equal(12, rehydrated.RegistrationLimit);
        Assert.Same(selection, Assert.Single(rehydrated.Selections));
    }

    [Fact]
    public void IsFull_RegistrationCountIsNegative_ThrowsArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() => AnEventAccepting(4).IsFull(-1));

        Assert.Equal("registrationCount", exception.ParamName);
    }

    [Fact]
    public void IsFull_RegistrationCountIsBelowTheLimit_ReturnsFalse()
    {
        Assert.False(AnEventAccepting(4).IsFull(3));
    }

    [Fact]
    public void IsFull_RegistrationCountEqualsTheLimit_ReturnsTrue()
    {
        Assert.True(AnEventAccepting(4).IsFull(4));
    }

    [Fact]
    public void IsFull_RegistrationCountExceedsTheLimit_ReturnsTrue()
    {
        Assert.True(AnEventAccepting(4).IsFull(5));
    }

    [Fact]
    public void IsRegistrationClosed_StartHasPassed_ReturnsTrue()
    {
        Assert.True(AnEventStartingAt(DateTime.UtcNow.AddHours(-1)).IsRegistrationClosed);
    }

    [Fact]
    public void IsRegistrationClosed_StartIsTheInstantRead_ReturnsTrue()
    {
        Assert.True(AnEventStartingAt(DateTime.UtcNow).IsRegistrationClosed);
    }

    [Fact]
    public void IsRegistrationClosed_StartIsStillAhead_ReturnsFalse()
    {
        Assert.False(AnEventStartingAt(DateTime.UtcNow.AddHours(1)).IsRegistrationClosed);
    }

    [Fact]
    public void IsRegistrationClosed_EventWasJustCreated_ReturnsFalse()
    {
        Assert.False(AnEvent().IsRegistrationClosed);
    }

    private static Event AnEvent() => AnEventAccepting(8);

    // Create refuses a start that has passed, so an event that has already begun is rehydrated.
    private static Event AnEventStartingAt(DateTime startDateTime) => Event.Reconstitute(
        1,
        Guid.CreateVersion7(),
        "Friday Night Magic",
        null,
        "The Back Room",
        startDateTime,
        startDateTime.AddHours(3),
        AnyGameType(),
        8);

    private static Event AnEventAccepting(int registrationLimit) => Event.Create(
        "Friday Night Magic",
        null,
        "The Back Room",
        AnyGameType(),
        FutureStart,
        FutureEnd,
        registrationLimit);

    private static GameType AnyGameType() => GameType.Create("Magic", [PlayerCountSetting()]);

    private static GameTypeSetting PlayerCountSetting(string key = "playerCount") =>
        GameTypeSetting.Create(key, "Player count", SettingType.Int, "4", 2, 8);

    private static GameTypeSetting RankedSetting() =>
        GameTypeSetting.Create("ranked", "Ranked", SettingType.Bool, "false");
}
