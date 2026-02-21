using Cscentamint.Api.Controllers;
using Cscentamint.Api.Infrastructure;
using Cscentamint.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Cscentamint.Api.IntegrationTests;

/// <summary>
/// Unit-style tests for root controller request-body safeguards.
/// </summary>
public sealed class RootTextControllerTests
{
    /// <summary>
    /// Verifies controller-level body guard rejects oversized payloads even outside middleware.
    /// </summary>
    [Fact]
    public async Task Classify_RequestBodyTooLarge_ThrowsPayloadTooLargeException()
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        var controller = new RootTextController(classifier)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        controller.HttpContext.Request.Body = new MemoryStream(
            new byte[checked((int)RootEndpointRequestSizeMiddleware.MaxRequestBodyBytes + 1)]);
        controller.HttpContext.Request.ContentType = "text/plain";

        var exception = await Assert.ThrowsAsync<PayloadTooLargeException>(() => controller.Classify());

        Assert.Equal("request body too large", exception.Message);
    }
}
