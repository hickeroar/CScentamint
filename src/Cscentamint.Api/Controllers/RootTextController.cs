using System.Text;
using Cscentamint.Api.Contracts;
using Cscentamint.Core;
using Microsoft.AspNetCore.Mvc;

namespace Cscentamint.Api.Controllers;

/// <summary>
/// Provides root text endpoints for train, score, classify, and model inspection.
/// </summary>
/// <param name="classifier">Classifier service.</param>
[ApiController]
[Route("")]
public sealed class RootTextController(ITextClassifier classifier) : ControllerBase
{
    /// <summary>
    /// Returns category summaries.
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(typeof(RootInfoResponse), StatusCodes.Status200OK)]
    public ActionResult<RootInfoResponse> Info()
    {
        return Ok(new RootInfoResponse
        {
            Categories = MapSummaries(classifier.GetSummaries())
        });
    }

    /// <summary>
    /// Trains a category from raw request body text.
    /// </summary>
    [HttpPost("train/{category:regex(^[[-_A-Za-z0-9]]+$)}")]
    [ProducesResponseType(typeof(RootMutationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RootMutationResponse>> Train([FromRoute] string category)
    {
        var text = await ReadBodyAsTextAsync();
        classifier.Train(category, text);
        return Ok(CreateSuccessResponse());
    }

    /// <summary>
    /// Untrains a category from raw request body text.
    /// </summary>
    [HttpPost("untrain/{category:regex(^[[-_A-Za-z0-9]]+$)}")]
    [ProducesResponseType(typeof(RootMutationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RootMutationResponse>> Untrain([FromRoute] string category)
    {
        var text = await ReadBodyAsTextAsync();
        classifier.Untrain(category, text);
        return Ok(CreateSuccessResponse());
    }

    /// <summary>
    /// Classifies raw request body text.
    /// </summary>
    [HttpPost("classify")]
    [ProducesResponseType(typeof(RootClassificationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RootClassificationResponse>> Classify()
    {
        var text = await ReadBodyAsTextAsync();
        var prediction = classifier.Classify(text);
        return Ok(new RootClassificationResponse
        {
            Category = prediction.PredictedCategory ?? string.Empty,
            Score = prediction.Score
        });
    }

    /// <summary>
    /// Scores raw request body text.
    /// </summary>
    [HttpPost("score")]
    [ProducesResponseType(typeof(IReadOnlyDictionary<string, float>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyDictionary<string, float>>> Score()
    {
        var text = await ReadBodyAsTextAsync();
        return Ok(classifier.GetScores(text));
    }

    /// <summary>
    /// Flushes all classifier state.
    /// </summary>
    [HttpPost("flush")]
    [ProducesResponseType(typeof(RootMutationResponse), StatusCodes.Status200OK)]
    public ActionResult<RootMutationResponse> Flush()
    {
        classifier.Reset();
        return Ok(CreateSuccessResponse());
    }

    private RootMutationResponse CreateSuccessResponse()
    {
        return new RootMutationResponse
        {
            Success = true,
            Categories = MapSummaries(classifier.GetSummaries())
        };
    }

    private static IReadOnlyDictionary<string, RootCategorySummaryResponse> MapSummaries(
        IReadOnlyDictionary<string, CategorySummary> summaries)
    {
        return summaries.ToDictionary(
            pair => pair.Key,
            pair => new RootCategorySummaryResponse
            {
                TokenTally = pair.Value.TokenTally,
                ProbNotInCat = pair.Value.ProbNotInCat,
                ProbInCat = pair.Value.ProbInCat
            },
            StringComparer.OrdinalIgnoreCase);
    }

    private async Task<string> ReadBodyAsTextAsync()
    {
        Request.EnableBuffering();
        Request.Body.Position = 0;
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        Request.Body.Position = 0;
        return body;
    }
}
