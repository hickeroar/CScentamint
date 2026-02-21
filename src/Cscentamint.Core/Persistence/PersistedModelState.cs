namespace Cscentamint.Core;

/// <summary>
/// Serializable model persistence root.
/// </summary>
public sealed record PersistedModelState
{
    /// <summary>
    /// Gets or sets persisted model schema version.
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    /// Gets or sets serialized category data keyed by category name.
    /// </summary>
    public required Dictionary<string, PersistedCategoryState> Categories { get; init; }
}

/// <summary>
/// Serializable category persistence record.
/// </summary>
public sealed record PersistedCategoryState
{
    /// <summary>
    /// Gets or sets serialized token counts.
    /// </summary>
    public required Dictionary<string, int> Tokens { get; init; }

    /// <summary>
    /// Gets or sets total token tally for the category.
    /// </summary>
    public int Tally { get; init; }
}
