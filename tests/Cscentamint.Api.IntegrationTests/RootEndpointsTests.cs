using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cscentamint.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Cscentamint.Api.IntegrationTests;

/// <summary>
/// Integration tests for root text endpoints.
/// </summary>
public sealed class RootEndpointsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client = factory.CreateClient();

    /// <summary>
    /// Verifies train and info endpoints return category summaries.
    /// </summary>
    [Fact]
    public async Task Train_ThenInfo_ReturnsCategorySummaries()
    {
        await PostTextAsync("/flush", string.Empty);
        var trainResponse = await PostTextAsync("/train/spam", "buy now limited offer");
        trainResponse.EnsureSuccessStatusCode();

        var trainPayload = await trainResponse.Content.ReadFromJsonAsync<RootMutationResponse>();
        Assert.NotNull(trainPayload);
        Assert.True(trainPayload.Success);
        Assert.True(trainPayload.Categories.ContainsKey("spam"));

        var infoResponse = await client.GetAsync("/info");
        infoResponse.EnsureSuccessStatusCode();
        var infoPayload = await infoResponse.Content.ReadFromJsonAsync<RootInfoResponse>();

        Assert.NotNull(infoPayload);
        Assert.True(infoPayload.Categories.TryGetValue("spam", out var summary));
        Assert.True(summary.TokenTally > 0);
    }

    /// <summary>
    /// Verifies classify and score endpoints return expected data.
    /// </summary>
    [Fact]
    public async Task ClassifyAndScore_ReturnExpectedPayloads()
    {
        await PostTextAsync("/flush", string.Empty);
        await PostTextAsync("/train/ham", "calendar meeting notes");

        var classifyResponse = await PostTextAsync("/classify", "calendar meeting");
        classifyResponse.EnsureSuccessStatusCode();
        var classifyPayload = await classifyResponse.Content.ReadFromJsonAsync<RootClassificationResponse>();
        Assert.NotNull(classifyPayload);
        Assert.Equal("ham", classifyPayload.Category);
        Assert.True(classifyPayload.Score > 0f);

        var scoreResponse = await PostTextAsync("/score", "calendar meeting");
        scoreResponse.EnsureSuccessStatusCode();
        var scorePayload = await scoreResponse.Content.ReadFromJsonAsync<Dictionary<string, float>>();
        Assert.NotNull(scorePayload);
        Assert.True(scorePayload.TryGetValue("ham", out var score));
        Assert.True(score > 0f);
    }

    /// <summary>
    /// Verifies untrain and flush reset model state.
    /// </summary>
    [Fact]
    public async Task UntrainAndFlush_ClearsModelState()
    {
        await PostTextAsync("/flush", string.Empty);
        await PostTextAsync("/train/spam", "buy now");
        await PostTextAsync("/untrain/spam", "buy now");

        var classifyResponse = await PostTextAsync("/classify", "buy now");
        classifyResponse.EnsureSuccessStatusCode();
        var classifyPayload = await classifyResponse.Content.ReadFromJsonAsync<RootClassificationResponse>();
        Assert.NotNull(classifyPayload);
        Assert.Equal(string.Empty, classifyPayload.Category);
        Assert.Equal(0f, classifyPayload.Score);

        var flushResponse = await PostTextAsync("/flush", string.Empty);
        flushResponse.EnsureSuccessStatusCode();
        var flushPayload = await flushResponse.Content.ReadFromJsonAsync<RootMutationResponse>();
        Assert.NotNull(flushPayload);
        Assert.True(flushPayload.Success);
        Assert.Empty(flushPayload.Categories);
    }

    /// <summary>
    /// Verifies invalid route categories return 404.
    /// </summary>
    [Fact]
    public async Task Train_InvalidCategoryRoute_ReturnsNotFound()
    {
        var response = await PostTextAsync("/train/!!!", "any");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Verifies valid maximum-length category names are accepted by root route constraints.
    /// </summary>
    [Fact]
    public async Task Train_MaxLengthCategoryRouteValue_Succeeds()
    {
        await PostTextAsync("/flush", string.Empty);
        var category = new string('a', 64);

        var response = await PostTextAsync($"/train/{category}", "valid sample text");

        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Verifies invalid bracket characters are rejected by root route category constraints.
    /// </summary>
    [Fact]
    public async Task Train_BracketedCategoryRouteValue_ReturnsNotFound()
    {
        var response = await PostTextAsync("/train/[spam]", "any");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Verifies wrong method is rejected with method-not-allowed.
    /// </summary>
    [Fact]
    public async Task Train_WrongMethod_ReturnsMethodNotAllowed()
    {
        var response = await client.GetAsync("/train/spam");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Contains(HttpMethod.Post.Method, response.Content.Headers.Allow);
    }

    /// <summary>
    /// Verifies root endpoints handle concurrent train/score traffic.
    /// </summary>
    [Fact]
    public async Task RootEndpoints_HandleConcurrentTrainAndScoreRequests()
    {
        await PostTextAsync("/flush", string.Empty);

        var tasks = Enumerable.Range(0, 20).Select(async i =>
        {
            await PostTextAsync("/train/loadtest", $"sample text {i}");
            var response = await PostTextAsync("/score", "sample text");
            response.EnsureSuccessStatusCode();
        });

        await Task.WhenAll(tasks);
    }

    private async Task<HttpResponseMessage> PostTextAsync(string url, string text)
    {
        using var content = new StringContent(text);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        return await client.PostAsync(url, content);
    }
}
