using System.ComponentModel.DataAnnotations;

namespace Wizards.Application.DTOs.Requests;

/// <summary>
/// The game an event is played with, and the settings chosen for it.
/// </summary>
/// <remarks>
/// Game types are reference data and are resolved, never created, by the events endpoints. An
/// identifier that is not already registered is rejected rather than added.
/// </remarks>
/// <param name="GameTypeId">
/// The identifier of an already-registered game type, as served by the game types resource. Required.
/// </param>
public record EventGameTypeRequest(
    [Required]
    Guid GameTypeId);
