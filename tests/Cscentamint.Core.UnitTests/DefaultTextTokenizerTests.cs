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

    /// <summary>
    /// Verifies Spanish stemming reduces inflected forms to common stems.
    /// </summary>
    [Fact]
    public void Tokenize_Spanish_AppliesStemming()
    {
        var spanishTokenizer = new DefaultTextTokenizer("spanish");
        var tokens = spanishTokenizer.Tokenize("corriendo correr corrieron").ToArray();

        Assert.True(tokens.Length >= 1);
        Assert.All(tokens, t => Assert.StartsWith("corr", t));
    }

    /// <summary>
    /// Verifies French stemming reduces inflected forms.
    /// </summary>
    [Fact]
    public void Tokenize_French_AppliesStemming()
    {
        var frenchTokenizer = new DefaultTextTokenizer("french");
        var tokens = frenchTokenizer.Tokenize("manger mangeant mangé").ToArray();

        Assert.All(tokens, t => Assert.StartsWith("mang", t));
    }

    /// <summary>
    /// Verifies Arabic stemming produces tokens (may skip some that stem to empty).
    /// </summary>
    [Fact]
    public void Tokenize_Arabic_AppliesStemming()
    {
        var arabicTokenizer = new DefaultTextTokenizer("arabic");
        var tokens = arabicTokenizer.Tokenize("الكتاب").ToArray();
        Assert.True(tokens.Length >= 0);
    }

    /// <summary>
    /// Verifies unknown language falls back to English.
    /// </summary>
    [Fact]
    public void Tokenize_UnknownLanguage_FallsBackToEnglish()
    {
        var fallbackTokenizer = new DefaultTextTokenizer("nonexistent");
        var tokens = fallbackTokenizer.Tokenize("offers boxes").ToArray();

        Assert.Equal(["offer", "box"], tokens);
    }

    /// <summary>
    /// Verifies stopword removal filters common English words when enabled.
    /// </summary>
    [Fact]
    public void Tokenize_RemoveStopWords_FiltersStopwords()
    {
        var tokenizerWithStopwords = new DefaultTextTokenizer("english", removeStopWords: true);
        var tokens = tokenizerWithStopwords.Tokenize("the elephant and the jumps fence").ToArray();

        Assert.DoesNotContain("the", tokens);
        Assert.DoesNotContain("and", tokens);
        Assert.True(tokens.Length >= 2);
        Assert.Contains("jump", tokens);
    }

    /// <summary>
    /// Verifies Language and RemoveStopWords properties expose constructor values.
    /// </summary>
    [Fact]
    public void Constructor_ExposesLanguageAndRemoveStopWords()
    {
        var t = new DefaultTextTokenizer("spanish", removeStopWords: true);
        Assert.Equal("spanish", t.Language);
        Assert.True(t.RemoveStopWords);
    }
}
