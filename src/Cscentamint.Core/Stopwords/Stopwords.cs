using System.Collections.Frozen;
using Cscentamint.Core.Internal;

namespace Cscentamint.Core;

/// <summary>
/// Built-in stopword lists for languages supported by the stemmer.
/// Stopwords align exactly with libstemmer.net languages: no more, no less.
/// </summary>
public static class Stopwords
{
    private static readonly FrozenDictionary<string, FrozenSet<string>> Cache = BuildCache();

    /// <summary>
    /// Gets the stopword set for the given language.
    /// </summary>
    /// <param name="lang">Language code (e.g. "english", "spanish").</param>
    /// <returns>The stopword set, or null if the language is not supported.</returns>
    public static IReadOnlySet<string>? Get(string lang)
    {
        if (string.IsNullOrWhiteSpace(lang))
        {
            return null;
        }

        var key = lang.Trim().ToLowerInvariant();
        return Cache.TryGetValue(key, out var set) ? set : null;
    }

    /// <summary>
    /// Returns true if the language has a stopword list.
    /// </summary>
    /// <param name="lang">Language code.</param>
    /// <returns>True if supported; otherwise false.</returns>
    public static bool Supported(string lang) => Get(lang) is not null;

    /// <summary>
    /// Lists all languages that have stopword lists.
    /// </summary>
    public static IReadOnlyList<string> SupportedLanguages { get; } = Data.SupportedLanguageList;

    private static FrozenDictionary<string, FrozenSet<string>> BuildCache()
    {
        var builder = new Dictionary<string, FrozenSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (lang, words) in Data.All)
        {
            var set = words
                .Select(w => w.ToLowerInvariant())
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .ToFrozenSet(StringComparer.OrdinalIgnoreCase);
            builder[lang] = set;
        }

        return builder.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
}
