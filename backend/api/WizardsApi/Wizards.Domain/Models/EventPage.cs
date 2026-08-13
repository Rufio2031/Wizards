using Wizards.Domain.Entities;

namespace Wizards.Domain.Models;

/// <summary>
/// A window of events together with the size of the collection it was taken from.
/// </summary>
/// <remarks>
/// The total is read alongside the window rather than under a shared snapshot, so a write landing
/// between the two can leave the total slightly ahead of, or behind, the events on the page.
/// </remarks>
/// <param name="Events">
/// The events falling in the window, in the order they were read. Empty when the window falls past
/// the end of the collection. Never <see langword="null"/>.
/// </param>
/// <param name="TotalCount">
/// The number of events in the whole collection, not the number on this page. Zero or greater.
/// </param>
public sealed record EventPage(IReadOnlyCollection<Event> Events, int TotalCount);
