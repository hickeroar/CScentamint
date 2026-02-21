using System.ComponentModel.DataAnnotations;
using Cscentamint.Api.Contracts;
using Cscentamint.Core;
using Microsoft.AspNetCore.Mvc;

namespace Cscentamint.Api.Controllers;

[ApiController]
[Route("api/categories/{category}/samples")]
public sealed class CategoriesController(ITextClassifier classifier) : ControllerBase
{
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
