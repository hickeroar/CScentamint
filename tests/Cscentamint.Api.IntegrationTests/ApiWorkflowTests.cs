using System.Net;
using System.Net.Http.Json;
using Cscentamint.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Cscentamint.Api.IntegrationTests;

public sealed class ApiWorkflowTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client = factory.CreateClient();

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
}
