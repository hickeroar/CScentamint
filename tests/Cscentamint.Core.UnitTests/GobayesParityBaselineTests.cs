using Xunit;

namespace Cscentamint.Core.UnitTests;

/// <summary>
/// Baseline parity tests for behavior shared with gobayes.
/// </summary>
public sealed class GobayesParityBaselineTests
{
    /// <summary>
    /// Ensures unknown tokens do not produce any category scores.
    /// </summary>
    [Fact]
    public void GetScores_IgnoresUnknownTokens()
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        classifier.Train("alpha", "known token one");

        var scores = classifier.GetScores("unseen content entirely");

        Assert.Empty(scores);
    }

    /// <summary>
    /// Ensures only positive scores are returned.
    /// </summary>
    [Fact]
    public void GetScores_ReturnsOnlyPositiveScores()
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        classifier.Train("alpha", "known token one");

        var scores = classifier.GetScores("known token");

        Assert.All(scores.Values, value => Assert.True(value > 0f));
    }

    /// <summary>
    /// Ensures empty model classification returns no category.
    /// </summary>
    [Fact]
    public void Classify_EmptyModel_ReturnsNullCategory()
    {
        var classifier = new InMemoryNaiveBayesClassifier();

        var result = classifier.Classify("text");

        Assert.Null(result.PredictedCategory);
    }

    /// <summary>
    /// Ensures category validation rejects values outside the allowed pattern.
    /// </summary>
    [Theory]
    [InlineData("space name")]
    [InlineData("slash/name")]
    [InlineData("name!")]
    public void Train_InvalidCategory_Throws(string category)
    {
        var classifier = new InMemoryNaiveBayesClassifier();

        Assert.Throws<ArgumentException>(() => classifier.Train(category, "sample"));
    }
}
