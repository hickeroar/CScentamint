using System.Text.RegularExpressions;

namespace Cscentamint.Core;

/// <summary>
/// In-memory naive Bayes classifier implementation for short text inputs.
/// </summary>
public sealed class InMemoryNaiveBayesClassifier : ITextClassifier
{
    private static readonly Regex CategoryPattern = new("^[a-zA-Z0-9_-]{1,64}$", RegexOptions.Compiled);
    private readonly ReaderWriterLockSlim _stateLock = new();
    private readonly Dictionary<string, CategoryState> _categories = new(StringComparer.OrdinalIgnoreCase);
    private readonly ITextTokenizer _tokenizer;

    /// <summary>
    /// Initializes a new in-memory classifier.
    /// </summary>
    /// <param name="tokenizer">Tokenizer used by train, untrain, score, and classify operations.</param>
    public InMemoryNaiveBayesClassifier(ITextTokenizer? tokenizer = null)
    {
        _tokenizer = tokenizer ?? new DefaultTextTokenizer();
    }

    /// <inheritdoc />
    public void Train(string category, string text)
    {
        var normalizedCategory = NormalizeCategory(category);
        var normalizedText = NormalizeText(text);

        _stateLock.EnterWriteLock();
        try
        {
            if (!_categories.TryGetValue(normalizedCategory, out var categoryState))
            {
                categoryState = new CategoryState();
                _categories[normalizedCategory] = categoryState;
            }

            foreach (var tokenOccurrence in CountTokenOccurrences(_tokenizer.Tokenize(normalizedText)))
            {
                categoryState.TokenCounts[tokenOccurrence.Key] =
                    categoryState.TokenCounts.GetValueOrDefault(tokenOccurrence.Key) + tokenOccurrence.Value;
                categoryState.TokenTally += tokenOccurrence.Value;
            }

            RecalculateCategoryPriors();
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public void Untrain(string category, string text)
    {
        var normalizedCategory = NormalizeCategory(category);
        var normalizedText = NormalizeText(text);

        _stateLock.EnterWriteLock();
        try
        {
            if (!_categories.TryGetValue(normalizedCategory, out var categoryState))
            {
                return;
            }

            foreach (var tokenOccurrence in CountTokenOccurrences(_tokenizer.Tokenize(normalizedText)))
            {
                if (!categoryState.TokenCounts.TryGetValue(tokenOccurrence.Key, out var currentTokenCount))
                {
                    continue;
                }

                if (tokenOccurrence.Value >= currentTokenCount)
                {
                    categoryState.TokenTally -= currentTokenCount;
                    categoryState.TokenCounts.Remove(tokenOccurrence.Key);
                }
                else
                {
                    categoryState.TokenCounts[tokenOccurrence.Key] = currentTokenCount - tokenOccurrence.Value;
                    categoryState.TokenTally -= tokenOccurrence.Value;
                }
            }

            if (categoryState.TokenTally == 0)
            {
                _categories.Remove(normalizedCategory);
            }

            RecalculateCategoryPriors();
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        _stateLock.EnterWriteLock();
        try
        {
            _categories.Clear();
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, float> GetScores(string text)
    {
        var normalizedText = NormalizeText(text);

        _stateLock.EnterReadLock();
        try
        {
            return ScoreUnsafe(normalizedText);
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public ClassificationPrediction Classify(string text)
    {
        var normalizedText = NormalizeText(text);

        _stateLock.EnterReadLock();
        try
        {
            var scores = ScoreUnsafe(normalizedText);
            if (scores.Count == 0)
            {
                return new ClassificationPrediction(null);
            }

            var highestCategory = string.Empty;
            var highestScore = 0f;

            foreach (var category in scores.Keys.OrderBy(name => name, StringComparer.Ordinal))
            {
                var score = scores[category];
                if (score > highestScore)
                {
                    highestScore = score;
                    highestCategory = category;
                }
            }

            return new ClassificationPrediction(highestCategory);
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    private IReadOnlyDictionary<string, float> ScoreUnsafe(string text)
    {
        var tokenOccurrences = CountTokenOccurrences(_tokenizer.Tokenize(text));
        var workingScores = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        foreach (var category in _categories.Keys)
        {
            workingScores[category] = 0f;
        }

        foreach (var token in tokenOccurrences)
        {
            var tokenScoresByCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var tokenText = token.Key;
            var tokenCount = token.Value;

            foreach (var category in _categories)
            {
                tokenScoresByCategory[category.Key] =
                    category.Value.TokenCounts.TryGetValue(tokenText, out var categoryCount) ? categoryCount : 0;
            }

            var totalTokenCount = tokenScoresByCategory.Sum(entry => entry.Value);
            if (totalTokenCount == 0)
            {
                continue;
            }

            foreach (var categoryScore in tokenScoresByCategory)
            {
                workingScores[categoryScore.Key] += tokenCount *
                    CalculateBayesianProbability(categoryScore.Key, categoryScore.Value, totalTokenCount);
            }
        }

        return workingScores
            .Where(entry => entry.Value > 0)
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
    }

    private void RecalculateCategoryPriors()
    {
        var totalTokenTally = _categories.Values.Sum(category => category.TokenTally);

        foreach (var category in _categories.Values)
        {
            category.PriorCategory = totalTokenTally > 0
                ? (float)category.TokenTally / totalTokenTally
                : 0f;
            category.PriorNonCategory = 1f - category.PriorCategory;
        }
    }

    private float CalculateBayesianProbability(string category, int tokenScore, int totalTokenCount)
    {
        if (!_categories.TryGetValue(category, out var categoryState))
        {
            return 0f;
        }

        var probabilityTokenGivenNonCategory = (float)(totalTokenCount - tokenScore) / totalTokenCount;
        var probabilityTokenGivenCategory = (float)tokenScore / totalTokenCount;

        var numerator = probabilityTokenGivenCategory * categoryState.PriorCategory;
        var denominator = (probabilityTokenGivenNonCategory * categoryState.PriorNonCategory) + numerator;
        return denominator == 0f ? 0f : numerator / denominator;
    }

    private static Dictionary<string, int> CountTokenOccurrences(IEnumerable<string> tokens)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var token in tokens)
        {
            if (!counts.ContainsKey(token))
            {
                counts[token] = 0;
            }

            counts[token]++;
        }

        return counts;
    }

    private static string NormalizeText(string? text)
    {
        return text ?? string.Empty;
    }

    private static string NormalizeCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category is required.", nameof(category));
        }

        var normalizedCategory = category.Trim();
        if (!CategoryPattern.IsMatch(normalizedCategory))
        {
            throw new ArgumentException(
                "Category must be 1-64 characters and contain only letters, numbers, underscore, or hyphen.",
                nameof(category));
        }

        return normalizedCategory;
    }

    private sealed class CategoryState
    {
        public Dictionary<string, int> TokenCounts { get; } = new(StringComparer.OrdinalIgnoreCase);

        public int TokenTally { get; set; }

        public float PriorCategory { get; set; }

        public float PriorNonCategory { get; set; }
    }
}
