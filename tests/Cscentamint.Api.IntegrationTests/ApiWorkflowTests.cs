using System.Net;
using System.Net.Http.Json;
using Cscentamint.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Cscentamint.Api.IntegrationTests;

/// <summary>
/// End-to-end API workflow tests using an in-memory ASP.NET host.
/// </summary>
/// <param name="factory">Factory used to create HTTP clients for the API.</param>
public sealed class ApiWorkflowTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client = factory.CreateClient();

    /// <summary>
    /// Verifies that training data influences later classification.
    /// </summary>
    [Fact]
    public async Task TrainThenClassify_ReturnsTrainedCategory()
    {
        await client.DeleteAsync("/api/model");

        var trainResponse = await client.PostAsJsonAsync(
            "/api/categories/spam/samples",
            new TextDocumentRequest { Text = "buy now limited offer" });

        Assert.Equal(HttpStatusCode.NoContent, trainResponse.StatusCode);

        var classifyResponse = await client.PostAsJsonAsync(
            "/api/classifications",
            new TextDocumentRequest { Text = "limited offer now" });

        classifyResponse.EnsureSuccessStatusCode();
        var payload = await classifyResponse.Content.ReadFromJsonAsync<ClassificationResponse>();

        Assert.NotNull(payload);
        Assert.Equal("spam", payload.Category);
    }

    /// <summary>
    /// Verifies invalid category route values return problem details.
    /// </summary>
    [Fact]
    public async Task InvalidCategory_ReturnsProblemDetails()
    {
        var response = await client.PostAsJsonAsync(
            "/api/categories/!!!/samples",
            new TextDocumentRequest { Text = "anything" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
    }

    /// <summary>
    /// Verifies score endpoint returns per-category values for trained samples.
    /// </summary>
    [Fact]
    public async Task Scores_ReturnsCategoryScores()
    {
        await client.DeleteAsync("/api/model");
        await client.PostAsJsonAsync(
            "/api/categories/ham/samples",
            new TextDocumentRequest { Text = "meeting schedule calendar" });

        var response = await client.PostAsJsonAsync(
            "/api/scores",
            new TextDocumentRequest { Text = "calendar meeting" });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, float>>();
        Assert.NotNull(payload);
        Assert.True(payload.ContainsKey("ham"));
        Assert.True(payload["ham"] > 0f);
    }

    /// <summary>
    /// Verifies remove-sample endpoint executes and allows classification fallback to null.
    /// </summary>
    [Fact]
    public async Task RemoveSample_ThenClassify_ReturnsNullCategory()
    {
        await client.DeleteAsync("/api/model");
        await client.PostAsJsonAsync(
            "/api/categories/spam/samples",
            new TextDocumentRequest { Text = "buy now limited offer" });

        var removeRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/categories/spam/samples")
        {
            Content = JsonContent.Create(new TextDocumentRequest { Text = "buy now limited offer" })
        };

        var removeResponse = await client.SendAsync(removeRequest);
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        var classifyResponse = await client.PostAsJsonAsync(
            "/api/classifications",
            new TextDocumentRequest { Text = "limited offer now" });
        classifyResponse.EnsureSuccessStatusCode();
        var payload = await classifyResponse.Content.ReadFromJsonAsync<ClassificationResponse>();
        Assert.NotNull(payload);
        Assert.Null(payload.Category);
    }

    /// <summary>
    /// Verifies request validation returns a validation problem for empty text.
    /// </summary>
    [Fact]
    public async Task EmptyText_ReturnsValidationProblem()
    {
        var response = await client.PostAsJsonAsync(
            "/api/scores",
            new TextDocumentRequest { Text = string.Empty });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var validation = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(validation);
    }

    /// <summary>
    /// Verifies API classification uses token tally weighted priors.
    /// </summary>
    [Fact]
    public async Task Classify_PrefersCategoryWithHigherTokenTally_WhenEvidenceIsEqual()
    {
        await client.DeleteAsync("/api/model");
        await client.PostAsJsonAsync(
            "/api/categories/heavy/samples",
            new TextDocumentRequest { Text = "topic topic topic topic topic" });
        await client.PostAsJsonAsync(
            "/api/categories/light/samples",
            new TextDocumentRequest { Text = "topic" });

        var classifyResponse = await client.PostAsJsonAsync(
            "/api/classifications",
            new TextDocumentRequest { Text = "topic" });
        classifyResponse.EnsureSuccessStatusCode();

        var payload = await classifyResponse.Content.ReadFromJsonAsync<ClassificationResponse>();
        Assert.NotNull(payload);
        Assert.Equal("heavy", payload.Category);
    }

    /// <summary>
    /// Verifies API classification tie-breaking is deterministic by lexical category order.
    /// </summary>
    [Fact]
    public async Task Classify_TieBreaksByLexicalCategoryName()
    {
        await client.DeleteAsync("/api/model");
        await client.PostAsJsonAsync(
            "/api/categories/zulu/samples",
            new TextDocumentRequest { Text = "alpha beta" });
        await client.PostAsJsonAsync(
            "/api/categories/apple/samples",
            new TextDocumentRequest { Text = "alpha beta" });

        var classifyResponse = await client.PostAsJsonAsync(
            "/api/classifications",
            new TextDocumentRequest { Text = "alpha beta" });
        classifyResponse.EnsureSuccessStatusCode();

        var payload = await classifyResponse.Content.ReadFromJsonAsync<ClassificationResponse>();
        Assert.NotNull(payload);
        Assert.Equal("apple", payload.Category);
    }

    /// <summary>
    /// Verifies API classification uses normalized casing and punctuation splitting.
    /// </summary>
    [Fact]
    public async Task Classify_UsesDefaultTokenizerNormalizationPipeline()
    {
        await client.DeleteAsync("/api/model");
        await client.PostAsJsonAsync(
            "/api/categories/ham/samples",
            new TextDocumentRequest { Text = "hello world" });

        var classifyResponse = await client.PostAsJsonAsync(
            "/api/classifications",
            new TextDocumentRequest { Text = "HELLO, WORLD!" });
        classifyResponse.EnsureSuccessStatusCode();

        var payload = await classifyResponse.Content.ReadFromJsonAsync<ClassificationResponse>();
        Assert.NotNull(payload);
        Assert.Equal("ham", payload.Category);
    }

    /// <summary>
    /// Verifies API classification uses stemming for common English inflections.
    /// </summary>
    [Fact]
    public async Task Classify_UsesDefaultTokenizerStemming()
    {
        await client.DeleteAsync("/api/model");
        await client.PostAsJsonAsync(
            "/api/categories/promo/samples",
            new TextDocumentRequest { Text = "offer" });

        var classifyResponse = await client.PostAsJsonAsync(
            "/api/classifications",
            new TextDocumentRequest { Text = "offering offers offered" });
        classifyResponse.EnsureSuccessStatusCode();

        var payload = await classifyResponse.Content.ReadFromJsonAsync<ClassificationResponse>();
        Assert.NotNull(payload);
        Assert.Equal("promo", payload.Category);
    }

    /// <summary>
    /// Verifies model reset endpoint returns no-content and clears learned state.
    /// </summary>
    [Fact]
    public async Task DeleteModel_ReturnsNoContent_AndResetsClassifierState()
    {
        await client.PostAsJsonAsync(
            "/api/categories/spam/samples",
            new TextDocumentRequest { Text = "buy now limited offer" });

        var resetResponse = await client.DeleteAsync("/api/model");
        Assert.Equal(HttpStatusCode.NoContent, resetResponse.StatusCode);

        var classifyResponse = await client.PostAsJsonAsync(
            "/api/classifications",
            new TextDocumentRequest { Text = "limited offer now" });
        classifyResponse.EnsureSuccessStatusCode();
        var payload = await classifyResponse.Content.ReadFromJsonAsync<ClassificationResponse>();
        Assert.NotNull(payload);
        Assert.Null(payload.Category);
    }
}
