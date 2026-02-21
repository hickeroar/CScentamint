namespace Cscentamint.Core;

public interface ITextClassifier
{
    void Train(string category, string text);
    void Untrain(string category, string text);
    void Reset();
    IReadOnlyDictionary<string, float> GetScores(string text);
    ClassificationPrediction Classify(string text);
}
