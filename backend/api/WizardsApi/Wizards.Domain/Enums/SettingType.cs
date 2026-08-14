namespace Wizards.Domain.Enums;

/// <summary>
/// The kind of value a game type setting holds.
/// </summary>
/// <remarks>
/// Every chosen value is stored as text regardless of kind, so this is what tells a reader how to
/// interpret that text.
/// </remarks>
public enum SettingType
{
    /// <summary>
    /// A whole number, optionally bounded by the setting's minimum and maximum.
    /// </summary>
    Int = 0,

    /// <summary>
    /// A true or false value, stored as <c>true</c> or <c>false</c> in lower case.
    /// </summary>
    Bool = 1,

    /// <summary>
    /// One value chosen from the fixed set the setting lists as its options.
    /// </summary>
    Enum = 2
}
