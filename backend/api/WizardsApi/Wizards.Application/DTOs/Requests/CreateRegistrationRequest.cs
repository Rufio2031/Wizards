using System.ComponentModel.DataAnnotations;

using Wizards.Domain.Entities;

namespace Wizards.Application.DTOs.Requests;

/// <summary>
/// The details supplied when registering a player for an event.
/// </summary>
/// <param name="Name">
/// The player's display name. Required, and capped at
/// <see cref="EventRegistration.MaxNameLength"/> characters.
/// </param>
public record CreateRegistrationRequest(
    [Required]
    [StringLength(EventRegistration.MaxNameLength, MinimumLength = 1)]
    string Name);
