using System.ComponentModel.DataAnnotations;

using Wizards.Domain.Entities;

namespace Wizards.Application.DTOs.Requests;

/// <summary>
/// The details supplied when creating an event.
/// </summary>
/// <param name="Name">
/// The event's display name. Required, and capped at <see cref="Event.MaxNameLength"/> characters.
/// </param>
/// <param name="Description">
/// The event's long-form description, capped at <see cref="Event.MaxDescriptionLength"/> characters.
/// Omit or pass <see langword="null"/> to leave the event without one.
/// </param>
/// <param name="StartDateTime">
/// The instant the event begins, which must not already have passed. Sent as an ISO 8601 date and
/// time down to at least the minute, such as <c>2026-08-13T16:00:00Z</c>; a date on its own such as
/// <c>2026-08-13</c> is rejected. Accepted with a UTC marker, with an offset, or with neither; an
/// offset is converted to the instant it denotes and a value with no zone marker at all is read as
/// UTC. Whatever is sent, UTC is what is stored and returned.
/// </param>
/// <param name="EndDateTime">
/// The instant the event ends, which must fall after <paramref name="StartDateTime"/>, written and
/// read the same way. Omit or pass <see langword="null"/> to create an event with no scheduled end.
/// </param>
/// <param name="GameType">
/// The already-registered game type the event is played with. Required, and rejected when no game
/// type is registered under that name.
/// </param>
public record CreateEventRequest(
    [Required]
    [StringLength(Event.MaxNameLength, MinimumLength = 1)]
    string Name,

    [StringLength(Event.MaxDescriptionLength)]
    string? Description,

    [Required]
    DateTime StartDateTime,

    DateTime? EndDateTime,

    [Required]
    GameTypeRequest GameType);
