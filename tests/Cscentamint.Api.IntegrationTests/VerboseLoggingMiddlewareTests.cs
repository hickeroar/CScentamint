using System.Text;
using Cscentamint.Api.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Cscentamint.Api.IntegrationTests;

/// <summary>
/// Tests for VerboseLoggingMiddleware.
/// </summary>
public sealed class VerboseLoggingMiddlewareTests
{
    /// <summary>
    /// Verifies middleware calls next and logs request and response.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_CallsNext_AndLogsRequestAndResponse()
    {
        var sink = new ListLoggerSink();
        var logger = new ListLogger(sink);
        var nextInvoked = false;
        var middleware = new VerboseLoggingMiddleware(_ =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        }, logger);

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/healthz";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.True(nextInvoked);
        Assert.Contains(sink.Messages, m => m.Contains("[cscentamint]") && m.Contains("GET") && m.Contains("/healthz"));
        Assert.Contains(sink.Messages, m => m.Contains("response") && m.Contains("200"));
    }

    /// <summary>
    /// Verifies middleware buffers response body and returns it to the client.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WhenNextWritesResponseBody_ReturnsSameBodyToClient()
    {
        var logger = new ListLogger(new ListLoggerSink());
        var expectedBody = "{\"status\":\"ok\"}";
        var middleware = new VerboseLoggingMiddleware(async ctx =>
        {
            ctx.Response.StatusCode = 200;
            await ctx.Response.WriteAsync(expectedBody);
        }, logger);

        var context = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        await middleware.InvokeAsync(context);

        responseStream.Position = 0;
        var actualBody = await new StreamReader(responseStream).ReadToEndAsync();
        Assert.Equal(expectedBody, actualBody);
    }

    /// <summary>
    /// Verifies request body preview is logged when present.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WhenRequestHasBody_LogsBodyPreview()
    {
        var sink = new ListLoggerSink();
        var logger = new ListLogger(sink);
        var middleware = new VerboseLoggingMiddleware(ctx => Task.CompletedTask, logger);

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/classify";
        context.Request.ContentType = "text/plain";
        var bodyBytes = Encoding.UTF8.GetBytes("hello world");
        context.Request.Body = new MemoryStream(bodyBytes);
        context.Request.ContentLength = bodyBytes.Length;
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Contains(sink.Messages, m => m.Contains("request body") && m.Contains("hello world"));
    }

    /// <summary>
    /// Verifies that when Verbose is enabled, the app runs with VerboseLoggingMiddleware and responds.
    /// Uses CSCENTAMINT_VERBOSE env var because ConfigureAppConfiguration runs too late for Program startup.
    /// </summary>
    [Fact]
    public async Task Integration_WhenVerboseEnabled_AppResponds()
    {
        var previous = Environment.GetEnvironmentVariable("CSCENTAMINT_VERBOSE");
        try
        {
            Environment.SetEnvironmentVariable("CSCENTAMINT_VERBOSE", "true");
            var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            var response = await client.GetAsync("/healthz");
            response.EnsureSuccessStatusCode();
        }
        finally
        {
            Environment.SetEnvironmentVariable("CSCENTAMINT_VERBOSE", previous ?? "");
        }
    }

    /// <summary>
    /// Verifies GetPreview truncates when body exceeds PreviewMaxChars (covers both branches).
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WhenResponseBodyExceedsPreviewLength_LogsTruncatedPreview()
    {
        var sink = new ListLoggerSink();
        var logger = new ListLogger(sink);
        var longBody = new string('x', 300);
        var middleware = new VerboseLoggingMiddleware(async ctx =>
        {
            ctx.Response.StatusCode = 200;
            await ctx.Response.WriteAsync(longBody);
        }, logger);

        var context = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        await middleware.InvokeAsync(context);

        Assert.Contains(sink.Messages, m => m.Contains("response body") && m.Contains("300") && m.Contains("..."));
    }

    /// <summary>
    /// Verifies request with ContentLength 0 does not enter body-logging block.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WhenRequestHasNoBody_DoesNotLogRequestBody()
    {
        var sink = new ListLoggerSink();
        var logger = new ListLogger(sink);
        var middleware = new VerboseLoggingMiddleware(ctx => Task.CompletedTask, logger);

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/healthz";
        context.Request.ContentLength = 0;
        context.Request.Body = new MemoryStream();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.DoesNotContain(sink.Messages, m => m.Contains("request body ("));
    }

    private sealed class ListLoggerSink
    {
        public List<string> Messages { get; } = [];
    }

    private sealed class ListLogger : ILogger<VerboseLoggingMiddleware>
    {
        private readonly ListLoggerSink _sink;

        public ListLogger(ListLoggerSink sink) => _sink = sink;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _sink.Messages.Add(formatter(state, exception));
        }
    }
}