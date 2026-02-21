namespace Cscentamint.Api.Contracts;

/// <summary>
/// Gobayes-compatible train/untrain/flush response.
/// </summary>
public sealed record CompatMutationResponse
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets category summaries keyed by category name.
    /// </summary>
    public required IReadOnlyDictionary<string, CompatCategorySummaryResponse> Categories { get; init; }
}
