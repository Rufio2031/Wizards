namespace Wizards.Domain.Enums;

/// <summary>
/// The property a page of events is ordered by.
/// </summary>
/// <remarks>
/// Whichever field is chosen, the ordering is tie-broken as
/// <see cref="Interfaces.Repositories.IEventsRepository.GetEventsAsync"/> describes.
/// </remarks>
public enum EventSortField
{
    /// <summary>The instant the event begins.</summary>
    StartDateTime = 0
}
