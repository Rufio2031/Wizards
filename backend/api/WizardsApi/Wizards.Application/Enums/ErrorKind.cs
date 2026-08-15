namespace Wizards.Application.Enums;

/// <summary>
/// The category of an application error, stated in transport-neutral terms.
/// </summary>
public enum ErrorKind
{
    /// <summary>The request was understood but its content is not acceptable.</summary>
    Invalid = 0,

    /// <summary>The addressed resource does not exist.</summary>
    NotFound = 1,

    /// <summary>The request conflicts with the current state of the resource.</summary>
    Conflict = 2
}
