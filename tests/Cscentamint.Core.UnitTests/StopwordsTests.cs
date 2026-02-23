using Xunit;

namespace Cscentamint.Core.UnitTests;

/// <summary>
/// Unit tests for <see cref="Stopwords" /> API.
/// </summary>
public sealed class StopwordsTests
{
    /// <summary>
    /// Verifies Get returns a non-null set for supported languages.
    /// </summary>
    [Fact]
    public void Get_SupportedLanguage_ReturnsSet()
    {
        var set = Stopwords.Get("english");
        Assert.NotNull(set);
        Assert.True(set.Count > 0);
        Assert.True(set.Contains("the"));
        Assert.True(set.Contains("and"));
    }

    /// <summary>
    /// Verifies Get returns null for unsupported language.
    /// </summary>
    [Fact]
    public void Get_UnsupportedLanguage_ReturnsNull()
    {
        Assert.Null(Stopwords.Get("klingon"));
        Assert.Null(Stopwords.Get("invalid"));
    }

    /// <summary>
    /// Verifies Get returns null for null or whitespace.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Get_NullOrWhitespace_ReturnsNull(string? lang)
    {
        Assert.Null(Stopwords.Get(lang!));
    }

    /// <summary>
    /// Verifies Get is case-insensitive.
    /// </summary>
    [Fact]
    public void Get_CaseInsensitive_ReturnsSameSet()
    {
        var lower = Stopwords.Get("english");
        var upper = Stopwords.Get("ENGLISH");
        var mixed = Stopwords.Get("English");

        Assert.NotNull(lower);
        Assert.NotNull(upper);
        Assert.NotNull(mixed);
        Assert.Same(lower, upper);
        Assert.Same(lower, mixed);
    }

    /// <summary>
    /// Verifies Supported returns true for supported languages.
    /// </summary>
    [Fact]
    public void Supported_English_ReturnsTrue()
    {
        Assert.True(Stopwords.Supported("english"));
        Assert.True(Stopwords.Supported("spanish"));
        Assert.True(Stopwords.Supported("french"));
    }

    /// <summary>
    /// Verifies Supported returns false for unsupported languages.
    /// </summary>
    [Fact]
    public void Supported_Unsupported_ReturnsFalse()
    {
        Assert.False(Stopwords.Supported("klingon"));
        Assert.False(Stopwords.Supported(""));
    }

    /// <summary>
    /// Verifies SupportedLanguages returns non-empty list.
    /// </summary>
    [Fact]
    public void SupportedLanguages_ReturnsNonEmptyList()
    {
        var list = Stopwords.SupportedLanguages;
        Assert.NotNull(list);
        Assert.NotEmpty(list);
        Assert.Contains("english", list);
    }
}
