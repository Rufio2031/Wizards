using Wizards.Domain.Entities;

namespace Wizards.Application.DTOs.Responses;

/// <summary>
/// A game type as returned to API callers.
/// </summary>
/// <param name="GameTypeId">The identifier assigned to the game type when it was registered.</param>
/// <param name="Name">The game type's display name, in its registered casing.</param>
public record GameTypeResponse(
    Guid GameTypeId,
    string Name)
{
    /// <summary>
    /// Projects a game type onto the shape returned to API callers.
    /// </summary>
    /// <param name="gameType">The game type to project. Must not be <see langword="null"/>.</param>
    public GameTypeResponse(GameType gameType)
        : this(gameType.PublicId, gameType.Name)
    {
    }
}
