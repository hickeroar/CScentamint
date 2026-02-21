namespace Cscentamint.Core;

/// <summary>
/// Tokenizes input text for classifier training and scoring.
/// </summary>
public interface ITextTokenizer
{
    /// <summary>
    /// Converts input text into a sequence of normalized tokens.
    /// </summary>
    /// <param name="text">Source text.</param>
    /// <returns>Normalized tokens.</returns>
    IEnumerable<string> Tokenize(string text);
}
