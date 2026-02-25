namespace Cscentamint.Api.Infrastructure;

/// <summary>
/// When verbose mode is enabled, logs request method/path and body preview, then response status and body preview to the logger (stderr in console).
/// </summary>
public sealed class VerboseLoggingMiddleware
{
    private const int PreviewMaxChars = 200;
    private readonly RequestDelegate _next;
    private readonly ILogger<VerboseLoggingMiddleware> _logger;

    /// <summary>
    /// Creates the middleware.
    /// </summary>
    public VerboseLoggingMiddleware(RequestDelegate next, ILogger<VerboseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware: logs request, calls next, then logs response.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.ToString();

        _logger.LogInformation("[cscentamint] {Method} {Path}", method, path);

        if (context.Request.ContentLength.GetValueOrDefault(0) != 0 || !context.Request.ContentLength.HasValue)
        {
            context.Request.EnableBuffering();
            context.Request.Body.Position = 0;
            var previewBuffer = new byte[PreviewMaxChars * 4]; // enough for 200 UTF-8 chars
            var read = await context.Request.Body.ReadAsync(previewBuffer);
            context.Request.Body.Position = 0;
            var bodyLength = context.Request.ContentLength ?? read;
            _logger.LogInformation("[cscentamint] request body ({Length} bytes): {Preview}", bodyLength, GetPreview(previewBuffer, read));
        }

        var originalBody = context.Response.Body;
        await using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        await _next(context);

        var status = context.Response.StatusCode;
        responseBuffer.Position = 0;
        var responseLength = responseBuffer.Length;
        _logger.LogInformation("[cscentamint] response {Status}", status);
        if (responseLength > 0)
        {
            var previewBuffer = new byte[Math.Min((int)responseLength, PreviewMaxChars * 4)];
            var read = await responseBuffer.ReadAsync(previewBuffer.AsMemory(0, previewBuffer.Length));
            var preview = GetPreview(previewBuffer, read);
            _logger.LogInformation("[cscentamint] response body ({Length} bytes): {Preview}", responseLength, preview);
        }

        responseBuffer.Position = 0;
        await responseBuffer.CopyToAsync(originalBody);
        context.Response.Body = originalBody;
    }

    private static string GetPreview(byte[] buffer, int length)
    {
        var text = System.Text.Encoding.UTF8.GetString(buffer.AsSpan(0, length));
        return text.Length > PreviewMaxChars ? text[..PreviewMaxChars] + "..." : text;
    }
}
