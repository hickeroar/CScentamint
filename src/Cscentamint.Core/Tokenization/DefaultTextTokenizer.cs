using System.Globalization;
using System.Text;
using Snowball;

namespace Cscentamint.Core;

/// <summary>
/// Default tokenizer pipeline for Cscentamint.
/// </summary>
public sealed class DefaultTextTokenizer : ITextTokenizer
{
    private static readonly ThreadLocal<EnglishStemmer> EnglishStemmerInstance = new(() => new EnglishStemmer());

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

        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                current.Append(character);
                continue;
            }

            FlushToken(tokens, current);
        }

        FlushToken(tokens, current);
        return tokens;
    }

    private static void FlushToken(ICollection<string> destination, StringBuilder current)
    {
        if (current.Length == 0)
        {
            return;
        }

        var token = current.ToString();
        current.Clear();

        token = StemEnglishToken(token);
        if (!string.IsNullOrWhiteSpace(token))
        {
            destination.Add(token);
        }
    }

    private static string StemEnglishToken(string token)
    {
        return EnglishStemmerInstance.Value!.Stem(token);
    }
}
