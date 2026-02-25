using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Snowball;

namespace Cscentamint.Core;

/// <summary>
/// Creates Snowball stemmers by language for use in tokenization.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class StemmerFactory
{
    private static readonly Assembly SnowballAssembly = typeof(EnglishStemmer).Assembly;

    /// <summary>
    /// Supported stemmer languages. Matches libstemmer.net exactly.
    /// </summary>
    internal static readonly IReadOnlySet<string> SupportedLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "arabic", "armenian", "basque", "catalan", "danish", "dutch", "english",
        "finnish", "french", "german", "greek", "hindi", "hungarian", "indonesian",
        "irish", "italian", "lithuanian", "nepali", "norwegian", "porter",
        "portuguese", "romanian", "russian", "serbian", "spanish", "swedish",
        "tamil", "turkish", "yiddish",
    };

    /// <summary>
    /// Resolves the effective language for stemming. Normalizes and falls back to "english" if unsupported.
    /// </summary>
    internal static string ResolveLanguage(string? language)
    {
        var normalized = (language ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized) || !SupportedLanguages.Contains(normalized))
        {
            return "english";
        }

        return normalized;
    }

    /// <summary>
    /// Creates a thread-local stemmer for the given language (one instance per thread).
    /// </summary>
    internal static ThreadLocal<Func<string, string>> CreateThreadLocalStemmer(string language)
    {
        var lang = ResolveLanguage(language);
        var typeName = $"Snowball.{ToPascalCase(lang)}Stemmer";
        var stemmerType = ResolveStemmerType(typeName);

        return new ThreadLocal<Func<string, string>>(() =>
        {
            var stemmer = Activator.CreateInstance(stemmerType)
                ?? throw new InvalidOperationException($"Failed to create stemmer for {lang}.");
            var stemMethod = stemmerType.GetMethod("Stem", [typeof(string)])
                ?? throw new InvalidOperationException($"Stemmer type {stemmerType.Name} has no Stem(string) method.");

            return token =>
            {
                try
                {
                    var result = stemMethod.Invoke(stemmer, [token]) as string;
                    return !string.IsNullOrEmpty(result) ? result! : token;
                }
                catch
                {
                    return FallbackOnStemmerError(token);
                }
            };
        });
    }

    [ExcludeFromCodeCoverage]
    private static Type ResolveStemmerType(string typeName) =>
        SnowballAssembly.GetType(typeName) ?? typeof(EnglishStemmer);

    private static string ToPascalCase(string lang)
    {
        // ResolveLanguage guarantees non-empty; this is only called with its result.
        return char.ToUpperInvariant(lang[0]) + lang[1..];
    }

    [ExcludeFromCodeCoverage]
    private static string FallbackOnStemmerError(string token) => token;
}
