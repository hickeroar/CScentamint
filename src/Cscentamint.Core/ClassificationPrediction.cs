namespace Cscentamint.Core;

/// <summary>
/// Represents the predicted category output for a classification request.
/// </summary>
public sealed record ClassificationPrediction
{
    /// <summary>
    /// Initializes a new prediction result.
    /// </summary>
    /// <param name="predictedCategory">Predicted category, or <c>null</c> when no category can be predicted.</param>
    /// <param name="score">Prediction score associated with the selected category.</param>
    public ClassificationPrediction(string? predictedCategory, float score = 0f)
    {
        PredictedCategory = predictedCategory;
        Score = score;
    }

    /// <summary>
    /// Gets the predicted category, or <c>null</c> when no match is available.
    /// </summary>
    public string? PredictedCategory { get; init; }

    /// <summary>
    /// Gets the score for the predicted category.
    /// </summary>
    public float Score { get; init; }
}
