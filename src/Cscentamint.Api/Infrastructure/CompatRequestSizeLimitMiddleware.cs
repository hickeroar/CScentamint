namespace Cscentamint.Api.Infrastructure;

/// <summary>
/// Enforces gobayes-style body size limits on compatibility endpoints.
/// </summary>
public sealed class CompatRequestSizeLimitMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Maximum allowed request body bytes for gobayes compatibility endpoints.
    /// </summary>
    public const long MaxRequestBodyBytes = 1024 * 1024;

    /// <summary>
    /// Applies request size checks before endpoint execution.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (AppliesTo(context.Request) &&
            context.Request.ContentLength.HasValue &&
            context.Request.ContentLength.Value > MaxRequestBodyBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await context.Response.WriteAsJsonAsync(new { error = "request body too large" });
            return;
        }

        await next(context);
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
