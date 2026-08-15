using Wizards.Application.DTOs.Requests;
using Wizards.Application.DTOs.Responses;
using Wizards.Application.Models;

namespace Wizards.Application.Interfaces;

public interface IEventsService
{
    /// <summary>Retrieves a page of events, ordered and narrowed as the request asks.</summary>
    /// <param name="request">The paging window, ordering and date range to read.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The matching page, empty when nothing falls in the window.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
    Task<Page<EventResponse>> GetEvents(GetEventsRequest request, CancellationToken cancellationToken);

    /// <summary>Retrieves a single event by its identifier.</summary>
    /// <param name="eventId">The identifier of the event to retrieve, which must not be empty.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The matching event, or null when no event carries that identifier.</returns>
    /// <exception cref="ArgumentException">Thrown when the identifier is empty.</exception>
    Task<EventResponse?> GetEvent(Guid eventId, CancellationToken cancellationToken);

    /// <summary>Creates an event and assigns it a new identifier.</summary>
    /// <param name="request">The details of the event to create.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The created event, or a failure naming the request field that broke a rule.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the request or its game type is null.</exception>
    Task<EventWriteResult> AddEvent(CreateEventRequest request, CancellationToken cancellationToken);
}
