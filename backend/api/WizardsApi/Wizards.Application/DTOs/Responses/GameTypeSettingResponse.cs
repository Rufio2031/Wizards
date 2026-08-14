using Wizards.Domain.Entities;
using Wizards.Domain.Enums;

namespace Wizards.Application.DTOs.Responses;

/// <summary>
/// One setting a game type exposes, as returned to API callers.
/// </summary>
/// <param name="Key">The identifier a chosen value is submitted under.</param>
/// <param name="Label">The name the setting is presented under.</param>
/// <param name="Description">
/// The explanation shown alongside the setting, or <see langword="null"/> when it has none.
/// </param>
/// <param name="Type">The kind of value the setting holds, which decides how it is presented.</param>
/// <param name="MinValue">
/// The smallest value the setting accepts, or <see langword="null"/> when it is unbounded below. Only
/// ever set for a <see cref="SettingType.Int"/> setting.
/// </param>
/// <param name="MaxValue">
/// The largest value the setting accepts, or <see langword="null"/> when it is unbounded above. Only
/// ever set for a <see cref="SettingType.Int"/> setting.
/// </param>
/// <param name="DefaultValue">The value used when the organizer does not choose one.</param>
/// <param name="Options">
/// The values the setting allows, which is empty for every kind other than
/// <see cref="SettingType.Enum"/>.
/// </param>
public record GameTypeSettingResponse(
    string Key,
    string Label,
    string? Description,
    SettingType Type,
    int? MinValue,
    int? MaxValue,
    string DefaultValue,
    IReadOnlyList<string> Options)
{
    /// <summary>
    /// Projects a setting onto the shape returned to API callers.
    /// </summary>
    /// <param name="setting">The setting to project. Must not be <see langword="null"/>.</param>
    public GameTypeSettingResponse(GameTypeSetting setting)
        : this(
            setting.Key,
            setting.Label,
            setting.Description,
            setting.Type,
            setting.MinValue,
            setting.MaxValue,
            setting.DefaultValue,
            setting.Options.Select(option => option.Value).ToList())
    {
    }
}
