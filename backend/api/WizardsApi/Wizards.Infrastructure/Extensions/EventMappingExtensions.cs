namespace Wizards.Infrastructure.Extensions;

/// <summary>
/// Translates between the event domain entity and its persistence record.
/// </summary>
internal static class EventMappingExtensions
{
    /// <summary>
    /// Rehydrates the domain entity a stored event represents.
    /// </summary>
    /// <param name="eventRecord">
    /// The stored event to translate. Its game type must already have been loaded, since the entity
    /// cannot exist without one.
    /// </param>
    /// <returns>The rehydrated event entity, carrying its game type and instants marked UTC.</returns>
    internal static Domain.Entities.Event ToEntity(this Persistence.Records.Event eventRecord)
    {
        ArgumentNullException.ThrowIfNull(eventRecord);

        return Domain.Entities.Event.Reconstitute(
            eventRecord.Id,
            eventRecord.PublicId,
            eventRecord.Name,
            eventRecord.Description,
            eventRecord.Location,
            eventRecord.StartDateTime,
            eventRecord.EndDateTime,
            eventRecord.GameType.ToEntity(),
            eventRecord.RegistrationLimit,
            eventRecord.Selections
                .OrderBy(selection => selection.Id)
                .Select(selection => Domain.Entities.EventGameTypeSelection.Reconstitute(
                    selection.Id,
                    selection.Key,
                    selection.Value))
                .ToList());
    }

    /// <summary>
    /// Projects an event entity onto the record shape the database stores.
    /// </summary>
    /// <remarks>
    /// Only the game type's foreign key is carried across. The navigation is deliberately left unset so
    /// that saving an event never inserts or updates the game type it points at.
    /// </remarks>
    /// <param name="eventEntity">The event to translate.</param>
    /// <returns>
    /// A detached record carrying the entity's current state, including its primary key, which is zero
    /// for an event that has never been persisted.
    /// </returns>
    internal static Persistence.Records.Event ToRecord(this Domain.Entities.Event eventEntity)
    {
        ArgumentNullException.ThrowIfNull(eventEntity);

        return new Persistence.Records.Event
        {
            Id = eventEntity.Id,
            PublicId = eventEntity.PublicId,
            Name = eventEntity.Name,
            Description = eventEntity.Description,
            Location = eventEntity.Location,
            GameTypeId = eventEntity.GameType.Id,
            StartDateTime = eventEntity.StartDateTime,
            EndDateTime = eventEntity.EndDateTime,
            RegistrationLimit = eventEntity.RegistrationLimit,
            Selections = eventEntity.Selections
                .Select(selection => new Persistence.Records.EventGameTypeSelection
                {
                    Id = selection.Id,
                    Key = selection.Key,
                    Value = selection.Value
                })
                .ToList()
        };
    }
}
