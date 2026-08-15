using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

using Microsoft.Net.Http.Headers;

namespace Wizards.Api.Filters;

/// <summary>
/// Marks a successful response as cacheable by any cache for a number of seconds. A response carrying
/// any other status is left without a cache directive.
/// </summary>
/// <param name="durationSeconds">How long a cache may reuse the response, in seconds.</param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class SuccessResponseCacheAttribute(int durationSeconds) : Attribute, IResultFilter
{
    /// <inheritdoc />
    public void OnResultExecuting(ResultExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        int? statusCode = (context.Result as IStatusCodeActionResult)?.StatusCode;

        if (statusCode is not (>= StatusCodes.Status200OK and < StatusCodes.Status300MultipleChoices))
        {
            return;
        }

        ResponseHeaders headers = context.HttpContext.Response.GetTypedHeaders();

        headers.CacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromSeconds(durationSeconds)
        };
    }

    /// <inheritdoc />
    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}
