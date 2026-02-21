using System.Text.RegularExpressions;

namespace Cscentamint.Core;

/// <summary>
/// In-memory naive Bayes classifier implementation for short text inputs.
/// </summary>
public sealed class InMemoryNaiveBayesClassifier : ITextClassifier
{
    private static readonly Regex CategoryPattern = new("^[a-zA-Z0-9_-]{1,64}$", RegexOptions.Compiled);
    private readonly ReaderWriterLockSlim _stateLock = new();
    private readonly Dictionary<string, Dictionary<string, int>> _tokenCountsByCategory =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CategoryPriors> _priorsByCategory =
        new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public void Train(string category, string text)
    {
        var normalizedCategory = NormalizeCategory(category);
        var normalizedText = NormalizeText(text);

        _stateLock.EnterWriteLock();
        try
        {
            if (!_tokenCountsByCategory.ContainsKey(normalizedCategory))
            {
                _tokenCountsByCategory[normalizedCategory] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            }

            foreach (var token in Tokenize(normalizedText))
            {
                if (!_tokenCountsByCategory[normalizedCategory].ContainsKey(token))
                {
                    _tokenCountsByCategory[normalizedCategory][token] = 0;
                }

                _tokenCountsByCategory[normalizedCategory][token]++;
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
            if (!_tokenCountsByCategory.ContainsKey(normalizedCategory))
            {
                return;
            }

            foreach (var token in Tokenize(normalizedText))
            {
                if (!_tokenCountsByCategory[normalizedCategory].ContainsKey(token))
                {
                    continue;
                }

                if (_tokenCountsByCategory[normalizedCategory][token] == 0)
                {
                    continue;
                }

                _tokenCountsByCategory[normalizedCategory][token]--;
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
            _tokenCountsByCategory.Clear();
            _priorsByCategory.Clear();
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

            var highestScore = scores.MaxBy(entry => entry.Value);
            return new ClassificationPrediction(highestScore.Key);
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    private IReadOnlyDictionary<string, float> ScoreUnsafe(string text)
    {
        var tokenOccurrences = CountTokenOccurrences(Tokenize(text));
        var workingScores = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        foreach (var category in _tokenCountsByCategory.Keys)
        {
            workingScores[category] = 0f;
        }

        foreach (var token in tokenOccurrences)
        {
            var tokenScoresByCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var tokenText = token.Key;
            var tokenCount = token.Value;

            foreach (var category in _tokenCountsByCategory)
            {
                tokenScoresByCategory[category.Key] =
                    category.Value.TryGetValue(tokenText, out var categoryCount) ? categoryCount : 0;
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
        var totalDistinctTokenCount = 0;
        var distinctTokenCountByCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var category in _tokenCountsByCategory)
        {
            totalDistinctTokenCount += category.Value.Count;
            distinctTokenCountByCategory[category.Key] = category.Value.Count;
        }

        var priorByCategory = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in distinctTokenCountByCategory)
        {
            priorByCategory[category.Key] = totalDistinctTokenCount > 0
                ? (float)category.Value / totalDistinctTokenCount
                : 0f;
        }

        var priorSum = priorByCategory.Sum(entry => entry.Value);
        _priorsByCategory.Clear();

        foreach (var category in priorByCategory)
        {
            _priorsByCategory[category.Key] = new CategoryPriors(
                PriorCategory: category.Value,
                PriorNonCategory: priorSum - category.Value);
        }
    }

    private float CalculateBayesianProbability(string category, int tokenScore, int totalTokenCount)
    {
        if (!_priorsByCategory.TryGetValue(category, out var categoryPriors))
        {
            return 0f;
        }

        var probabilityTokenGivenNonCategory = (float)(totalTokenCount - tokenScore) / totalTokenCount;
        var probabilityTokenGivenCategory = (float)tokenScore / totalTokenCount;

        var numerator = probabilityTokenGivenCategory * categoryPriors.PriorCategory;
        var denominator = numerator + (probabilityTokenGivenNonCategory + categoryPriors.PriorNonCategory);
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

    private static IEnumerable<string> Tokenize(string text)
    {
        return text
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length > 2);
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

    private readonly record struct CategoryPriors(float PriorCategory, float PriorNonCategory);
}
