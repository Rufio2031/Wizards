using Wizards.Application.DTOs.Requests;
using Wizards.Application.DTOs.Responses;
using Wizards.Application.Models;

namespace Wizards.Application.Interfaces;

/// <summary>
/// Reads and maintains the collection of events.
/// </summary>
public interface IEventsService
{
    /// <summary>
    /// Retrieves a page of events, ordered by when they start.
    /// </summary>
    /// <remarks>
    /// The window is read from the store rather than trimmed afterwards, and the ordering breaks ties
    /// so that neighbouring pages neither repeat nor skip an event.
    /// </remarks>
    /// <param name="request">
    /// The paging window to read. Its bounds are enforced at the API boundary, so a window supplied
    /// from anywhere else must already satisfy them.
    /// </param>
    /// <param name="cancellationToken">Cancels the read before it completes.</param>
    /// <returns>
    /// The page of events falling in the window, carrying the window itself and the size of the whole
    /// collection in its <see cref="Page{T}.Pagination"/>. The page carries no events when the window
    /// falls past the end. Never <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
    Task<Page<EventResponse>> GetEvents(GetEventsRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a single event by its identifier.
    /// </summary>
    /// <param name="eventId">The identifier of the event to retrieve. Must not be <see cref="Guid.Empty"/>.</param>
    /// <param name="cancellationToken">Cancels the read before it completes.</param>
    /// <returns>The matching event, or <see langword="null"/> when no event carries that identifier.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="eventId"/> is <see cref="Guid.Empty"/>.</exception>
    Task<EventResponse?> GetEvent(Guid eventId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates an event from the supplied details and assigns it a new identifier.
    /// </summary>
    /// <remarks>
    /// The requested game type is resolved by identifier and is never created, so an identifier that is
    /// not already registered fails the call rather than registering it.
    /// </remarks>
    /// <param name="request">The details of the event to create.</param>
    /// <param name="cancellationToken">Cancels the write before it completes.</param>
    /// <returns>
    /// A result carrying the created event, one carrying <see cref="EventErrors.GameTypeNotFound"/>
    /// when no game type carries the requested identifier, or one carrying an
    /// <see cref="EventErrors.Invalid(string)"/> failure when the requested details break a rule about
    /// what makes a valid event, such as a start date and time that has already passed or an end that
    /// does not fall after the start.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
    Task<EventWriteResult> AddEvent(CreateEventRequest request, CancellationToken cancellationToken);
}
