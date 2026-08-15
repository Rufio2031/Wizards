using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;

namespace Wizards.Application.Validation;

/// <summary>
/// Requires a value that is supplied rather than left at its type's default, such as a
/// <see cref="DateTime"/> at <see cref="DateTime.MinValue"/> or an empty <see cref="Guid"/>.
/// </summary>
/// <remarks>A reference type passes whenever it is not null, whatever it carries.</remarks>
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
    AllowMultiple = false)]
public sealed class RequiredValueAttribute : ValidationAttribute
{
    private static readonly ConcurrentDictionary<Type, object?> Defaults = new();

    /// <summary>Initializes an attribute reporting the field as missing when it fails.</summary>
    public RequiredValueAttribute()
        : base("The {0} field is required.")
    {
    }

    /// <summary>Reports whether the value was supplied.</summary>
    /// <param name="value">The value to check, where null counts as missing.</param>
    /// <returns><see langword="true"/> when the value is neither null nor its type's default.</returns>
    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return false;
        }

        Type type = value.GetType();

        if (!type.IsValueType)
        {
            return true;
        }

        return !value.Equals(Defaults.GetOrAdd(type, static valueType => Activator.CreateInstance(valueType)));
    }
}
