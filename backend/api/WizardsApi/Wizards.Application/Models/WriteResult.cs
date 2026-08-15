namespace Wizards.Application.Models;

/// <summary>
/// The result of an attempt to write, carrying either the written value or the failure that stopped it.
/// </summary>
/// <typeparam name="T">The type of the written value.</typeparam>
/// <param name="Value">The written value, or <see langword="null"/> when the attempt failed.</param>
/// <param name="Error">The failure, or <see langword="null"/> when the attempt succeeded.</param>
public sealed record WriteResult<T>(T? Value, ApplicationError? Error)
    where T : class
{
    /// <summary>
    /// Creates a result carrying the written value.
    /// </summary>
    /// <param name="value">The value that was written.</param>
    /// <returns>A result whose error is <see langword="null"/>.</returns>
    public static WriteResult<T> Success(T value)
    {
        return new WriteResult<T>(value, null);
    }

    /// <summary>
    /// Creates a result carrying the reason nothing was written.
    /// </summary>
    /// <param name="error">The failure that stopped the write.</param>
    /// <returns>A result whose value is <see langword="null"/>.</returns>
    public static WriteResult<T> Failure(ApplicationError error)
    {
        return new WriteResult<T>(null, error);
    }
}
