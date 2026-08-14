namespace Wizards.Application.Models;

/// <summary>
/// The deployment-specific values a calendar invite carries.
/// </summary>
/// <param name="UidDomain">
/// The domain every invite's identifier is qualified with, such as <c>wizards.local</c>. RFC 5545
/// borrows email address syntax for that identifier so a domain's ownership makes it unique across
/// calendar systems. No mail is sent to it.
/// </param>
public record CalendarInviteSettings(string UidDomain);
