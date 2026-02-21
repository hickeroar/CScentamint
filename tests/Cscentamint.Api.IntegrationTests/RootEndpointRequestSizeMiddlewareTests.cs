using Cscentamint.Api.Infrastructure;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Cscentamint.Api.IntegrationTests;

/// <summary>
/// Focused tests for root request-size middleware behaviors.
/// </summary>
public sealed class RootEndpointRequestSizeMiddlewareTests
{
    /// <summary>
    /// Verifies unknown-length root requests above the cap are rejected before next middleware.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_RootPostWithoutContentLength_OverLimit_ReturnsPayloadTooLarge()
    {
        var nextInvoked = false;
        var middleware = new RootEndpointRequestSizeMiddleware(_ =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/classify";
        context.Request.Body = new MemoryStream(
            new byte[checked((int)RootEndpointRequestSizeMiddleware.MaxRequestBodyBytes + 1)]);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, context.Response.StatusCode);
        Assert.False(nextInvoked);
    }

    /// <summary>
    /// Verifies unknown-length root requests under the cap continue with body position reset.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_RootPostWithoutContentLength_WithinLimit_CallsNext()
    {
        var nextInvoked = false;
        long bodyPositionSeenByNext = -1;
        var middleware = new RootEndpointRequestSizeMiddleware(context =>
        {
            nextInvoked = true;
            bodyPositionSeenByNext = context.Request.Body.Position;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/classify";
        context.Request.Body = new MemoryStream(
            new byte[checked((int)RootEndpointRequestSizeMiddleware.MaxRequestBodyBytes - 1)]);

        await middleware.InvokeAsync(context);

        Assert.True(nextInvoked);
        Assert.Equal(0, bodyPositionSeenByNext);
    }
}
