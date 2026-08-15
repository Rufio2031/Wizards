namespace Wizards.Domain.Exceptions;

/// <summary>
/// Thrown when the database rejects a save because a row already holds the values a unique
/// constraint covers. Nothing is saved. Catch it and read the existing row or return your own error;
/// the message describes the failed write, not the rule, so do not show it to the client.
/// </summary>
public class StoreUniquenessViolationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StoreUniquenessViolationException"/> class.
    /// </summary>
    public StoreUniquenessViolationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StoreUniquenessViolationException"/> class.
    /// </summary>
    /// <param name="message">Describes the write the database rejected. Not for client display.</param>
    public StoreUniquenessViolationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StoreUniquenessViolationException"/> class.
    /// </summary>
    /// <param name="message">Describes the write the database rejected. Not for client display.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public StoreUniquenessViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
