using System.ComponentModel.DataAnnotations;
using Cscentamint.Api.Contracts;
using Cscentamint.Core;
using Microsoft.AspNetCore.Mvc;

namespace Cscentamint.Api.Controllers;

/// <summary>
/// Manages category training samples for the classifier model.
/// </summary>
/// <param name="classifier">Classifier used to train and untrain samples.</param>
[ApiController]
[Route("api/categories/{category}/samples")]
public sealed class CategoriesController(ITextClassifier classifier) : ControllerBase
{
    /// <summary>
    /// Adds a training sample to the specified category.
    /// </summary>
    /// <param name="category">Target category for the sample.</param>
    /// <param name="request">Request containing sample text.</param>
    /// <returns>No content when training succeeds.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult AddSample(
        [FromRoute, Required, StringLength(64, MinimumLength = 1), RegularExpression("^[a-zA-Z0-9_-]+$")] string category,
        [FromBody] TextDocumentRequest request)
    {
        classifier.Train(category, request.Text);
        return NoContent();
    }

    /// <summary>
    /// Removes influence of a training sample from the specified category.
    /// </summary>
    /// <param name="category">Target category for sample removal.</param>
    /// <param name="request">Request containing sample text.</param>
    /// <returns>No content when untraining succeeds.</returns>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult RemoveSample(
        [FromRoute, Required, StringLength(64, MinimumLength = 1), RegularExpression("^[a-zA-Z0-9_-]+$")] string category,
        [FromBody] TextDocumentRequest request)
    {
        classifier.Untrain(category, request.Text);
        return NoContent();
    }
}
