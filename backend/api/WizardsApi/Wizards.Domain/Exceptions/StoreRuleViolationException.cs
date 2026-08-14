namespace Wizards.Domain.Exceptions;

/// <summary>
/// Thrown when the database rejects a save for violating a constraint it enforces, such as the
/// trigger blocking registrations for a full event. Nothing is saved. Catch it and return your own
/// error; the message describes the failed write, not the rule, so do not show it to the client.
/// </summary>
public class StoreRuleViolationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StoreRuleViolationException"/> class.
    /// </summary>
    public StoreRuleViolationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StoreRuleViolationException"/> class.
    /// </summary>
    /// <param name="message">Describes the write the database rejected. Not for client display.</param>
    public StoreRuleViolationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StoreRuleViolationException"/> class.
    /// </summary>
    /// <param name="message">Describes the write the database rejected. Not for client display.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public StoreRuleViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
