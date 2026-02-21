using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Cscentamint.Api.Infrastructure;

/// <summary>
/// Enforces optional bearer token auth for API requests.
/// </summary>
public sealed class BearerTokenMiddleware(RequestDelegate next, IOptions<AuthOptions> authOptions)
{
    private static readonly PathString HealthPath = new("/healthz");
    private static readonly PathString ReadyPath = new("/readyz");

    /// <summary>
    /// Validates bearer token when configured.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var configuredToken = authOptions.Value.Token;
        if (string.IsNullOrWhiteSpace(configuredToken) || IsProbePath(context.Request.Path))
        {
            await next(context);
            return;
        }

        if (!TryGetBearerToken(context.Request, out var providedToken) || !FixedTimeEquals(configuredToken, providedToken))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer realm=\"cscentamint\"";
            await context.Response.WriteAsJsonAsync(new { error = "unauthorized" });
            return;
        }

        await next(context);
    }

    private static bool TryGetBearerToken(HttpRequest request, out string token)
    {
        token = string.Empty;
        if (!request.Headers.TryGetValue("Authorization", out var authValue))
        {
            return false;
        }

        var header = authValue.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        token = header["Bearer ".Length..].Trim();
        return token.Length > 0;
    }

    private static bool IsProbePath(PathString path)
    {
        return path.Equals(HealthPath, StringComparison.OrdinalIgnoreCase) ||
            path.Equals(ReadyPath, StringComparison.OrdinalIgnoreCase);
    }

    private static bool FixedTimeEquals(string expected, string actual)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        var comparisonLength = Math.Max(expectedBytes.Length, actualBytes.Length);
        var paddedExpected = new byte[comparisonLength];
        var paddedActual = new byte[comparisonLength];

        Buffer.BlockCopy(expectedBytes, 0, paddedExpected, 0, expectedBytes.Length);
        Buffer.BlockCopy(actualBytes, 0, paddedActual, 0, actualBytes.Length);

        var valuesMatch = CryptographicOperations.FixedTimeEquals(paddedExpected, paddedActual);
        var lengthsMatch = expectedBytes.Length == actualBytes.Length;

        return valuesMatch && lengthsMatch;
    }
}
