using FsCheck.Xunit;
using Xunit;

namespace Cscentamint.Core.UnitTests;

/// <summary>
/// Property-based tests for classifier invariants.
/// </summary>
public sealed class ClassifierPropertyTests
{
    /// <summary>
    /// Verifies training and untraining the same sample does not leave positive scores.
    /// </summary>
    [Property(MaxTest = 100)]
    public void TrainThenUntrainSameSample_LeavesNoPositiveScores(string? sample)
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        var text = sample ?? string.Empty;

        classifier.Train("prop", text);
        classifier.Untrain("prop", text);

        Assert.Empty(classifier.GetScores(text));
    }

    /// <summary>
    /// Verifies scoring never returns negative values.
    /// </summary>
    [Property(MaxTest = 100)]
    public void Scores_AreAlwaysNonNegative(string? left, string? right, string? input)
    {
        var classifier = new InMemoryNaiveBayesClassifier();

        classifier.Train("left", $"{left ?? string.Empty} fallback");
        classifier.Train("right", $"{right ?? string.Empty} fallback");

        var scores = classifier.GetScores(input ?? string.Empty);
        Assert.All(scores.Values, value => Assert.True(value >= 0f));
    }

    /// <summary>
    /// Verifies classify does not throw for arbitrary input values.
    /// </summary>
    [Property(MaxTest = 100)]
    public void Classify_DoesNotThrow_ForArbitraryInput(string? sample, string? input)
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        classifier.Train("safe", sample ?? string.Empty);

        var exception = Record.Exception(() => classifier.Classify(input ?? string.Empty));
        Assert.Null(exception);
    }
}
