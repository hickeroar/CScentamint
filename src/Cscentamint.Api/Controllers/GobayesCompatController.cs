using System.Text;
using Cscentamint.Api.Contracts;
using Cscentamint.Core;
using Microsoft.AspNetCore.Mvc;

namespace Cscentamint.Api.Controllers;

/// <summary>
/// Provides gobayes-compatible endpoints while preserving existing API routes.
/// </summary>
/// <param name="classifier">Classifier service.</param>
[ApiController]
[Route("")]
public sealed class GobayesCompatController(ITextClassifier classifier) : ControllerBase
{
    /// <summary>
    /// Returns gobayes-compatible category summaries.
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(typeof(CompatInfoResponse), StatusCodes.Status200OK)]
    public ActionResult<CompatInfoResponse> Info()
    {
        return Ok(new CompatInfoResponse
        {
            Categories = MapSummaries(classifier.GetSummaries())
        });
    }

    /// <summary>
    /// Trains a category from raw request body text.
    /// </summary>
    [HttpPost("train/{category:regex(^[[-_A-Za-z0-9]]+$)}")]
    [ProducesResponseType(typeof(CompatMutationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompatMutationResponse>> Train([FromRoute] string category)
    {
        var text = await ReadBodyAsTextAsync();
        classifier.Train(category, text);
        return Ok(CreateSuccessResponse());
    }

    /// <summary>
    /// Untrains a category from raw request body text.
    /// </summary>
    [HttpPost("untrain/{category:regex(^[[-_A-Za-z0-9]]+$)}")]
    [ProducesResponseType(typeof(CompatMutationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompatMutationResponse>> Untrain([FromRoute] string category)
    {
        var text = await ReadBodyAsTextAsync();
        classifier.Untrain(category, text);
        return Ok(CreateSuccessResponse());
    }

    /// <summary>
    /// Classifies raw request body text.
    /// </summary>
    [HttpPost("classify")]
    [ProducesResponseType(typeof(CompatClassificationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompatClassificationResponse>> Classify()
    {
        var text = await ReadBodyAsTextAsync();
        var prediction = classifier.Classify(text);
        return Ok(new CompatClassificationResponse
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
    [ProducesResponseType(typeof(CompatMutationResponse), StatusCodes.Status200OK)]
    public ActionResult<CompatMutationResponse> Flush()
    {
        classifier.Reset();
        return Ok(CreateSuccessResponse());
    }

    private CompatMutationResponse CreateSuccessResponse()
    {
        return new CompatMutationResponse
        {
            Success = true,
            Categories = MapSummaries(classifier.GetSummaries())
        };
    }

    private static IReadOnlyDictionary<string, CompatCategorySummaryResponse> MapSummaries(
        IReadOnlyDictionary<string, CategorySummary> summaries)
    {
        return summaries.ToDictionary(
            pair => pair.Key,
            pair => new CompatCategorySummaryResponse
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
