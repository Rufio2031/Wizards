namespace Wizards.Application.Models;

/// <summary>
/// The deployment-specific values a calendar invite carries.
/// </summary>
/// <param name="UidDomain">
/// The domain every invite's identifier is qualified with, such as <c>wizards.local</c>. No mail is
/// sent to it.
/// </param>
/// <param name="OrganizerAddress">
/// The address every invite names as its organizer, as a <c>mailto</c> URI. No mail is sent to it.
/// </param>
/// <param name="OrganizerName">The display name written beside the organizer's address.</param>
public record CalendarInviteSettings(string UidDomain, Uri OrganizerAddress, string OrganizerName);
