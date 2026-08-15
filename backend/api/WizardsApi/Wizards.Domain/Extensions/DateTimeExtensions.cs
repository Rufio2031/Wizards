namespace Wizards.Domain.Extensions;

/// <summary>
/// Resolves instants onto the single kind the domain and the store deal in.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Returns the UTC instant a date and time denotes.
    /// </summary>
    /// <remarks>A value carrying no zone is read as UTC rather than as the host's local time.</remarks>
    /// <param name="instant">The date and time to resolve.</param>
    /// <returns>The same instant, carrying <see cref="DateTimeKind.Utc"/>.</returns>
    public static DateTime ToUtcInstant(this DateTime instant) =>
        instant.Kind switch
        {
            DateTimeKind.Utc => instant,
            DateTimeKind.Local => instant.ToUniversalTime(),
            _ => DateTime.SpecifyKind(instant, DateTimeKind.Utc)
        };

    /// <summary>
    /// Returns the UTC instant a date and time denotes, passing an absent value through.
    /// </summary>
    /// <param name="instant">The date and time to resolve, or <see langword="null"/>.</param>
    /// <returns>The same instant, carrying <see cref="DateTimeKind.Utc"/>.</returns>
    public static DateTime? ToUtcInstant(this DateTime? instant) =>
        instant?.ToUtcInstant();
}
