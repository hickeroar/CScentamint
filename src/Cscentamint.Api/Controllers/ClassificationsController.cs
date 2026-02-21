using Cscentamint.Api.Contracts;
using Cscentamint.Core;
using Microsoft.AspNetCore.Mvc;

namespace Cscentamint.Api.Controllers;

[ApiController]
[Route("api/classifications")]
public sealed class ClassificationsController(ITextClassifier classifier) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ClassificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<ClassificationResponse> Create([FromBody] TextDocumentRequest request)
    {
        var prediction = classifier.Classify(request.Text);
        return Ok(new ClassificationResponse(prediction.PredictedCategory));
    }
}
