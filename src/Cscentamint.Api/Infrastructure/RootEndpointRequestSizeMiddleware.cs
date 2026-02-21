namespace Cscentamint.Api.Infrastructure;

/// <summary>
/// Enforces body size limits on root text endpoints.
/// </summary>
public sealed class RootEndpointRequestSizeMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Maximum allowed request body bytes for root text endpoints.
    /// </summary>
    public const long MaxRequestBodyBytes = 1024 * 1024;

    /// <summary>
    /// Applies request size checks before endpoint execution.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!AppliesTo(context.Request))
        {
            await next(context);
            return;
        }

        if (context.Request.ContentLength.HasValue &&
            context.Request.ContentLength.Value > MaxRequestBodyBytes)
        {
            await WritePayloadTooLargeAsync(context.Response);
            return;
        }

        if (!context.Request.ContentLength.HasValue &&
            await ExceedsMaxBodySizeAsync(context.Request))
        {
            await WritePayloadTooLargeAsync(context.Response);
            return;
        }

        await next(context);
    }

    private static async Task<bool> ExceedsMaxBodySizeAsync(HttpRequest request)
    {
        request.EnableBuffering();
        request.Body.Position = 0;

        var totalBytesRead = 0L;
        var buffer = new byte[16 * 1024];
        while (true)
        {
            var bytesRead = await request.Body.ReadAsync(buffer);
            if (bytesRead == 0)
            {
                request.Body.Position = 0;
                return false;
            }

            totalBytesRead += bytesRead;
            if (totalBytesRead > MaxRequestBodyBytes)
            {
                request.Body.Position = 0;
                return true;
            }
        }
    }

    private static Task WritePayloadTooLargeAsync(HttpResponse response)
    {
        response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        return response.WriteAsJsonAsync(new { error = "request body too large" });
    }

    private static bool AppliesTo(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method))
        {
            return false;
        }

        var path = request.Path;
        return path.StartsWithSegments("/train", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/untrain", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/classify", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/score", StringComparison.OrdinalIgnoreCase);
    }
}
