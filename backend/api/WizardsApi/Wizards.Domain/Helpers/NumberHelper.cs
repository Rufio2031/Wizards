using System.Globalization;

namespace Wizards.Domain.Helpers;

/// <summary>
/// Converts whole numbers to and from text using the invariant culture.
/// </summary>
internal static class NumberHelper
{
    /// <summary>
    /// Reads a whole number from text.
    /// </summary>
    /// <param name="value">The text to read, which may carry a sign and surrounding whitespace.</param>
    /// <param name="parsed">The number the text carried, or zero when it carried none.</param>
    /// <returns>
    /// <see langword="true"/> when the text is a whole number, otherwise <see langword="false"/>.
    /// </returns>
    internal static bool TryParseInt(string? value, out int parsed) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);

    /// <summary>
    /// Writes a whole number as text.
    /// </summary>
    /// <param name="value">The number to write.</param>
    /// <returns>The number as text, in the form <see cref="TryParseInt"/> reads back.</returns>
    internal static string ToText(int value) => value.ToString(CultureInfo.InvariantCulture);
}
