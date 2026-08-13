namespace Wizards.Application.DTOs.Responses;

/// <summary>
/// The window a page was read with, together with the size of the collection it was taken from.
/// </summary>
/// <param name="Skip">The number of items passed over before the page began.</param>
/// <param name="Take">
/// The maximum number of items the page could carry. A page carrying fewer has reached the end.
/// </param>
/// <param name="TotalCount">
/// The number of items in the whole collection, not the number on the page. Read alongside the page
/// rather than under a shared snapshot, so a concurrent write can leave it slightly out of step with
/// the items returned.
/// </param>
public sealed record PaginationMeta(
    int Skip,
    int Take,
    int TotalCount);
