using Cscentamint.Api.Contracts;
using Cscentamint.Core;
using Microsoft.AspNetCore.Mvc;

namespace Cscentamint.Api.Controllers;

/// <summary>
/// Predicts the best category match for an input document.
/// </summary>
/// <param name="classifier">Classifier used to predict categories.</param>
[ApiController]
[Route("api/classifications")]
public sealed class ClassificationsController(ITextClassifier classifier) : ControllerBase
{
    /// <summary>
    /// Classifies the provided text and returns a category prediction.
    /// </summary>
    /// <param name="request">Request containing text to classify.</param>
    /// <returns>Prediction response with the best matching category.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ClassificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<ClassificationResponse> Create([FromBody] TextDocumentRequest request)
    {
        var prediction = classifier.Classify(request.Text);
        return Ok(new ClassificationResponse(prediction.PredictedCategory));
    }
}
