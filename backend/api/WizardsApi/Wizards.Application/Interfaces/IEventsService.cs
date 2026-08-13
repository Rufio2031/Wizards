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
    /// The requested game type is resolved by name and is never created, so a name that is not already
    /// registered fails the call rather than registering it.
    /// </remarks>
    /// <param name="request">The details of the event to create.</param>
    /// <param name="cancellationToken">Cancels the write before it completes.</param>
    /// <returns>
    /// A result carrying the created event, one carrying <see cref="EventErrors.GameTypeNotFound"/>
    /// when no game type is registered under the requested name, or one carrying an
    /// <see cref="EventErrors.Invalid(string)"/> failure when the requested details break a rule about
    /// what makes a valid event, such as a start date and time that has already passed or an end that
    /// does not fall after the start.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
    Task<EventWriteResult> AddEvent(CreateEventRequest request, CancellationToken cancellationToken);
}
