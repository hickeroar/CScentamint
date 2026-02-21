namespace Cscentamint.Core;

/// <summary>
/// Defines operations for training and using a text classifier.
/// </summary>
public interface ITextClassifier
{
    /// <summary>
    /// Trains the classifier with a sample text for a category.
    /// </summary>
    /// <param name="category">Category label associated with the training text.</param>
    /// <param name="text">Training text to learn from.</param>
    void Train(string category, string text);

    /// <summary>
    /// Removes influence of a sample text from a category.
    /// </summary>
    /// <param name="category">Category label associated with the training text.</param>
    /// <param name="text">Training text to remove.</param>
    void Untrain(string category, string text);

    /// <summary>
    /// Clears all learned in-memory state.
    /// </summary>
    void Reset();

    /// <summary>
    /// Calculates scores by category for the provided text.
    /// </summary>
    /// <param name="text">Text to score.</param>
    /// <returns>Category scores keyed by category name.</returns>
    IReadOnlyDictionary<string, float> GetScores(string text);

    /// <summary>
    /// Predicts the best matching category for the provided text.
    /// </summary>
    /// <param name="text">Text to classify.</param>
    /// <returns>Classification result containing the predicted category.</returns>
    ClassificationPrediction Classify(string text);

    /// <summary>
    /// Returns per-category summaries for compatibility and observability endpoints.
    /// </summary>
    /// <returns>Category summaries keyed by category name.</returns>
    IReadOnlyDictionary<string, CategorySummary> GetSummaries();

    /// <summary>
    /// Saves classifier state to the provided stream.
    /// </summary>
    /// <param name="destination">Writable destination stream.</param>
    void Save(Stream destination);

    /// <summary>
    /// Loads classifier state from the provided stream.
    /// </summary>
    /// <param name="source">Readable source stream.</param>
    void Load(Stream source);

    /// <summary>
    /// Saves classifier state to an absolute file path using atomic replacement.
    /// </summary>
    /// <param name="absolutePath">Absolute file path, or <c>null</c> for the default path.</param>
    void SaveToFile(string? absolutePath = null);

    /// <summary>
    /// Loads classifier state from an absolute file path.
    /// </summary>
    /// <param name="absolutePath">Absolute file path, or <c>null</c> for the default path.</param>
    void LoadFromFile(string? absolutePath = null);
}
