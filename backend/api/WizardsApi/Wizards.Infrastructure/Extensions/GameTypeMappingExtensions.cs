namespace Wizards.Infrastructure.Extensions;

/// <summary>
/// Translates between the game type domain entity and its persistence record.
/// </summary>
internal static class GameTypeMappingExtensions
{
    /// <summary>
    /// Rehydrates the domain entity a stored game type represents.
    /// </summary>
    /// <remarks>
    /// Settings and options are ordered by primary key, so they come back in the order they were
    /// inserted rather than in whatever order the database returns them.
    /// </remarks>
    /// <param name="gameTypeRecord">
    /// The stored game type to translate. Its settings are only carried across when the read that
    /// produced it loaded them; otherwise the entity comes back exposing none.
    /// </param>
    /// <returns>The rehydrated game type entity.</returns>
    internal static Domain.Entities.GameType ToEntity(this Persistence.Records.GameType gameTypeRecord)
    {
        ArgumentNullException.ThrowIfNull(gameTypeRecord);

        return Domain.Entities.GameType.Reconstitute(
            gameTypeRecord.Id,
            gameTypeRecord.PublicId,
            gameTypeRecord.Name,
            gameTypeRecord.Settings
                .OrderBy(setting => setting.Id)
                .Select(setting => setting.ToEntity())
                .ToList());
    }

    /// <summary>
    /// Projects a game type entity onto the record shape the database stores.
    /// </summary>
    /// <param name="gameTypeEntity">The game type to translate.</param>
    /// <returns>
    /// A detached record carrying the entity's current state, including its primary key, which is zero
    /// for a game type that has never been persisted and is therefore assigned by the database on
    /// insert. Its settings are carried along, so inserting the record inserts them too.
    /// </returns>
    internal static Persistence.Records.GameType ToRecord(this Domain.Entities.GameType gameTypeEntity)
    {
        ArgumentNullException.ThrowIfNull(gameTypeEntity);

        return new Persistence.Records.GameType
        {
            Id = gameTypeEntity.Id,
            PublicId = gameTypeEntity.PublicId,
            Name = gameTypeEntity.Name,
            Settings = gameTypeEntity.Settings.Select(setting => setting.ToRecord()).ToList()
        };
    }

    private static Domain.Entities.GameTypeSetting ToEntity(this Persistence.Records.GameTypeSetting settingRecord) =>
        Domain.Entities.GameTypeSetting.Reconstitute(
            settingRecord.Id,
            settingRecord.Key,
            settingRecord.Label,
            settingRecord.Description,
            settingRecord.Type,
            settingRecord.DefaultValue,
            settingRecord.MinValue,
            settingRecord.MaxValue,
            settingRecord.Options
                .OrderBy(option => option.Id)
                .Select(option => Domain.Entities.GameTypeSettingOption.Reconstitute(option.Id, option.Value))
                .ToList());

    private static Persistence.Records.GameTypeSetting ToRecord(this Domain.Entities.GameTypeSetting settingEntity) =>
        new()
        {
            Id = settingEntity.Id,
            Key = settingEntity.Key,
            Label = settingEntity.Label,
            Description = settingEntity.Description,
            Type = settingEntity.Type,
            MinValue = settingEntity.MinValue,
            MaxValue = settingEntity.MaxValue,
            DefaultValue = settingEntity.DefaultValue,
            Options = settingEntity.Options
                .Select(option => new Persistence.Records.GameTypeSettingOption
                {
                    Id = option.Id,
                    Value = option.Value
                })
                .ToList()
        };
}
