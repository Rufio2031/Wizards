namespace Wizards.Infrastructure.Persistence.Records;

internal sealed class GameTypeSettingOption
{
    /// <summary>Gets or sets the primary key of the option.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the foreign key of the setting that allows the option.</summary>
    public int GameTypeSettingId { get; set; }

    /// <summary>
    /// Gets or sets the setting that allows the option. Only populated on reads that explicitly load
    /// it.
    /// </summary>
    public GameTypeSetting Setting { get; set; } = null!;

    /// <summary>Gets or sets the value the option allows.</summary>
    public required string Value { get; set; }
}
