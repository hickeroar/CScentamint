using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cscentamint.Core;

/// <summary>
/// In-memory naive Bayesian classifier implementation for short text inputs.
/// </summary>
public sealed class InMemoryNaiveBayesClassifier : ITextClassifier
{
    private static readonly Regex CategoryPattern = new("^[a-zA-Z0-9_-]{1,64}$", RegexOptions.Compiled);
    private const int PersistedModelVersion = 1;
    private const string DefaultModelFilePath = "/tmp/cscentamint-model.bin";
    private readonly ReaderWriterLockSlim _stateLock = new();
    private readonly Dictionary<string, CategoryState> _categories = new(StringComparer.OrdinalIgnoreCase);
    private ITextTokenizer _tokenizer;

    /// <summary>
    /// Initializes a new in-memory classifier with a custom tokenizer. Tokenizer config is not persisted.
    /// </summary>
    /// <param name="tokenizer">Custom tokenizer.</param>
    public InMemoryNaiveBayesClassifier(ITextTokenizer tokenizer)
    {
        ArgumentNullException.ThrowIfNull(tokenizer);
        _tokenizer = tokenizer;
    }

    /// <summary>
    /// Initializes a new in-memory classifier with the default tokenizer.
    /// </summary>
    /// <param name="language">Language for the default tokenizer (e.g. "english", "spanish"). Default "english".</param>
    /// <param name="removeStopWords">Whether to filter stop words. Default false.</param>
    public InMemoryNaiveBayesClassifier(string? language = "english", bool removeStopWords = false)
    {
        _tokenizer = new DefaultTextTokenizer(language, removeStopWords);
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

            return new ClassificationPrediction(highestCategory, highestScore);
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, CategorySummary> GetSummaries()
    {
        _stateLock.EnterReadLock();
        try
        {
            return _categories.ToDictionary(
                pair => pair.Key,
                pair => new CategorySummary(
                    TokenTally: pair.Value.TokenTally,
                    ProbNotInCat: pair.Value.PriorNonCategory,
                    ProbInCat: pair.Value.PriorCategory),
                StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public void Save(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite)
        {
            throw new ArgumentException("Destination stream must be writable.", nameof(destination));
        }

        PersistedModelState snapshot;

        _stateLock.EnterReadLock();
        try
        {
            var categories = _categories.ToDictionary(
                pair => pair.Key,
                pair => new PersistedCategoryState
                {
                    Tally = pair.Value.TokenTally,
                    Tokens = pair.Value.TokenCounts.ToDictionary(
                        token => token.Key,
                        token => token.Value,
                        StringComparer.OrdinalIgnoreCase)
                },
                StringComparer.OrdinalIgnoreCase);

            PersistedTokenizerState? tokenizerState = null;
            if (_tokenizer is DefaultTextTokenizer dt)
            {
                tokenizerState = new PersistedTokenizerState
                {
                    Language = dt.Language,
                    RemoveStopWords = dt.RemoveStopWords
                };
            }

            snapshot = new PersistedModelState
            {
                Version = PersistedModelVersion,
                Categories = categories,
                Tokenizer = tokenizerState
            };
        }
        finally
        {
            _stateLock.ExitReadLock();
        }

        JsonSerializer.Serialize(destination, snapshot);
    }

    /// <inheritdoc />
    public void Load(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead)
        {
            throw new ArgumentException("Source stream must be readable.", nameof(source));
        }

        var model = JsonSerializer.Deserialize<PersistedModelState>(source) ??
            throw new InvalidDataException("Unable to deserialize persisted model.");
        if (model.Version != PersistedModelVersion)
        {
            throw new InvalidDataException($"Unsupported model version: {model.Version}.");
        }

        var nextCategories = new Dictionary<string, CategoryState>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in model.Categories)
        {
            if (!CategoryPattern.IsMatch(category.Key))
            {
                throw new InvalidDataException($"Invalid category name: {category.Key}.");
            }

            if (category.Value.Tally < 0)
            {
                throw new InvalidDataException($"Invalid tally for category {category.Key}: {category.Value.Tally}.");
            }

            var nextCategoryState = new CategoryState();
            var sum = 0;
            foreach (var token in category.Value.Tokens)
            {
                if (string.IsNullOrWhiteSpace(token.Key))
                {
                    throw new InvalidDataException($"Invalid token name for category {category.Key}.");
                }

                if (token.Value <= 0)
                {
                    throw new InvalidDataException($"Invalid token count for category {category.Key}: {token.Value}.");
                }

                nextCategoryState.TokenCounts[token.Key] = token.Value;
                sum += token.Value;
            }

            if (sum != category.Value.Tally)
            {
                throw new InvalidDataException(
                    $"Invalid tally for category {category.Key}: tally={category.Value.Tally}, sum={sum}.");
            }

            nextCategoryState.TokenTally = category.Value.Tally;
            nextCategories[category.Key] = nextCategoryState;
        }

        _stateLock.EnterWriteLock();
        try
        {
            _categories.Clear();
            foreach (var category in nextCategories)
            {
                _categories[category.Key] = category.Value;
            }

            if (model.Tokenizer is not null)
            {
                _tokenizer = new DefaultTextTokenizer(model.Tokenizer.Language, model.Tokenizer.RemoveStopWords);
            }

            RecalculateCategoryPriors();
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Saves model state to an absolute file path, using an atomic replace strategy.
    /// </summary>
    /// <param name="absolutePath">
    /// Absolute destination path. When null or whitespace, uses the default development fallback path.
    /// For production, prefer an explicit absolute path with restricted service-account permissions.
    /// </param>
    public void SaveToFile(string? absolutePath = null)
    {
        var resolvedPath = ResolveModelPath(absolutePath);
        var directory = Path.GetDirectoryName(resolvedPath) ??
            throw new InvalidOperationException("Unable to determine model directory.");
        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(directory, $".cscentamint-{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                Save(stream);
                stream.Flush(flushToDisk: true);
            }

            File.Move(tempPath, resolvedPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    /// <summary>
    /// Loads model state from an absolute file path.
    /// </summary>
    /// <param name="absolutePath">
    /// Absolute source path. When null or whitespace, uses the default development fallback path.
    /// For production, prefer an explicit absolute path with restricted service-account permissions.
    /// </param>
    public void LoadFromFile(string? absolutePath = null)
    {
        var resolvedPath = ResolveModelPath(absolutePath);
        using var stream = new FileStream(resolvedPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        Load(stream);
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

    private static string ResolveModelPath(string? absolutePath)
    {
        var resolvedPath = string.IsNullOrWhiteSpace(absolutePath) ? DefaultModelFilePath : absolutePath;
        if (!Path.IsPathRooted(resolvedPath))
        {
            throw new ArgumentException("Model file path must be absolute.", nameof(absolutePath));
        }

        return resolvedPath;
    }

    private sealed class CategoryState
    {
        public Dictionary<string, int> TokenCounts { get; } = new(StringComparer.OrdinalIgnoreCase);

        public int TokenTally { get; set; }

        public float PriorCategory { get; set; }

        public float PriorNonCategory { get; set; }
    }
}
