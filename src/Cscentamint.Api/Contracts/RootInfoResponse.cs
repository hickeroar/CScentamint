namespace Cscentamint.Api.Contracts;

/// <summary>
/// Model info response for root text endpoints.
/// </summary>
public sealed record RootInfoResponse
{
    /// <summary>
    /// Gets category summaries keyed by category name.
    /// </summary>
    public required IReadOnlyDictionary<string, RootCategorySummaryResponse> Categories { get; init; }
}
