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
}
