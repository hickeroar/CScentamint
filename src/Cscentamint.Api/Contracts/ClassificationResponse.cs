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
    /// <param name="score">Prediction score for the selected category.</param>
    public ClassificationResponse(string? category, float score = 0f)
    {
        Category = category;
        Score = score;
    }

    /// <summary>
    /// Gets the predicted category, or <c>null</c> when the model cannot determine one.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Gets the score associated with the predicted category.
    /// </summary>
    public float Score { get; init; }
}
