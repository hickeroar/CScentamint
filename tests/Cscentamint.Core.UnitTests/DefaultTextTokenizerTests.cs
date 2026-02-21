using Xunit;

namespace Cscentamint.Core.UnitTests;

/// <summary>
/// Unit tests for default tokenizer behavior.
/// </summary>
public sealed class DefaultTextTokenizerTests
{
    private readonly DefaultTextTokenizer tokenizer = new();

    /// <summary>
    /// Verifies tokenizer lowercases and splits on non-letter/non-digit boundaries.
    /// </summary>
    [Fact]
    public void Tokenize_SplitsAndLowercases()
    {
        var tokens = tokenizer.Tokenize("Hello, WORLD! 123").ToArray();

        Assert.Equal(["hello", "world", "123"], tokens);
    }

    /// <summary>
    /// Verifies tokenizer applies NFKC normalization to text.
    /// </summary>
    [Fact]
    public void Tokenize_AppliesNfkcNormalization()
    {
        var tokens = tokenizer.Tokenize("Ｆｕｌｌｗｉｄｔｈ").ToArray();

        Assert.Equal(["fullwidth"], tokens);
    }

    /// <summary>
    /// Verifies tokenizer performs basic stemming for common English suffixes.
    /// </summary>
    [Fact]
    public void Tokenize_AppliesStemmingRules()
    {
        var tokens = tokenizer.Tokenize("offers offered offering categories boxes").ToArray();

        Assert.Equal(["offer", "offer", "offer", "categori", "box"], tokens);
    }
}
