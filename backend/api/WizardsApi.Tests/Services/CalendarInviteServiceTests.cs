using NSubstitute;

using Wizards.Application.Models;
using Wizards.Application.Services;
using Wizards.Domain.Entities;
using Wizards.Domain.Interfaces.Repositories;

namespace WizardsApi.Tests.Services;

public sealed class CalendarInviteServiceTests
{
    private const string OrganizerEmailAddress = "events@wizardsassessment.local";

    private const string OrganizerName = "The Wizard's Table";

    private static readonly Guid EventId = new("3f2f6d7e-6f4a-4f2b-9d1a-9c5a4b3c2d1e");

    private readonly IEventsRepository eventsRepository = Substitute.For<IEventsRepository>();
    private readonly TimeProvider timeProvider = Substitute.For<TimeProvider>();

    private readonly CalendarInviteService calendarInviteService;

    public CalendarInviteServiceTests()
    {
        this.timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero));

        this.calendarInviteService = this.CreateService(OrganizerName);
    }

    [Fact]
    public async Task GetInvite_EventIdIsEmpty_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => this.calendarInviteService.GetInvite(Guid.Empty, CancellationToken.None));

        await this.eventsRepository.DidNotReceiveWithAnyArgs().GetEventByPublicIdAsync(default, default);
    }

    [Fact]
    public async Task GetInvite_NoEventCarriesTheIdentifier_ReturnsNull()
    {
        this.eventsRepository
            .GetEventByPublicIdAsync(EventId, Arg.Any<CancellationToken>())
            .Returns((Event?)null);

        Assert.Null(await this.calendarInviteService.GetInvite(EventId, CancellationToken.None));
    }

    [Fact]
    public async Task GetInvite_NameCarriesABackslash_WritesItDoubled()
    {
        string content = await this.Serialize(name: @"Hall \ Room");

        Assert.Equal(@"SUMMARY:Hall \\ Room", PropertyLine(content, "SUMMARY"));
    }

    [Fact]
    public async Task GetInvite_LocationCarriesABackslash_WritesItDoubled()
    {
        string content = await this.Serialize(location: @"Hall \ Room");

        Assert.Equal(@"LOCATION:Hall \\ Room", PropertyLine(content, "LOCATION"));
    }

    [Fact]
    public async Task GetInvite_DescriptionCarriesABackslash_WritesItDoubled()
    {
        string content = await this.Serialize(description: @"Desc \ one");

        Assert.Equal(
            @"DESCRIPTION:Desc \\ one\nGame: Magic: The Gathering",
            PropertyLine(content, "DESCRIPTION"));
    }

    [Fact]
    public async Task GetInvite_NameCarriesABackslashFollowedByN_WritesTheBackslashDoubledRatherThanALineBreak()
    {
        string content = await this.Serialize(name: @"Backslash \n Literal");

        Assert.Equal(@"SUMMARY:Backslash \\n Literal", PropertyLine(content, "SUMMARY"));
    }

    [Fact]
    public async Task GetInvite_NameCarriesADoubledBackslash_WritesFour()
    {
        string content = await this.Serialize(name: @"Doubled \\ Backslash");

        Assert.Equal(@"SUMMARY:Doubled \\\\ Backslash", PropertyLine(content, "SUMMARY"));
    }

    [Fact]
    public async Task GetInvite_NameCarriesACommaOrSemicolon_WritesEachEscapedOnceRatherThanTwice()
    {
        string content = await this.Serialize(name: "Room, B; 2");

        Assert.Equal(@"SUMMARY:Room\, B\; 2", PropertyLine(content, "SUMMARY"));
    }

    [Fact]
    public async Task GetInvite_DescriptionCarriesALineBreak_WritesItEscapedOnceRatherThanTwice()
    {
        string content = await this.Serialize(description: "First\nSecond");

        Assert.Equal(
            @"DESCRIPTION:First\nSecond\nGame: Magic: The Gathering",
            PropertyLine(content, "DESCRIPTION"));
    }

    [Fact]
    public async Task GetInvite_NameCarriesABackslashBeforeAComma_DoublesTheBackslashAndLeavesTheCommaEscapedOnce()
    {
        string content = await this.Serialize(name: @"Room \, B");

        Assert.Equal(@"SUMMARY:Room \\\, B", PropertyLine(content, "SUMMARY"));
    }

    [Fact]
    public async Task GetInvite_EveryTextFieldCarriesEscapableCharacters_WritesEachFieldEscapedOnce()
    {
        string content = await this.Serialize(
            name: @"Backslash \n Literal",
            location: @"Hall \ Room, B; 2",
            description: @"Desc \ one, two; three\\ four");

        Assert.Equal(@"SUMMARY:Backslash \\n Literal", PropertyLine(content, "SUMMARY"));
        Assert.Equal(@"LOCATION:Hall \\ Room\, B\; 2", PropertyLine(content, "LOCATION"));
        Assert.Equal(
            @"DESCRIPTION:Desc \\ one\, two\; three\\\\ four\nGame: Magic: The Gathering",
            PropertyLine(content, "DESCRIPTION"));
    }

    [Fact]
    public async Task GetInvite_NoTextCarriesAnythingToEscape_WritesEachFieldAsGiven()
    {
        string content = await this.Serialize();

        Assert.Equal("SUMMARY:Friday Night Magic", PropertyLine(content, "SUMMARY"));
        Assert.Equal("LOCATION:Store Front Room", PropertyLine(content, "LOCATION"));
        Assert.Equal(
            @"DESCRIPTION:A weekly casual tournament.\nGame: Magic: The Gathering",
            PropertyLine(content, "DESCRIPTION"));
    }

    [Fact]
    public async Task GetInvite_InviteIsBuilt_WritesTheConfiguredOrganizerAsAMailtoAddressCarryingTheConfiguredName()
    {
        string content = await this.Serialize();

        Assert.Equal(
            $"ORGANIZER;CN={OrganizerName}:mailto:{OrganizerEmailAddress}",
            ContentLine(content, "ORGANIZER"));
    }

    [Fact]
    public async Task GetInvite_OrganizerNameCarriesABackslash_WritesItOnceWhileTheTextFieldsWriteTheirsDoubled()
    {
        string content = await this.Serialize(
            name: @"Hall \ Room",
            location: @"Hall \ Room",
            service: this.CreateService(@"The Wizard \ Table"));

        Assert.Equal(
            $@"ORGANIZER;CN=The Wizard \ Table:mailto:{OrganizerEmailAddress}",
            ContentLine(content, "ORGANIZER"));
        Assert.Equal(@"SUMMARY:Hall \\ Room", PropertyLine(content, "SUMMARY"));
        Assert.Equal(@"LOCATION:Hall \\ Room", PropertyLine(content, "LOCATION"));
    }

    [Fact]
    public async Task GetInvite_OrganizerNameCarriesADoubledBackslash_WritesTwoRatherThanFour()
    {
        string content = await this.Serialize(service: this.CreateService(@"The Wizard \\ Table"));

        Assert.Equal(
            $@"ORGANIZER;CN=The Wizard \\ Table:mailto:{OrganizerEmailAddress}",
            ContentLine(content, "ORGANIZER"));
    }

    [Theory]
    [InlineData("The: Wizard's Table")]
    [InlineData("The; Wizard's Table")]
    [InlineData("The, Wizard's Table")]
    public async Task GetInvite_OrganizerNameCarriesACharacterAParameterValueCannotHoldRaw_WritesItQuotedRatherThanEscaped(
        string organizerName)
    {
        string content = await this.Serialize(service: this.CreateService(organizerName));

        Assert.Equal(
            $"ORGANIZER;CN=\"{organizerName}\":mailto:{OrganizerEmailAddress}",
            ContentLine(content, "ORGANIZER"));
    }

    [Fact]
    public async Task GetInvite_InviteIsBuilt_WritesExactlyOneOrganizerAsThePublishMethodRequires()
    {
        string content = await this.Serialize();

        Assert.Contains("METHOD:PUBLISH", content, StringComparison.Ordinal);
        Assert.Single(
            Unfold(content).Split(["\r\n", "\n"], StringSplitOptions.None),
            line => line.StartsWith("ORGANIZER", StringComparison.Ordinal));
    }

    private CalendarInviteService CreateService(string organizerName) =>
        new(
            this.eventsRepository,
            new CalendarInviteSettings(
                "wizards.local",
                new Uri($"mailto:{OrganizerEmailAddress}"),
                organizerName),
            this.timeProvider);

    private async Task<string> Serialize(
        string name = "Friday Night Magic",
        string location = "Store Front Room",
        string? description = "A weekly casual tournament.",
        CalendarInviteService? service = null)
    {
        Event @event = Event.Reconstitute(
            1,
            EventId,
            name,
            description,
            location,
            new DateTime(2026, 9, 1, 18, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 22, 0, 0, DateTimeKind.Utc),
            GameType.Create("Magic: The Gathering"),
            8);

        this.eventsRepository
            .GetEventByPublicIdAsync(EventId, Arg.Any<CancellationToken>())
            .Returns(@event);

        CalendarInvite? invite = await (service ?? this.calendarInviteService).GetInvite(
            EventId,
            CancellationToken.None);

        Assert.NotNull(invite);

        return invite.Content;
    }

    /// <summary>
    /// Reads a property back as one logical line, undoing the 75-octet folding RFC 5545 requires so an
    /// assertion sees the value the serializer wrote rather than where it wrapped.
    /// </summary>
    private static string PropertyLine(string content, string propertyName) =>
        Assert.Single(
            Unfold(content).Split(["\r\n", "\n"], StringSplitOptions.None),
            line => line.StartsWith($"{propertyName}:", StringComparison.Ordinal));

    /// <summary>
    /// Reads a property back as one logical line where the property carries parameters, which sit
    /// between the name and the <c>:</c> that opens the value.
    /// </summary>
    private static string ContentLine(string content, string propertyName) =>
        Assert.Single(
            Unfold(content).Split(["\r\n", "\n"], StringSplitOptions.None),
            line => line.StartsWith($"{propertyName};", StringComparison.Ordinal)
                || line.StartsWith($"{propertyName}:", StringComparison.Ordinal));

    private static string Unfold(string content) =>
        content
            .Replace("\r\n ", string.Empty, StringComparison.Ordinal)
            .Replace("\r\n\t", string.Empty, StringComparison.Ordinal)
            .Replace("\n ", string.Empty, StringComparison.Ordinal)
            .Replace("\n\t", string.Empty, StringComparison.Ordinal);
}
