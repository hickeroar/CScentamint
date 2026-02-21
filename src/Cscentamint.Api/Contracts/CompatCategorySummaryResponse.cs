namespace Cscentamint.Api.Contracts;

/// <summary>
/// Gobayes-compatible category summary contract.
/// </summary>
public sealed record CompatCategorySummaryResponse
{
    /// <summary>
    /// Gets the total token tally in the category.
    /// </summary>
    public int TokenTally { get; init; }

    /// <summary>
    /// Gets the probability a token is not in this category.
    /// </summary>
    public float ProbNotInCat { get; init; }

    /// <summary>
    /// Gets the probability a token is in this category.
    /// </summary>
    public float ProbInCat { get; init; }
}
