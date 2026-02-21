namespace Cscentamint.Core;

/// <summary>
/// Snapshot summary of a trained category.
/// </summary>
/// <param name="TokenTally">Total trained token tally for the category.</param>
/// <param name="ProbNotInCat">Probability that a token is not in this category.</param>
/// <param name="ProbInCat">Probability that a token is in this category.</param>
public readonly record struct CategorySummary(
    int TokenTally,
    float ProbNotInCat,
    float ProbInCat);
