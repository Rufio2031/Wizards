using System.ComponentModel.DataAnnotations;

using Wizards.Application.Validation;
using Wizards.Domain.Entities;

namespace Wizards.Application.DTOs.Requests;

/// <summary>The details supplied when creating an event.</summary>
/// <param name="Name">
/// The event's display name, required and capped at <see cref="Event.MaxNameLength"/> characters
/// once trimmed.
/// </param>
/// <param name="Description">
/// The event's long-form description, capped at <see cref="Event.MaxDescriptionLength"/> characters
/// once trimmed, where null or whitespace leaves the event without one.
/// </param>
/// <param name="Location">
/// Where the event is held, required and capped at <see cref="Event.MaxLocationLength"/> characters
/// once trimmed.
/// </param>
/// <param name="StartDateTime">
/// The instant the event begins, required, sent as an ISO 8601 date and time down to at least the
/// minute such as <c>2026-08-13T16:00:00Z</c>, read as UTC when it carries no zone marker, and always
/// stored and returned in UTC. A date on its own, such as <c>2026-08-13</c>, is rejected.
/// </param>
/// <param name="EndDateTime">
/// The instant the event ends, required, written the same way and falling after
/// <paramref name="StartDateTime"/>.
/// </param>
/// <param name="RegistrationLimit">
/// How many players the event accepts, required, at least one and no more than
/// <see cref="Event.MaxRegistrationLimit"/>.
/// </param>
/// <param name="GameType">
/// The game the event is played with and the settings chosen for it, required and rejected when no
/// game type carries that identifier.
/// </param>
public record CreateEventRequest(
    [Required]
    string Name,

    string? Description,

    [Required]
    string Location,

    [RequiredValue]
    DateTime StartDateTime,

    [RequiredValue]
    DateTime EndDateTime,

    [Range(1, Event.MaxRegistrationLimit)]
    int RegistrationLimit,

    [Required]
    EventGameTypeRequest GameType);
