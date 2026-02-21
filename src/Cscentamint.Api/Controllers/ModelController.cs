using Cscentamint.Core;
using Microsoft.AspNetCore.Mvc;

namespace Cscentamint.Api.Controllers;

/// <summary>
/// Provides model-level maintenance operations.
/// </summary>
/// <param name="classifier">Classifier instance to reset.</param>
[ApiController]
[Route("api/model")]
public sealed class ModelController(ITextClassifier classifier) : ControllerBase
{
    /// <summary>
    /// Clears all in-memory model state.
    /// </summary>
    /// <returns>No content when reset succeeds.</returns>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Reset()
    {
        classifier.Reset();
        return NoContent();
    }
}
