namespace Cscentamint.Api.Contracts;

/// <summary>
/// Mutation response for root text endpoints.
/// </summary>
public sealed record RootMutationResponse
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets category summaries keyed by category name.
    /// </summary>
    public required IReadOnlyDictionary<string, RootCategorySummaryResponse> Categories { get; init; }
}
