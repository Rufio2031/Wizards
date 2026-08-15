using Wizards.Domain.Entities;

namespace Wizards.Domain.Interfaces.Repositories;

public interface IEventRegistrationsRepository
{
    /// <summary>
    /// Counts the registrations held against a single event.
    /// </summary>
    /// <remarks>
    /// The store enforces the same limit on insert, so a caller must not treat this count as the gate
    /// on whether a registration will be accepted.
    /// </remarks>
    /// <param name="event">The event to count registrations for, as read from the store.</param>
    /// <param name="cancellationToken">Cancels the read before it completes.</param>
    /// <returns>The number of players registered for the event.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="event"/> is <see langword="null"/>.
    /// </exception>
    Task<int> CountRegistrationsAsync(Event @event, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the registrations held against a single event, in the order they were taken.
    /// </summary>
    /// <param name="event">
    /// The event to read registrations for, as read from the store. Every returned registration is
    /// held against this instance.
    /// </param>
    /// <param name="cancellationToken">Cancels the read before it completes.</param>
    /// <returns>
    /// The registrations, empty when nobody has registered. Never <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="event"/> is <see langword="null"/>.
    /// </exception>
    Task<IReadOnlyList<EventRegistration>> GetRegistrationsAsync(
        Event @event,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the registration an event already holds under a key.
    /// </summary>
    /// <param name="event">The event to read the registration for, as read from the store.</param>
    /// <param name="idempotencyKey">The key the registration would have been taken under.</param>
    /// <param name="cancellationToken">Cancels the read before it completes.</param>
    /// <returns>
    /// The registration, or <see langword="null"/> when the event holds none under that key.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="event"/> is <see langword="null"/>.
    /// </exception>
    Task<EventRegistration?> GetRegistrationByIdempotencyKeyAsync(
        Event @event,
        Guid idempotencyKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stages the insertion of a new registration.
    /// </summary>
    /// <remarks>
    /// The event the registration is held against must already exist and must have room for it. Both
    /// are rules the store enforces when the staged work is committed, so breaking either one fails
    /// <see cref="IUnitOfWork.SaveChangesAsync"/> rather than this call.
    /// </remarks>
    /// <param name="registration">The registration to insert.</param>
    /// <param name="cancellationToken">Cancels the staging before it completes.</param>
    /// <returns>A task that completes once the insertion is staged.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="registration"/> is <see langword="null"/>.
    /// </exception>
    Task AddRegistrationAsync(EventRegistration registration, CancellationToken cancellationToken);
}
