using System.ComponentModel.DataAnnotations;

namespace Cscentamint.Api.Contracts;

/// <summary>
/// Request contract for endpoints that accept plain text input.
/// </summary>
public sealed class TextDocumentRequest
{
    /// <summary>
    /// Gets the text payload to classify, score, or train with.
    /// </summary>
    [Required]
    [MinLength(1)]
    [MaxLength(4000)]
    public string Text { get; init; } = string.Empty;
}
