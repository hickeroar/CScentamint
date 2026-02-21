namespace Cscentamint.Api.Contracts;

/// <summary>
/// Gobayes-compatible model info response.
/// </summary>
public sealed record CompatInfoResponse
{
    /// <summary>
    /// Gets category summaries keyed by category name.
    /// </summary>
    public required IReadOnlyDictionary<string, CompatCategorySummaryResponse> Categories { get; init; }
}
