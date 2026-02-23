using System.Globalization;
using System.Text;

namespace Cscentamint.Core;

/// <summary>
/// Default tokenizer pipeline: NFKC normalization, lowercasing, non-alphanumeric split,
/// language-specific stemming, and optional stopword filtering.
/// </summary>
public sealed class DefaultTextTokenizer : ITextTokenizer
{
    private readonly string _language;
    private readonly bool _removeStopWords;
    private readonly ThreadLocal<Func<string, string>> _stemmer;
    private readonly IReadOnlySet<string>? _stemmedStopwords;

    /// <summary>
    /// Creates a tokenizer with the given language and stopword settings.
    /// </summary>
    /// <param name="language">Language code for stemming and stopwords (e.g. "english", "spanish"). Default "english".</param>
    /// <param name="removeStopWords">When true, filter out stop words. Default false.</param>
    public DefaultTextTokenizer(string? language = "english", bool removeStopWords = false)
    {
        _language = StemmerFactory.ResolveLanguage(language);
        _removeStopWords = removeStopWords;
        _stemmer = StemmerFactory.CreateThreadLocalStemmer(_language);

        if (_removeStopWords && Stopwords.Get(_language) is { } rawStopwords)
        {
            var stemmer = _stemmer.Value!;
            _stemmedStopwords = rawStopwords
                .Select(w => stemmer(w.ToLowerInvariant()))
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            _stemmedStopwords = null;
        }
    }

    /// <summary>
    /// Language used for stemming and stopwords.
    /// </summary>
    public string Language => _language;

    /// <summary>
    /// Whether stop words are filtered out.
    /// </summary>
    public bool RemoveStopWords => _removeStopWords;

    /// <inheritdoc />
    public IEnumerable<string> Tokenize(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var normalized = text.Normalize(NormalizationForm.FormKC).ToLower(CultureInfo.InvariantCulture);
        var tokens = new List<string>();
        var current = new StringBuilder();
        var stemmer = _stemmer.Value!;

        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                current.Append(character);
                continue;
            }

            FlushToken(tokens, current, stemmer);
        }

        FlushToken(tokens, current, stemmer);
        return tokens;
    }

    private void FlushToken(ICollection<string> destination, StringBuilder current, Func<string, string> stemmer)
    {
        if (current.Length == 0)
        {
            return;
        }

        var token = current.ToString();
        current.Clear();

        token = stemmer(token);
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        if (_stemmedStopwords is not null && _stemmedStopwords.Contains(token))
        {
            return;
        }

        destination.Add(token);
    }
}
