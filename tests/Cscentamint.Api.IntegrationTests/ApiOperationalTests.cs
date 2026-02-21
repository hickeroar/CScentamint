using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Cscentamint.Api.Contracts;
using Cscentamint.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cscentamint.Api.IntegrationTests;

/// <summary>
/// Integration tests for auth, probes, readiness, and request-size behavior.
/// </summary>
public sealed class ApiOperationalTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    /// <summary>
    /// Verifies health and readiness probes remain accessible when auth is enabled.
    /// </summary>
    [Fact]
    public async Task Probes_AreAccessible_WithoutAuthToken()
    {
        using var authFactory = CreateAuthFactory(factory);
        using var client = authFactory.CreateClient();

        var healthResponse = await client.GetAsync("/healthz");
        var readyResponse = await client.GetAsync("/readyz");

        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);
    }

    /// <summary>
    /// Verifies unauthorized requests are rejected with expected auth headers.
    /// </summary>
    [Fact]
    public async Task RootEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        using var authFactory = CreateAuthFactory(factory);
        using var client = authFactory.CreateClient();

        var response = await PostTextAsync(client, "/classify", "hello world");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer realm=\"cscentamint\"", response.Headers.WwwAuthenticate.ToString());
    }

    /// <summary>
    /// Verifies non-bearer authorization headers are rejected.
    /// </summary>
    [Fact]
    public async Task RootEndpoint_WithNonBearerHeader_ReturnsUnauthorized()
    {
        using var authFactory = CreateAuthFactory(factory);
        using var client = authFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "abc123");

        var response = await PostTextAsync(client, "/classify", "hello world");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Verifies bearer tokens with mismatched length are rejected.
    /// </summary>
    [Fact]
    public async Task RootEndpoint_WithWrongLengthBearerToken_ReturnsUnauthorized()
    {
        using var authFactory = CreateAuthFactory(factory);
        using var client = authFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "short");

        var response = await PostTextAsync(client, "/classify", "hello world");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Verifies authorized requests succeed when the bearer token matches configuration.
    /// </summary>
    [Fact]
    public async Task RootEndpoint_WithValidToken_Succeeds()
    {
        using var authFactory = CreateAuthFactory(factory);
        using var client = authFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "secret-token");

        await PostTextAsync(client, "/train/ham", "meeting calendar");
        var response = await PostTextAsync(client, "/classify", "calendar meeting");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<RootClassificationResponse>();
        Assert.NotNull(payload);
        Assert.Equal("ham", payload.Category);
    }

    /// <summary>
    /// Verifies readiness returns 503 after the service is marked not ready.
    /// </summary>
    [Fact]
    public async Task Readyz_ReturnsNotReady_WhenReadinessStateIsFlipped()
    {
        using var client = factory.CreateClient();
        var readiness = factory.Services.GetRequiredService<ReadinessState>();
        readiness.MarkNotReady();

        var response = await client.GetAsync("/readyz");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    /// <summary>
    /// Verifies root endpoints enforce the configured request body cap.
    /// </summary>
    [Fact]
    public async Task RootEndpoint_RequestBodyTooLarge_ReturnsPayloadTooLarge()
    {
        using var client = factory.CreateClient();
        var largeText = new string('a', checked((int)RootEndpointRequestSizeMiddleware.MaxRequestBodyBytes + 1));

        var response = await PostTextAsync(client, "/classify", largeText);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateAuthFactory(WebApplicationFactory<Program> baseFactory)
    {
        return baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:Token"] = "secret-token"
                });
            });
        });
    }

    private static async Task<HttpResponseMessage> PostTextAsync(HttpClient client, string url, string text)
    {
        using var content = new StringContent(text, Encoding.UTF8, "text/plain");
        return await client.PostAsync(url, content);
    }
}
