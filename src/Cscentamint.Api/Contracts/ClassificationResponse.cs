namespace Cscentamint.Api.Contracts;

/// <summary>
/// Response contract for category prediction endpoints.
/// </summary>
public sealed record ClassificationResponse
{
    /// <summary>
    /// Initializes a new classification response.
    /// </summary>
    /// <param name="category">Predicted category, or <c>null</c> when no category is predicted.</param>
    public ClassificationResponse(string? category)
    {
        Category = category;
    }

    /// <summary>
    /// Gets the predicted category, or <c>null</c> when the model cannot determine one.
    /// </summary>
    public string? Category { get; init; }
}
