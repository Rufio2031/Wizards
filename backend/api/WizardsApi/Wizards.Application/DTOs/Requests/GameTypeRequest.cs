using System.ComponentModel.DataAnnotations;

using Wizards.Domain.Entities;

namespace Wizards.Application.DTOs.Requests;

/// <summary>
/// The game type an event is played with, identified by name.
/// </summary>
/// <remarks>
/// Game types are reference data and are resolved, never created, by the events endpoints. A name
/// that is not already registered is rejected rather than added.
/// </remarks>
/// <param name="Name">
/// The display name of an already-registered game type. Required, capped at
/// <see cref="GameType.MaxNameLength"/> characters, and matched without regard to case.
/// </param>
public record GameTypeRequest(
    [Required]
    [StringLength(GameType.MaxNameLength, MinimumLength = 1)]
    string Name);
