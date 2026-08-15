using System.ComponentModel.DataAnnotations;

using Wizards.Application.DTOs.Requests;
using Wizards.Domain.Enums;

namespace WizardsApi.Tests.Models;

public sealed class GetEventsRequestTests
{
    private static readonly DateTime Instant = new(2026, 8, 13, 16, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_NothingIsSupplied_ReadsTheFirstPageByStartDateTimeEarliestFirst()
    {
        GetEventsRequest request = new();

        Assert.Equal(0, request.Skip);
        Assert.Equal(50, request.Take);
        Assert.Equal(EventSortField.StartDateTime, request.SortBy);
        Assert.Equal(SortDirection.Ascending, request.SortDirection);
    }

    [Fact]
    public void StartingOnOrAfterUtc_NoBoundIsSupplied_IsNull()
    {
        GetEventsRequest request = new();

        Assert.Null(request.StartingOnOrAfterUtc);
    }

    [Fact]
    public void StartingOnOrAfterUtc_BoundIsAlreadyUtc_CarriesItThroughUnchanged()
    {
        GetEventsRequest request = new(StartingOnOrAfter: Instant);

        Assert.Equal(Instant.Ticks, request.StartingOnOrAfterUtc!.Value.Ticks);
        Assert.Equal(DateTimeKind.Utc, request.StartingOnOrAfterUtc.Value.Kind);
    }

    [Fact]
    public void StartingOnOrAfterUtc_BoundIsLocal_ResolvesToTheInstantItDenotes()
    {
        DateTime sameInstantInHostZone = Instant.ToLocalTime();

        GetEventsRequest request = new(StartingOnOrAfter: sameInstantInHostZone);

        Assert.Equal(Instant, request.StartingOnOrAfterUtc);
        Assert.Equal(DateTimeKind.Utc, request.StartingOnOrAfterUtc!.Value.Kind);
    }

    [Fact]
    public void StartingOnOrAfterUtc_BoundIsUnspecified_IsReadAsUtcRatherThanShiftedFromTheHostZone()
    {
        DateTime zonelessBound = new(2026, 8, 13, 16, 0, 0, DateTimeKind.Unspecified);

        GetEventsRequest request = new(StartingOnOrAfter: zonelessBound);

        Assert.Equal(zonelessBound.Ticks, request.StartingOnOrAfterUtc!.Value.Ticks);
        Assert.Equal(DateTimeKind.Utc, request.StartingOnOrAfterUtc.Value.Kind);
    }

    [Fact]
    public void StartingBeforeUtc_NoBoundIsSupplied_IsNull()
    {
        GetEventsRequest request = new();

        Assert.Null(request.StartingBeforeUtc);
    }

    [Fact]
    public void StartingBeforeUtc_BoundIsAlreadyUtc_CarriesItThroughUnchanged()
    {
        GetEventsRequest request = new(StartingBefore: Instant);

        Assert.Equal(Instant.Ticks, request.StartingBeforeUtc!.Value.Ticks);
        Assert.Equal(DateTimeKind.Utc, request.StartingBeforeUtc.Value.Kind);
    }

    [Fact]
    public void StartingBeforeUtc_BoundIsLocal_ResolvesToTheInstantItDenotes()
    {
        DateTime sameInstantInHostZone = Instant.ToLocalTime();

        GetEventsRequest request = new(StartingBefore: sameInstantInHostZone);

        Assert.Equal(Instant, request.StartingBeforeUtc);
        Assert.Equal(DateTimeKind.Utc, request.StartingBeforeUtc!.Value.Kind);
    }

    [Fact]
    public void StartingBeforeUtc_BoundIsUnspecified_IsReadAsUtcRatherThanShiftedFromTheHostZone()
    {
        DateTime zonelessBound = new(2026, 8, 13, 16, 0, 0, DateTimeKind.Unspecified);

        GetEventsRequest request = new(StartingBefore: zonelessBound);

        Assert.Equal(zonelessBound.Ticks, request.StartingBeforeUtc!.Value.Ticks);
        Assert.Equal(DateTimeKind.Utc, request.StartingBeforeUtc.Value.Kind);
    }

    [Fact]
    public void Validate_RangeIsInverted_ReturnsAFailureNamingBothBounds()
    {
        GetEventsRequest request = new(
            StartingOnOrAfter: Instant,
            StartingBefore: Instant.AddTicks(-1));

        ValidationResult failure = Assert.Single(Validate(request));

        Assert.Equal(
            $"{nameof(GetEventsRequest.StartingOnOrAfter)} must not fall after " +
            $"{nameof(GetEventsRequest.StartingBefore)}.",
            failure.ErrorMessage);
        Assert.Equal(
            [nameof(GetEventsRequest.StartingOnOrAfter), nameof(GetEventsRequest.StartingBefore)],
            failure.MemberNames);
    }

    [Fact]
    public void Validate_BoundsAreWrittenInDifferentKindsAndTheRangeIsInverted_ReturnsAFailure()
    {
        GetEventsRequest request = new(
            StartingOnOrAfter: Instant.ToLocalTime(),
            StartingBefore: DateTime.SpecifyKind(Instant.AddHours(-1), DateTimeKind.Unspecified));

        Assert.Single(Validate(request));
    }

    [Fact]
    public void Validate_BoundsAreEqual_IsAccepted()
    {
        GetEventsRequest request = new(StartingOnOrAfter: Instant, StartingBefore: Instant);

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void Validate_RangeRunsForward_IsAccepted()
    {
        GetEventsRequest request = new(
            StartingOnOrAfter: Instant,
            StartingBefore: Instant.AddHours(1));

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void Validate_BoundsAreWrittenInDifferentKindsAndTheRangeRunsForward_IsAccepted()
    {
        GetEventsRequest request = new(
            StartingOnOrAfter: Instant.ToLocalTime(),
            StartingBefore: DateTime.SpecifyKind(Instant.AddHours(1), DateTimeKind.Unspecified));

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void Validate_OnlyTheLowerBoundIsSupplied_IsAccepted()
    {
        GetEventsRequest request = new(StartingOnOrAfter: Instant);

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void Validate_OnlyTheUpperBoundIsSupplied_IsAccepted()
    {
        GetEventsRequest request = new(StartingBefore: Instant);

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void Validate_NeitherBoundIsSupplied_IsAccepted()
    {
        Assert.Empty(Validate(new GetEventsRequest()));
    }

    private static IReadOnlyList<ValidationResult> Validate(GetEventsRequest request) =>
        [.. request.Validate(new ValidationContext(request))];
}
