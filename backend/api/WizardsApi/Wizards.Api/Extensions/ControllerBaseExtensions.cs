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
            ErrorKind.Invalid => controller.ValidationProblem(ToProblemDetails(error)),
            ErrorKind.Conflict => controller.Conflict(ToProblemDetails(
                error,
                StatusCodes.Status409Conflict)),
            _ => throw new UnreachableException($"Unhandled error kind: {error.Kind}.")
        };
    }

    private static ValidationProblemDetails ToProblemDetails(ApplicationError error, int? status = null)
    {
        return new ValidationProblemDetails(
            new Dictionary<string, string[]> { [error.Key] = [error.Message] })
        {
            Status = status
        };
    }
}
