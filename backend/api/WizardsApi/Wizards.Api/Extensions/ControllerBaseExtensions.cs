using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;

using Wizards.Application.Enums;
using Wizards.Application.Models;

namespace Wizards.Api.Extensions;

/// <summary>
/// Turns application failures into HTTP responses.
/// </summary>
public static class ControllerBaseExtensions
{
    /// <summary>
    /// Reports an application failure as the HTTP response its kind calls for.
    /// </summary>
    /// <remarks>
    /// The single place the API decides which status code an <see cref="ErrorKind"/> earns, so the
    /// Application layer never states one. A not-found failure produces the framework's bare response
    /// rather than one describing the field, matching every other 404 the API returns.
    /// </remarks>
    /// <param name="controller">The controller the response is produced on.</param>
    /// <param name="error">The failure to report.</param>
    /// <returns>The response carrying the failure.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="controller"/> or <paramref name="error"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="UnreachableException">Thrown when the failure carries an unhandled kind.</exception>
    public static ActionResult ToProblem(this ControllerBase controller, ApplicationError error)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(error);

        return error.Kind switch
        {
            ErrorKind.NotFound => controller.NotFound(),
            ErrorKind.Invalid => controller.ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { [error.Key] = [error.Message] })),
            ErrorKind.Conflict => controller.Conflict(),
            _ => throw new UnreachableException($"Unhandled error kind: {error.Kind}.")
        };
    }
}
