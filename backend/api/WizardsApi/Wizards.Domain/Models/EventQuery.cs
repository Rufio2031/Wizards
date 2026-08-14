using Wizards.Domain.Enums;

namespace Wizards.Domain.Models;

/// <summary>
/// The window, ordering and date range a page of events is read over.
/// </summary>
/// <remarks>
/// Both bounds must already be UTC instants, which construction enforces. A range that selects
/// nothing is a valid query and yields an empty page.
/// </remarks>
/// <param name="Skip">
/// The number of events to pass over before the page begins. Zero or greater. Counted within the
/// date range, not within the whole collection.
/// </param>
/// <param name="Take">The maximum number of events the page carries. Greater than zero.</param>
/// <param name="SortField">The property the events are ordered by.</param>
/// <param name="SortDirection">The direction that ordering runs in.</param>
/// <param name="StartingOnOrAfter">
/// The inclusive lower bound on when an event starts, in UTC, or <see langword="null"/> to leave the
/// range open at that end.
/// </param>
/// <param name="StartingBefore">
/// The exclusive upper bound on when an event starts, in UTC, or <see langword="null"/> to leave the
/// range open at that end.
/// </param>
/// <exception cref="ArgumentException">
/// Thrown when either bound carries a kind other than <see cref="DateTimeKind.Utc"/>.
/// </exception>
public sealed record EventQuery(
    int Skip,
    int Take,
    EventSortField SortField,
    SortDirection SortDirection,
    DateTime? StartingOnOrAfter,
    DateTime? StartingBefore)
{
    /// <inheritdoc cref="EventQuery(int, int, EventSortField, SortDirection, DateTime?, DateTime?)" path="/param[@name='StartingOnOrAfter']"/>
    public DateTime? StartingOnOrAfter { get; } =
        RequireUtcInstant(StartingOnOrAfter, nameof(StartingOnOrAfter));

    /// <inheritdoc cref="EventQuery(int, int, EventSortField, SortDirection, DateTime?, DateTime?)" path="/param[@name='StartingBefore']"/>
    public DateTime? StartingBefore { get; } =
        RequireUtcInstant(StartingBefore, nameof(StartingBefore));

    private static DateTime? RequireUtcInstant(DateTime? instant, string parameterName)
    {
        if (instant is { Kind: not DateTimeKind.Utc })
        {
            throw new ArgumentException("Event date range bounds must be UTC.", parameterName);
        }

        return instant;
    }
}
