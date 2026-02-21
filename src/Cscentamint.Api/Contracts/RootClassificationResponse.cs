namespace Cscentamint.Api.Contracts;

/// <summary>
/// Classification response contract for root text endpoints.
/// </summary>
public sealed record RootClassificationResponse
{
    /// <summary>
    /// Gets the predicted category or empty string when no prediction is available.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Gets the prediction score for the selected category.
    /// </summary>
    public float Score { get; init; }
}
