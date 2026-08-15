using System.ComponentModel.DataAnnotations;

using Wizards.Domain.Enums;
using Wizards.Domain.Extensions;

namespace Wizards.Application.DTOs.Requests;

/// <summary>The paging window, ordering and date range supplied when listing events.</summary>
/// <remarks>
/// Events are paged over a stable ordering, so a window only shifts when events are inserted or
/// removed ahead of it.
/// </remarks>
/// <param name="Skip">
/// The number of events to pass over before the page begins, zero or greater, defaulting to
/// <see cref="DefaultSkip"/>.
/// </param>
/// <param name="Take">
/// The maximum number of events the page carries, at least one, capped at <see cref="MaxTake"/> and
/// defaulting to <see cref="DefaultTake"/>.
/// </param>
/// <param name="SortBy">
/// The property the events are ordered by, defaulting to
/// <see cref="EventSortField.StartDateTime"/> and rejected when outside the defined set.
/// </param>
/// <param name="SortDirection">
/// The direction the ordering runs in, checked the same way as <paramref name="SortBy"/> and
/// defaulting to <see cref="Wizards.Domain.Enums.SortDirection.Ascending"/>.
/// </param>
/// <param name="StartingOnOrAfter">
/// The inclusive lower bound on when an event starts, sent as an ISO 8601 date and time down to at
/// least the minute such as <c>2026-08-13T16:00:00Z</c>, and omitted to leave the range open at that
/// end.
/// </param>
/// <param name="StartingBefore">
/// The exclusive upper bound on when an event starts, written the same way as
/// <paramref name="StartingOnOrAfter"/> and not falling before it.
/// </param>
public record GetEventsRequest(
    [Range(0, int.MaxValue)]
    int Skip = GetEventsRequest.DefaultSkip,

    [Range(1, GetEventsRequest.MaxTake)]
    int Take = GetEventsRequest.DefaultTake,

    [EnumDataType(typeof(EventSortField))]
    EventSortField SortBy = EventSortField.StartDateTime,

    [EnumDataType(typeof(SortDirection))]
    SortDirection SortDirection = Wizards.Domain.Enums.SortDirection.Ascending,

    DateTime? StartingOnOrAfter = null,

    DateTime? StartingBefore = null) : IValidatableObject
{
    /// <summary>The number of events passed over when the caller asks for no particular window.</summary>
    public const int DefaultSkip = 0;

    /// <summary>The size of a page the caller does not size itself.</summary>
    public const int DefaultTake = 50;

    /// <summary>The largest page a caller may ask for.</summary>
    public const int MaxTake = 100;

    /// <summary>The instant <see cref="StartingOnOrAfter"/> denotes, in UTC.</summary>
    public DateTime? StartingOnOrAfterUtc => this.StartingOnOrAfter.ToUtcInstant();

    /// <summary>The instant <see cref="StartingBefore"/> denotes, in UTC.</summary>
    public DateTime? StartingBeforeUtc => this.StartingBefore.ToUtcInstant();

    /// <summary>Reports an inverted date range.</summary>
    /// <param name="validationContext">The context the request is being validated in.</param>
    /// <returns>A failure naming both bounds when the range is inverted, and nothing otherwise.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (this.StartingOnOrAfterUtc > this.StartingBeforeUtc)
        {
            yield return new ValidationResult(
                $"{nameof(this.StartingOnOrAfter)} must not fall after {nameof(this.StartingBefore)}.",
                [nameof(this.StartingOnOrAfter), nameof(this.StartingBefore)]);
        }
    }
}
