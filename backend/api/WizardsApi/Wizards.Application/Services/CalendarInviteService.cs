using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

using Wizards.Application.Interfaces;
using Wizards.Application.Models;
using Wizards.Domain.Entities;
using Wizards.Domain.Interfaces.Repositories;

namespace Wizards.Application.Services;

/// <summary>
/// Serves calendar invites in the iCalendar format described by RFC 5545.
/// </summary>
/// <param name="eventsRepository">Reads the event an invite describes.</param>
/// <param name="settings">The deployment-specific values every invite carries.</param>
/// <param name="timeProvider">Reads the instant an invite is stamped with.</param>
internal sealed class CalendarInviteService(
    IEventsRepository eventsRepository,
    CalendarInviteSettings settings,
    TimeProvider timeProvider) : ICalendarInviteService
{
    /// <summary>Names the software that produced the invite, as RFC 5545 requires.</summary>
    private const string ProductId = "-//Wizards//Event Calendar//EN";

    private const string PublishMethod = "PUBLISH";

    private const string UtcTimeZoneId = "UTC";

    private const string FileNameExtension = ".ics";

    /// <inheritdoc />
    public async Task<CalendarInvite?> GetInvite(Guid eventId, CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event identifier cannot be empty.", nameof(eventId));
        }

        Event? @event = await eventsRepository.GetEventByPublicIdAsync(eventId, cancellationToken);

        return @event is null ? null : this.Build(@event);
    }

    private CalendarInvite Build(Event @event)
    {
        CalendarEvent calendarEvent = new()
        {
            Uid = EscapeBackslashes($"{@event.PublicId}@{settings.UidDomain}"),
            Summary = EscapeBackslashes(@event.Name),
            Location = EscapeBackslashes(@event.Location),
            Description = EscapeBackslashes(BuildDescription(@event)),
            Organizer = new Organizer
            {
                Value = settings.OrganizerAddress,
                CommonName = settings.OrganizerName
            },
            DtStart = ToCalendarDateTime(@event.StartDateTime),
            DtEnd = ToCalendarDateTime(@event.EndDateTime),
            DtStamp = ToCalendarDateTime(timeProvider.GetUtcNow().UtcDateTime),

            // Events cannot be edited, so no invite supersedes one already imported.
            Sequence = 0
        };

        Calendar calendar = new()
        {
            ProductId = ProductId,
            Method = PublishMethod
        };

        calendar.Events.Add(calendarEvent);

        string content = new CalendarSerializer(calendar).SerializeToString()
            ?? throw new InvalidOperationException("The calendar serializer produced no invite.");

        return new CalendarInvite(BuildFileName(@event), CalendarInvite.MediaType, content);
    }

    /// <summary>
    /// Escapes a text value's backslashes for RFC 5545, which Ical.Net 5.2.3 leaves raw while escaping
    /// commas, semicolons and newlines itself. Run before serialization so the two passes compose.
    /// </summary>
    private static string EscapeBackslashes(string value) =>
        value.Replace(@"\", @"\\", StringComparison.Ordinal);

    /// <summary>
    /// States the game alongside whatever the organizer wrote.
    /// </summary>
    private static string BuildDescription(Event @event)
    {
        List<string> lines = [];

        if (!string.IsNullOrWhiteSpace(@event.Description))
        {
            lines.Add(@event.Description);
        }

        lines.Add($"Game: {@event.GameType.Name}");

        return string.Join("\n", lines);
    }

    /// <summary>Marks an instant as UTC, which is what writes it with a trailing <c>Z</c>.</summary>
    private static CalDateTime ToCalendarDateTime(DateTime instant) =>
        new(instant, UtcTimeZoneId, hasTime: true);

    /// <summary>
    /// Names the download after the event's identifier. An event's name is free text, and nothing here
    /// depends on the file being called anything in particular.
    /// </summary>
    private static string BuildFileName(Event @event) =>
        @event.PublicId + FileNameExtension;
}
