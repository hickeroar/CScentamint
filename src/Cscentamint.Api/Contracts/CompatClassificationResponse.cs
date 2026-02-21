namespace Cscentamint.Api.Contracts;

/// <summary>
/// Gobayes-compatible classify response contract.
/// </summary>
public sealed record CompatClassificationResponse
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
