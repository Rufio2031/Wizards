using Wizards.Domain.Entities;

namespace Wizards.Domain.Models;

/// <summary>
/// A window of events together with the size of the selection it was taken from.
/// </summary>
/// <remarks>
/// The total is read alongside the window rather than under a shared snapshot, so a write landing
/// between the two can leave the total slightly ahead of, or behind, the events on the page.
/// </remarks>
/// <param name="Events">
/// The events falling in the window, in the order they were read. Empty when the window falls past
/// the end of the selection. Never <see langword="null"/>.
/// </param>
/// <param name="TotalCount">
/// The number of events the read selected before the window was applied, not the number on this
/// page. Any filter the read applied has already narrowed it, so it counts the whole collection only
/// when the read was unfiltered. Zero or greater.
/// </param>
public sealed record EventPage(IReadOnlyCollection<Event> Events, int TotalCount);
