using System.Reflection;
using Xunit;

namespace Cscentamint.Core.UnitTests;

/// <summary>
/// Unit tests for <see cref="InMemoryNaiveBayesClassifier" /> behavior.
/// </summary>
public sealed class ClassifierTests
{
    /// <summary>
    /// Verifies classification returns <c>null</c> when no samples have been trained.
    /// </summary>
    [Fact]
    public void Classify_ReturnsNull_WhenNoTrainingData()
    {
        var classifier = new InMemoryNaiveBayesClassifier();

        var result = classifier.Classify("any text here");

        Assert.Null(result.PredictedCategory);
    }

    /// <summary>
    /// Verifies a trained category is predicted for matching input.
    /// </summary>
    [Fact]
    public void TrainAndClassify_ReturnsExpectedCategory()
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        classifier.Train("positive", "great awesome happy");
        classifier.Train("negative", "bad terrible sad");

        var result = classifier.Classify("awesome happy great");

        Assert.Equal("positive", result.PredictedCategory);
    }

    /// <summary>
    /// Verifies reset removes all learned scores and priors.
    /// </summary>
    [Fact]
    public void Reset_ClearsAllLearnedState()
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        classifier.Train("foo", "foo bar baz");
        Assert.NotEmpty(classifier.GetScores("foo"));

        classifier.Reset();

        Assert.Empty(classifier.GetScores("foo"));
    }

    /// <summary>
    /// Verifies concurrent train and score operations complete safely.
    /// </summary>
    [Fact]
    public async Task ParallelTrainAndScore_CompletesWithoutErrors()
    {
        var classifier = new InMemoryNaiveBayesClassifier();

        var tasks = Enumerable.Range(0, 25).Select(async i =>
        {
            await Task.Yield();
            classifier.Train("tech", $"dotnet csharp api sample {i}");
            var scores = classifier.GetScores("dotnet api");
            Assert.True(scores.Count >= 0);
        });

        await Task.WhenAll(tasks);
        var classification = classifier.Classify("dotnet csharp");
        Assert.Equal("tech", classification.PredictedCategory);
    }

    /// <summary>
    /// Verifies category guard clauses reject null/whitespace and invalid names.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    public void Train_ThrowsArgumentException_ForInvalidCategory(string? category)
    {
        var classifier = new InMemoryNaiveBayesClassifier();

        var exception = Assert.Throws<ArgumentException>(() => classifier.Train(category!, "valid text token"));
        Assert.Equal("category", exception.ParamName);
    }

    /// <summary>
    /// Verifies null input text is normalized and treated as empty without errors.
    /// </summary>
    [Fact]
    public void GetScores_AllowsNullText_AndReturnsEmptyScores()
    {
        var classifier = new InMemoryNaiveBayesClassifier();

        var scores = classifier.GetScores(null!);

        Assert.Empty(scores);
    }

    /// <summary>
    /// Verifies untrain exits early when the category does not exist.
    /// </summary>
    [Fact]
    public void Untrain_UnknownCategory_DoesNotThrow()
    {
        var classifier = new InMemoryNaiveBayesClassifier();

        var exception = Record.Exception(() => classifier.Untrain("missing", "any sample text"));

        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies untrain paths for missing token, decrement, and zero-count guard.
    /// </summary>
    [Fact]
    public void Untrain_HandlesTokenRemovalBranches()
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        classifier.Train("music", "guitar riff solo");

        classifier.Untrain("music", "unknown-token");
        classifier.Untrain("music", "guitar");
        classifier.Untrain("music", "guitar");

        var scores = classifier.GetScores("guitar riff");
        Assert.True(scores.TryGetValue("music", out var score));
        Assert.True(score > 0f);
    }

    /// <summary>
    /// Verifies priors can be recalculated when tokenization yields no learned tokens.
    /// </summary>
    [Fact]
    public void Train_WithOnlyShortTokens_ProducesNoScores()
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        classifier.Train("tiny", "a an to of");

        var scores = classifier.GetScores("a an to");

        Assert.Empty(scores);
    }

    /// <summary>
    /// Verifies private probability helper returns zero for unknown categories.
    /// </summary>
    [Fact]
    public void CalculateBayesianProbability_ReturnsZero_ForUnknownCategory()
    {
        var classifier = new InMemoryNaiveBayesClassifier();

        var result = InvokeBayesianProbability(classifier, "missing", 1, 1);

        Assert.Equal(0f, result);
    }

    /// <summary>
    /// Verifies denominator-zero guard in probability helper returns zero.
    /// </summary>
    [Fact]
    public void CalculateBayesianProbability_ReturnsZero_WhenDenominatorIsZero()
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        classifier.Train("empty", "a an to");

        var result = InvokeBayesianProbability(classifier, "empty", 1, 1);

        Assert.Equal(0f, result);
    }

    /// <summary>
    /// Verifies classifier breaks ties deterministically by lexical category order.
    /// </summary>
    [Fact]
    public void Classify_TieBreaksByLexicalCategoryName()
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        classifier.Train("zulu", "alpha beta");
        classifier.Train("apple", "alpha beta");

        var result = classifier.Classify("alpha beta");

        Assert.Equal("apple", result.PredictedCategory);
    }

    /// <summary>
    /// Verifies prior weighting is based on token tally volume, not distinct token count.
    /// </summary>
    [Fact]
    public void Classify_PrefersCategoryWithHigherTokenTally_WhenTokenEvidenceIsEqual()
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        classifier.Train("heavy", "topic topic topic topic topic");
        classifier.Train("light", "topic");

        var result = classifier.Classify("topic");

        Assert.Equal("heavy", result.PredictedCategory);
    }

    /// <summary>
    /// Verifies untraining full token influence deletes an emptied category.
    /// </summary>
    [Fact]
    public void Untrain_RemovesCategory_WhenTokenTallyReachesZero()
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        classifier.Train("music", "guitar riff solo");

        classifier.Untrain("music", "guitar riff solo");

        Assert.Empty(classifier.GetScores("guitar riff solo"));
        Assert.Null(classifier.Classify("guitar riff solo").PredictedCategory);
    }

    /// <summary>
    /// Verifies partial untraining decrements token count without removing category state.
    /// </summary>
    [Fact]
    public void Untrain_PartialTokenCount_DecrementsWithoutRemovingToken()
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        classifier.Train("music", "guitar guitar riff");

        classifier.Untrain("music", "guitar");

        var scores = classifier.GetScores("guitar");
        Assert.True(scores.TryGetValue("music", out var score));
        Assert.True(score > 0f);
    }

    private static float InvokeBayesianProbability(
        InMemoryNaiveBayesClassifier classifier,
        string category,
        int tokenScore,
        int totalTokenCount)
    {
        var method = typeof(InMemoryNaiveBayesClassifier).GetMethod(
            "CalculateBayesianProbability",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);
        var value = method!.Invoke(classifier, [category, tokenScore, totalTokenCount]);
        Assert.NotNull(value);
        return Assert.IsType<float>(value);
    }
}
