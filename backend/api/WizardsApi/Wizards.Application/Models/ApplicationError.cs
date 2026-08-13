using Wizards.Application.Enums;

namespace Wizards.Application.Models;

/// <summary>
/// A failure an application service reports to its caller, described independently of any transport.
/// </summary>
/// <param name="Kind">The category the failure falls into, which decides how it is reported.</param>
/// <param name="Key">
/// The path of the field the failure is attributed to, in the shape a client sends it, such as
/// <c>gameType.name</c>.
/// </param>
/// <param name="Message">
/// The explanation shown to the caller. Safe to surface as-is and never carries internal
/// implementation detail.
/// </param>
public sealed record ApplicationError(ErrorKind Kind, string Key, string Message);
