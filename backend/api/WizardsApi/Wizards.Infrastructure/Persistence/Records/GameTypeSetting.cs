using Wizards.Domain.Enums;

namespace Wizards.Infrastructure.Persistence.Records;

internal sealed class GameTypeSetting
{
    /// <summary>Gets or sets the primary key of the setting.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the foreign key of the game type that exposes the setting.</summary>
    public int GameTypeId { get; set; }

    /// <summary>
    /// Gets or sets the game type that exposes the setting. Only populated on reads that explicitly
    /// load it.
    /// </summary>
    public GameType GameType { get; set; } = null!;

    /// <summary>Gets or sets the stable identifier of the setting, unique within its game type.</summary>
    public required string Key { get; set; }

    /// <summary>Gets or sets the name the setting is presented under.</summary>
    public required string Label { get; set; }

    /// <summary>
    /// Gets or sets the explanation of the setting shown alongside it, or <see langword="null"/> when
    /// it has none.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the kind of value the setting holds, stored as its numeric value.</summary>
    public SettingType Type { get; set; }

    /// <summary>
    /// Gets or sets the smallest value the setting accepts, or <see langword="null"/> when it is
    /// unbounded below.
    /// </summary>
    public int? MinValue { get; set; }

    /// <summary>
    /// Gets or sets the largest value the setting accepts, or <see langword="null"/> when it is
    /// unbounded above.
    /// </summary>
    public int? MaxValue { get; set; }

    /// <summary>Gets or sets the value used when an organizer does not choose one.</summary>
    public required string DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the fixed values the setting allows, which is empty for every kind other than
    /// <see cref="SettingType.Enum"/>.
    /// </summary>
    public List<GameTypeSettingOption> Options { get; set; } = [];
}
