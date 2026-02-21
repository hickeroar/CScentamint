using Cscentamint.Api.Contracts;
using Cscentamint.Core;
using Microsoft.AspNetCore.Mvc;

namespace Cscentamint.Api.Controllers;

[ApiController]
[Route("api/scores")]
public sealed class ScoresController(ITextClassifier classifier) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(IReadOnlyDictionary<string, float>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyDictionary<string, float>> Create([FromBody] TextDocumentRequest request)
    {
        return Ok(classifier.GetScores(request.Text));
    }
}
