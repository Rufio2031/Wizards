namespace Wizards.Infrastructure.Extensions;

/// <summary>
/// Translates between the game type domain entity and its persistence record.
/// </summary>
internal static class GameTypeMappingExtensions
{
    /// <summary>
    /// Rehydrates the domain entity a stored game type represents.
    /// </summary>
    /// <param name="gameTypeRecord">The stored game type to translate.</param>
    /// <returns>The rehydrated game type entity.</returns>
    internal static Domain.Entities.GameType ToEntity(this Persistence.Records.GameType gameTypeRecord)
    {
        ArgumentNullException.ThrowIfNull(gameTypeRecord);

        return Domain.Entities.GameType.Reconstitute(gameTypeRecord.Id, gameTypeRecord.PublicId, gameTypeRecord.Name);
    }

    /// <summary>
    /// Projects a game type entity onto the record shape the database stores.
    /// </summary>
    /// <param name="gameTypeEntity">The game type to translate.</param>
    /// <returns>
    /// A detached record carrying the entity's current state, including its primary key, which is zero
    /// for a game type that has never been persisted and is therefore assigned by the database on
    /// insert.
    /// </returns>
    internal static Persistence.Records.GameType ToRecord(this Domain.Entities.GameType gameTypeEntity)
    {
        ArgumentNullException.ThrowIfNull(gameTypeEntity);

        return new Persistence.Records.GameType
        {
            Id = gameTypeEntity.Id,
            PublicId = gameTypeEntity.PublicId,
            Name = gameTypeEntity.Name
        };
    }
}
