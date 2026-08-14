namespace Wizards.Application.Models;

/// <summary>
/// A calendar invite, ready to be written to a response as it stands.
/// </summary>
/// <param name="FileName">The name to offer the file under, carrying the <c>.ics</c> extension.</param>
/// <param name="ContentType">The media type the content must be served as.</param>
/// <param name="Content">The invite in the iCalendar format described by RFC 5545.</param>
public record CalendarInvite(string FileName, string ContentType, string Content)
{
    /// <summary>The media type every invite is served as.</summary>
    public const string MediaType = "text/calendar";
}
