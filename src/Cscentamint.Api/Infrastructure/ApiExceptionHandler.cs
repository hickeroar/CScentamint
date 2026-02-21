using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Cscentamint.Api.Infrastructure;

/// <summary>
/// Maps known exceptions to API-friendly problem details responses.
/// </summary>
/// <param name="logger">Logger for unhandled exceptions.</param>
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>
    /// Attempts to handle an exception for the current HTTP request.
    /// </summary>
    /// <param name="httpContext">HTTP context for the active request.</param>
    /// <param name="exception">Unhandled exception.</param>
    /// <param name="cancellationToken">Cancellation token for the request pipeline.</param>
    /// <returns><c>true</c> when the exception is handled; otherwise <c>false</c>.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ArgumentException argumentException)
        {
            var details = new ProblemDetails
            {
                Title = "Invalid request.",
                Detail = argumentException.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = "https://httpstatuses.com/400"
            };

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(details, cancellationToken);
            return true;
        }

        logger.LogError(exception, "Unhandled exception while processing API request.");
        return false;
    }
}
