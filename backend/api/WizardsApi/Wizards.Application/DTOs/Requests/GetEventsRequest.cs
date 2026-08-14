using System.ComponentModel.DataAnnotations;

using Wizards.Domain.Enums;
using Wizards.Domain.Extensions;

namespace Wizards.Application.DTOs.Requests;

/// <summary>
/// The paging window, ordering and date range supplied when listing events.
/// </summary>
/// <remarks>
/// Events are paged over a stable ordering, so a window only shifts when events are inserted or
/// removed ahead of it. Every member is optional, so a caller that supplies nothing reads the first
/// <see cref="DefaultTake"/> events by start date and time, earliest first, over an open range.
/// </remarks>
/// <param name="Skip">
/// The number of events to pass over before the page begins. Zero or greater, and defaults to
/// <see cref="DefaultSkip"/>. A window starting past the end is served as an empty page, not an error.
/// </param>
/// <param name="Take">
/// The maximum number of events the page carries. At least one, capped at <see cref="MaxTake"/>, and
/// defaults to <see cref="DefaultTake"/>.
/// </param>
/// <param name="SortBy">
/// The property the events are ordered by. Defaults to
/// <see cref="EventSortField.StartDateTime"/>. A value outside the defined set is rejected rather
/// than falling back to the default.
/// </param>
/// <param name="SortDirection">
/// The direction the ordering runs in, checked the same way as <paramref name="SortBy"/>. Defaults
/// to <see cref="Wizards.Domain.Enums.SortDirection.Ascending"/>.
/// </param>
/// <param name="StartingOnOrAfter">
/// The lower bound on when an event starts, compared inclusively, so an event starting at exactly
/// this instant is carried. Sent as an ISO 8601 date and time down to at least the minute, such as
/// <c>2026-08-13T16:00:00Z</c>, and omitted to leave the range open at that end.
/// </param>
/// <param name="StartingBefore">
/// The upper bound on when an event starts, compared exclusively, so an event starting at exactly
/// this instant is left off. Written the same way as <paramref name="StartingOnOrAfter"/>, and must
/// not fall before it.
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

    /// <summary>
    /// The instant <see cref="StartingOnOrAfter"/> denotes, in UTC, which every consumer reads in
    /// place of the bound value.
    /// </summary>
    /// <remarks>
    /// The framework's query string binder resolves a value carrying <c>Z</c> or an offset against the
    /// host's zone, yielding <see cref="DateTimeKind.Local"/>, and leaves a zoneless value
    /// <see cref="DateTimeKind.Unspecified"/>, so two bounds written in different forms are comparable
    /// neither with each other nor with a stored instant until they are resolved here.
    /// </remarks>
    public DateTime? StartingOnOrAfterUtc => this.StartingOnOrAfter.ToUtcInstant();

    /// <summary>
    /// The instant <see cref="StartingBefore"/> denotes, in UTC, resolved in the same way as
    /// <see cref="StartingOnOrAfterUtc"/>.
    /// </summary>
    public DateTime? StartingBeforeUtc => this.StartingBefore.ToUtcInstant();

    /// <summary>
    /// Reports an inverted date range rather than leaving it to be served as an empty page.
    /// </summary>
    /// <remarks>
    /// The bounds are compared as the instants <see cref="StartingOnOrAfterUtc"/> and
    /// <see cref="StartingBeforeUtc"/> resolve them to. Equal bounds select nothing and are accepted.
    /// </remarks>
    /// <param name="validationContext">The context the request is being validated in. Not consulted.</param>
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
