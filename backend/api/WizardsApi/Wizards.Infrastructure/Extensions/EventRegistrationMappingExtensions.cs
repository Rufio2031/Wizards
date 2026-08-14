namespace Wizards.Infrastructure.Extensions;

/// <summary>
/// Translates the event registration domain entity into its persistence record.
/// </summary>
/// <remarks>
/// Nothing reads a registration back yet, so the translation only runs on the way to the database.
/// The other direction is added here once something addresses one.
/// </remarks>
internal static class EventRegistrationMappingExtensions
{
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
            Name = registration.Name
        };
    }
}
