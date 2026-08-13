using System.ComponentModel.DataAnnotations;

namespace Wizards.Application.DTOs.Requests;

/// <summary>
/// The paging window supplied when listing events.
/// </summary>
/// <remarks>
/// Events are paged over a stable ordering, so a window only shifts when events are inserted or
/// removed ahead of it. The page reports the size of the whole collection, so a caller can size the
/// remaining windows without walking them.
/// </remarks>
/// <param name="Skip">
/// The number of events to pass over before the page begins. Zero or greater, and defaults to
/// <see cref="DefaultSkip"/>. A window that starts past the end yields an empty page rather than an
/// error.
/// </param>
/// <param name="Take">
/// The maximum number of events the page carries. At least one, capped at <see cref="MaxTake"/>, and
/// defaults to <see cref="DefaultTake"/>.
/// </param>
public record GetEventsRequest(
    [Range(0, int.MaxValue)]
    int Skip = GetEventsRequest.DefaultSkip,

    [Range(1, GetEventsRequest.MaxTake)]
    int Take = GetEventsRequest.DefaultTake)
{
    /// <summary>The number of events passed over when the caller asks for no particular window.</summary>
    public const int DefaultSkip = 0;

    /// <summary>The size of a page the caller does not size itself.</summary>
    public const int DefaultTake = 50;

    /// <summary>The largest page a caller may ask for.</summary>
    public const int MaxTake = 100;
}
