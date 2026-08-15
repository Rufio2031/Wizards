using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

using Microsoft.Net.Http.Headers;

using Wizards.Api.Filters;

namespace WizardsApi.Tests.Filters;

public sealed class SuccessResponseCacheAttributeTests
{
    private const int DurationSeconds = 60;

    private readonly SuccessResponseCacheAttribute attribute = new(DurationSeconds);

    [Fact]
    public void OnResultExecuting_ContextIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => this.attribute.OnResultExecuting(null!));
    }

    [Fact]
    public void OnResultExecuting_ResultCarriesNoStatusCodeOfItsOwn_WritesNoCacheDirective()
    {
        ResultExecutingContext context = BuildContext(new EmptyResult());

        this.attribute.OnResultExecuting(context);

        AssertNoCacheDirective(context);
    }

    [Fact]
    public void OnResultExecuting_ResultLeavesItsStatusCodeUnset_WritesNoCacheDirective()
    {
        ResultExecutingContext context = BuildContext(new JsonResult(new { }) { StatusCode = null });

        this.attribute.OnResultExecuting(context);

        AssertNoCacheDirective(context);
    }

    [Fact]
    public void OnResultExecuting_ResultIsNotFound_WritesNoCacheDirective()
    {
        ResultExecutingContext context = BuildContext(new NotFoundResult());

        this.attribute.OnResultExecuting(context);

        AssertNoCacheDirective(context);
    }

    [Theory]
    [InlineData(StatusCodes.Status100Continue)]
    [InlineData(199)]
    [InlineData(StatusCodes.Status300MultipleChoices)]
    [InlineData(StatusCodes.Status302Found)]
    [InlineData(StatusCodes.Status400BadRequest)]
    [InlineData(StatusCodes.Status401Unauthorized)]
    [InlineData(StatusCodes.Status500InternalServerError)]
    public void OnResultExecuting_ResultCarriesANonSuccessStatusCode_WritesNoCacheDirective(int statusCode)
    {
        ResultExecutingContext context = BuildContext(new StatusCodeResult(statusCode));

        this.attribute.OnResultExecuting(context);

        AssertNoCacheDirective(context);
    }

    [Fact]
    public void OnResultExecuting_ResultIsOk_MarksItCacheableByAnyCacheForTheConfiguredDuration()
    {
        ResultExecutingContext context = BuildContext(new OkObjectResult(new { }));

        this.attribute.OnResultExecuting(context);

        CacheControlHeaderValue? cacheControl = context.HttpContext.Response
            .GetTypedHeaders()
            .CacheControl;

        Assert.NotNull(cacheControl);
        Assert.True(cacheControl.Public);
        Assert.Equal(TimeSpan.FromSeconds(DurationSeconds), cacheControl.MaxAge);
        Assert.False(cacheControl.Private);
        Assert.False(cacheControl.NoStore);
    }

    [Theory]
    [InlineData(StatusCodes.Status200OK)]
    [InlineData(StatusCodes.Status201Created)]
    [InlineData(StatusCodes.Status204NoContent)]
    [InlineData(299)]
    public void OnResultExecuting_ResultCarriesASuccessStatusCode_WritesTheCacheDirective(int statusCode)
    {
        ResultExecutingContext context = BuildContext(new StatusCodeResult(statusCode));

        this.attribute.OnResultExecuting(context);

        Assert.Equal(
            $"public, max-age={DurationSeconds}",
            context.HttpContext.Response.Headers.CacheControl);
    }

    [Fact]
    public void OnResultExecuting_DurationIsConfiguredPerUsage_WritesThatDuration()
    {
        SuccessResponseCacheAttribute attribute = new(300);

        ResultExecutingContext context = BuildContext(new OkResult());

        attribute.OnResultExecuting(context);

        Assert.Equal("public, max-age=300", context.HttpContext.Response.Headers.CacheControl);
    }

    private static ResultExecutingContext BuildContext(IActionResult result)
    {
        ActionContext actionContext = new(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        return new ResultExecutingContext(actionContext, [], result, new object());
    }

    private static void AssertNoCacheDirective(ResultExecutingContext context)
    {
        Assert.False(context.HttpContext.Response.Headers.ContainsKey(HeaderNames.CacheControl));
    }
}
