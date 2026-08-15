namespace Wizards.Infrastructure.Extensions;

/// <summary>
/// Translates between the event registration domain entity and its persistence record.
/// </summary>
internal static class EventRegistrationMappingExtensions
{
    /// <summary>
    /// Rehydrates the domain entity a stored registration represents.
    /// </summary>
    /// <param name="registrationRecord">The stored registration to translate.</param>
    /// <param name="event">
    /// The event the registration is held against, already rehydrated. Supplied by the caller rather
    /// than read through the record's navigation, since a registration is always read for an event the
    /// caller already has.
    /// </param>
    /// <returns>The rehydrated registration entity.</returns>
    internal static Domain.Entities.EventRegistration ToEntity(
        this Persistence.Records.EventRegistration registrationRecord,
        Domain.Entities.Event @event)
    {
        ArgumentNullException.ThrowIfNull(registrationRecord);
        ArgumentNullException.ThrowIfNull(@event);

        return Domain.Entities.EventRegistration.Reconstitute(
            @event,
            registrationRecord.Name,
            registrationRecord.IdempotencyKey);
    }

    /// <summary>
    /// Projects a registration entity onto the record shape the database stores.
    /// </summary>
    /// <remarks>
    /// Only the event's foreign key is carried across. The navigation is deliberately left unset so
    /// that saving a registration never inserts or updates the event it points at.
    /// </remarks>
    /// <param name="registration">The registration to translate.</param>
    /// <returns>
    /// A detached record carrying the entity's current state, with no primary key, which the store
    /// assigns when the row is written.
    /// </returns>
    internal static Persistence.Records.EventRegistration ToRecord(
        this Domain.Entities.EventRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        return new Persistence.Records.EventRegistration
        {
            EventId = registration.Event.Id,
            Name = registration.Name,
            IdempotencyKey = registration.IdempotencyKey
        };
    }
}
