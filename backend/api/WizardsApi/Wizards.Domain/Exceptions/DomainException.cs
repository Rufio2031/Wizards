namespace Wizards.Domain.Exceptions;

/// <summary>
/// Thrown when a caller asks the domain for a state its rules forbid.
/// </summary>
/// <remarks>
/// Reports a mistake the caller can correct, such as an event that ends before it starts, so a
/// caller that can reach the originator is expected to catch it and report the message back. This
/// is deliberately distinct from the argument exceptions the domain throws for a broken
/// precondition, which report a programming error no caller can act on.
/// </remarks>
public class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    public DomainException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">
    /// The explanation of the rule that was broken. Safe to surface to the caller as-is and never
    /// carries internal implementation detail.
    /// </param>
    public DomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">
    /// The explanation of the rule that was broken. Safe to surface to the caller as-is and never
    /// carries internal implementation detail.
    /// </param>
    /// <param name="innerException">The exception that caused this one.</param>
    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
