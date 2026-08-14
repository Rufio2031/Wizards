using Wizards.Application.DTOs.Requests;
using Wizards.Application.Models;

namespace Wizards.Application.Interfaces;

public interface IRegistrationsService
{
    /// <summary>
    /// Registers a player for an event.
    /// </summary>
    /// <remarks>
    /// The event is resolved by identifier and is never created, so an identifier that is not already
    /// scheduled fails the call. A player may hold more than one registration for the same event,
    /// since nothing yet identifies who is registering beyond the name they supply.
    /// </remarks>
    /// <param name="eventId">
    /// The identifier of the event to register for. Must not be <see cref="Guid.Empty"/>.
    /// </param>
    /// <param name="request">The player's details.</param>
    /// <param name="cancellationToken">Cancels the write before it completes.</param>
    /// <returns>
    /// <see langword="null"/> once the registration is durable, or the reason nothing was written:
    /// <see cref="RegistrationErrors.EventNotFound"/> when no event carries the identifier,
    /// <see cref="RegistrationErrors.EventFull"/> when the event has taken every registration it
    /// accepts, or a <see cref="RegistrationErrors.Invalid"/> failure when the supplied details break
    /// a rule about what makes a valid registration.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="eventId"/> is <see cref="Guid.Empty"/>.
    /// </exception>
    Task<ApplicationError?> AddRegistration(
        Guid eventId,
        CreateRegistrationRequest request,
        CancellationToken cancellationToken);
}
