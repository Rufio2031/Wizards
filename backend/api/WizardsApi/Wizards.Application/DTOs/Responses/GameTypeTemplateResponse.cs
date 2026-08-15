using Wizards.Domain.Entities;

namespace Wizards.Application.DTOs.Responses;

/// <summary>
/// A game type and the settings it exposes, as returned to API callers.
/// </summary>
/// <param name="GameTypeId">The identifier assigned to the game type when it was registered.</param>
/// <param name="Name">The game type's display name, in its registered casing.</param>
/// <param name="Settings">
/// The settings the game type exposes, which is empty when it exposes none.
/// </param>
public record GameTypeTemplateResponse(
    Guid GameTypeId,
    string Name,
    IReadOnlyList<GameTypeSettingResponse> Settings)
{
    /// <summary>
    /// Projects a game type onto the shape returned to API callers.
    /// </summary>
    /// <param name="gameType">
    /// The game type to project. Must not be <see langword="null"/>, and must have been loaded with its
    /// settings populated.
    /// </param>
    public GameTypeTemplateResponse(GameType gameType)
        : this(
            gameType.PublicId,
            gameType.Name,
            gameType.Settings.Select(setting => new GameTypeSettingResponse(setting)).ToList())
    {
    }
}
