using System.ComponentModel.DataAnnotations;

namespace Wizards.Application.DTOs.Requests;

/// <summary>
/// The details supplied when registering a player for an event.
/// </summary>
/// <param name="Name">
/// The player's display name. Required, and capped at
/// <see cref="MaxNameLength"/> characters.
/// </param>
public record CreateRegistrationRequest(
    [Required]
    [StringLength(CreateRegistrationRequest.MaxNameLength, MinimumLength = 1)]
    string Name)
{
    /// <summary>The maximum length of a player's name.</summary>
    /// <remarks>
    /// Owned here until a registration entity exists to carry the rule, the way
    /// <c>Event.MaxNameLength</c> carries the event's.
    /// </remarks>
    public const int MaxNameLength = 100;
}
