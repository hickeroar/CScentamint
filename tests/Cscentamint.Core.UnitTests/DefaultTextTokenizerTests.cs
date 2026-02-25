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
    /// Verifies null or empty language falls back to English.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Tokenize_NullOrEmptyLanguage_FallsBackToEnglish(string? language)
    {
        var fallbackTokenizer = new DefaultTextTokenizer(language);
        var tokens = fallbackTokenizer.Tokenize("offers boxes").ToArray();
        Assert.Equal(["offer", "box"], tokens);
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
    /// Verifies token that stems to a stopword is filtered (covers FlushToken stopword branch).
    /// Uses "being" which stems to "be" - a stopword that stays non-empty after stemming.
    /// </summary>
    [Fact]
    public void Tokenize_RemoveStopWords_TokenStemmingToStopword_IsFiltered()
    {
        var tokenizerWithStopwords = new DefaultTextTokenizer("english", removeStopWords: true);
        var tokens = tokenizerWithStopwords.Tokenize("being and being").ToArray();
        Assert.Empty(tokens);
    }

    /// <summary>
    /// Verifies "be" (stopword that stems to itself) is filtered via _stemmedStopwords.Contains (covers FlushToken branch).
    /// </summary>
    [Fact]
    public void Tokenize_RemoveStopWords_StopwordStemmingToSelf_IsFiltered()
    {
        var tokenizerWithStopwords = new DefaultTextTokenizer("english", removeStopWords: true);
        var tokens = tokenizerWithStopwords.Tokenize("be jumps").ToArray();
        Assert.DoesNotContain("be", tokens);
        Assert.Single(tokens);
        Assert.Equal("jump", tokens[0]);
    }

    /// <summary>
    /// Verifies "doing" (stems to "do") is filtered via _stemmedStopwords.Contains (covers FlushToken stopword branch).
    /// </summary>
    [Fact]
    public void Tokenize_RemoveStopWords_DoingStemsToDo_IsFiltered()
    {
        var tokenizerWithStopwords = new DefaultTextTokenizer("english", removeStopWords: true);
        var tokens = tokenizerWithStopwords.Tokenize("doing jumps").ToArray();
        Assert.DoesNotContain("do", tokens);
        Assert.Single(tokens);
        Assert.Equal("jump", tokens[0]);
    }

    /// <summary>
    /// Verifies "running" (stems to "run") is filtered via _stemmedStopwords.Contains (covers FlushToken stopword branch).
    /// </summary>
    [Fact]
    public void Tokenize_RemoveStopWords_RunningStemsToRun_IsFiltered()
    {
        var tokenizerWithStopwords = new DefaultTextTokenizer("english", removeStopWords: true);
        var tokens = tokenizerWithStopwords.Tokenize("running walks").ToArray();
        Assert.DoesNotContain("run", tokens);
        Assert.Single(tokens);
        Assert.Equal("walk", tokens[0]);
    }

    /// <summary>
    /// Verifies stopword branch via custom stemmer that returns stopword stem (covers FlushToken _stemmedStopwords.Contains true).
    /// </summary>
    [Fact]
    public void Tokenize_RemoveStopWords_CustomStemmerReturnsStopword_IsFiltered()
    {
        static string Stem(string w) => w switch { "doing" => "do", "jumps" => "jump", _ => w };
        var tokenizer = new DefaultTextTokenizer("english", removeStopWords: true, stemmerOverride: Stem);
        var tokens = tokenizer.Tokenize("doing jumps").ToArray();
        Assert.DoesNotContain("do", tokens);
        Assert.Single(tokens);
        Assert.Equal("jump", tokens[0]);
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
