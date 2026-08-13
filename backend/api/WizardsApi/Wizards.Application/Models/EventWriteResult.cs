using Wizards.Application.DTOs.Responses;

namespace Wizards.Application.Models;

/// <summary>
/// The result of an attempt to create or update an event.
/// </summary>
/// <remarks>
/// Exactly one of the two members is populated. Construct instances through <see cref="Success"/> or
/// <see cref="Failure"/> so that pairing is never broken.
/// </remarks>
/// <param name="Event">The written event, or <see langword="null"/> when the attempt failed.</param>
/// <param name="Error">The failure, or <see langword="null"/> when the attempt succeeded.</param>
public sealed record EventWriteResult(EventResponse? Event, ApplicationError? Error)
{
    /// <summary>
    /// Creates a result carrying the written event.
    /// </summary>
    /// <param name="event">The event that was written.</param>
    /// <returns>A result whose error is <see langword="null"/>.</returns>
    public static EventWriteResult Success(EventResponse @event)
    {
        return new EventWriteResult(@event, null);
    }

    /// <summary>
    /// Creates a result carrying the reason nothing was written.
    /// </summary>
    /// <param name="error">The failure that stopped the write.</param>
    /// <returns>A result whose event is <see langword="null"/>.</returns>
    public static EventWriteResult Failure(ApplicationError error)
    {
        return new EventWriteResult(null, error);
    }
}
