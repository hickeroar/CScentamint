using Cscentamint.Core;
using Microsoft.AspNetCore.Mvc;

namespace Cscentamint.Api.Controllers;

[ApiController]
[Route("api/model")]
public sealed class ModelController(ITextClassifier classifier) : ControllerBase
{
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Reset()
    {
        classifier.Reset();
        return NoContent();
    }
}
