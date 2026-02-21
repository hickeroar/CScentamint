using System.Globalization;
using System.Text;

namespace Cscentamint.Core;

/// <summary>
/// Default tokenizer pipeline inspired by gobayes behavior.
/// </summary>
public sealed class DefaultTextTokenizer : ITextTokenizer
{
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
        if (token.Length <= 2)
        {
            return token;
        }

        if (token.EndsWith("ies", StringComparison.Ordinal) && token.Length > 4)
        {
            return token[..^3] + "y";
        }

        if (token.EndsWith("ing", StringComparison.Ordinal) && token.Length > 5)
        {
            return token[..^3];
        }

        if (token.EndsWith("ed", StringComparison.Ordinal) && token.Length > 4)
        {
            return token[..^2];
        }

        if (token.EndsWith("es", StringComparison.Ordinal) && token.Length > 4)
        {
            return token[..^2];
        }

        if (token.EndsWith("s", StringComparison.Ordinal) && token.Length > 3)
        {
            return token[..^1];
        }

        return token;
    }
}
