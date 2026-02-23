using Cscentamint.Api.Contracts;
using Cscentamint.Core;
using Microsoft.AspNetCore.Mvc;

namespace Cscentamint.Api.Controllers;

/// <summary>
/// Returns per-category score results for an input document.
/// </summary>
/// <param name="classifier">Classifier used to score text.</param>
[ApiController]
[Route("api/scores")]
public sealed class ScoresController(ITextClassifier classifier) : ControllerBase
{
    /// <summary>
    /// Calculates category scores for the provided text.
    /// </summary>
    /// <param name="request">Request containing text to score.</param>
    /// <returns>Dictionary of category scores.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(IReadOnlyDictionary<string, float>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyDictionary<string, float>> Create([FromBody] TextDocumentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Ok(classifier.GetScores(request.Text));
    }
}
