namespace Wizards.Application.DTOs.Responses;

/// <summary>
/// A page of items as returned to API callers, alongside the window it was read with.
/// </summary>
/// <remarks>
/// The last page is the one whose skip plus returned items reach
/// <see cref="PaginationMeta.TotalCount"/> under the same filters.
/// </remarks>
/// <typeparam name="T">The type of item carried on the page.</typeparam>
/// <param name="Items">
/// The items falling in the window, in the order the collection is paged by. Empty when the window
/// falls past the end of the selection. Never <see langword="null"/>.
/// </param>
/// <param name="Pagination">
/// The window the page was read with and the size of the selection it was taken from. Never
/// <see langword="null"/>.
/// </param>
public sealed record Page<T>(
    IReadOnlyList<T> Items,
    PaginationMeta Pagination);
