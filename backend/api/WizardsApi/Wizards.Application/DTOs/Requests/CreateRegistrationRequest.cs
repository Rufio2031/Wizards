using System.ComponentModel.DataAnnotations;

using Wizards.Application.Validation;
using Wizards.Domain.Entities;

namespace Wizards.Application.DTOs.Requests;

/// <summary>
/// The details supplied when registering a player for an event.
/// </summary>
/// <param name="Name">
/// The player's display name. Required, and capped at
/// <see cref="EventRegistration.MaxNameLength"/> characters.
/// </param>
/// <param name="IdempotencyKey">
/// A key the caller generates per registration attempt, required. Repeating a key for the same event
/// returns the registration the key first took rather than taking another.
/// </param>
public record CreateRegistrationRequest(
    [Required]
    [StringLength(EventRegistration.MaxNameLength, MinimumLength = 1)]
    string Name,

    [RequiredValue]
    Guid IdempotencyKey);
