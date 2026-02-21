using System.Text.Json;
using Cscentamint.Api.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cscentamint.Api.IntegrationTests;

/// <summary>
/// Direct tests for API exception handler branches.
/// </summary>
public sealed class ApiExceptionHandlerTests
{
    /// <summary>
    /// Verifies custom payload-too-large exception messages are retained.
    /// </summary>
    [Fact]
    public void PayloadTooLargeException_CustomMessage_Preserved()
    {
        var exception = new PayloadTooLargeException("custom payload message");

        Assert.Equal("custom payload message", exception.Message);
    }

    /// <summary>
    /// Verifies payload-too-large exceptions are converted to HTTP 413 with stable error payload.
    /// </summary>
    [Fact]
    public async Task TryHandleAsync_PayloadTooLargeException_ReturnsTrueAndWritesExpectedPayload()
    {
        var handler = new ApiExceptionHandler(new NullLogger<ApiExceptionHandler>());
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            context,
            new PayloadTooLargeException(),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        var payload = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(context.Response.Body);
        Assert.NotNull(payload);
        Assert.True(payload.TryGetValue("error", out var error));
        Assert.Equal("request body too large", error);
    }

    /// <summary>
    /// Verifies argument exceptions are converted to HTTP 400 ProblemDetails.
    /// </summary>
    [Fact]
    public async Task TryHandleAsync_ArgumentException_ReturnsTrueAndWritesProblemDetails()
    {
        var handler = new ApiExceptionHandler(new NullLogger<ApiExceptionHandler>());
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            context,
            new ArgumentException("Bad argument message.", "category"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body);
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Invalid request.", problem.Title);
        Assert.Contains("Bad argument message.", problem.Detail);
    }

    /// <summary>
    /// Verifies non-argument exceptions are not handled by this handler.
    /// </summary>
    [Fact]
    public async Task TryHandleAsync_NonArgumentException_ReturnsFalse()
    {
        var handler = new ApiExceptionHandler(new NullLogger<ApiExceptionHandler>());
        var context = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("boom"),
            CancellationToken.None);

        Assert.False(handled);
    }
}
