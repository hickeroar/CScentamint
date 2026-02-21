using System.ComponentModel.DataAnnotations;

namespace Cscentamint.Api.Contracts;

public sealed class TextDocumentRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(4000)]
    public string Text { get; init; } = string.Empty;
}
