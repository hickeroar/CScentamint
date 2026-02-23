# CScentamint

`CScentamint` is a trainable naive Bayesian text classification library with optional persistence for .NET.

## Source code

[CScentamint repository on GitHub](https://github.com/hickeroar/cscentamint)

## Install

```bash
dotnet add package CScentamint
```

## Quick Start

```csharp
using Cscentamint.Core;

ITextClassifier classifier = new InMemoryNaiveBayesClassifier();

classifier.Train("spam", "buy now limited offer click here");
classifier.Train("ham", "team meeting schedule for tomorrow");

var prediction = classifier.Classify("limited offer today");
Console.WriteLine($"category={prediction.PredictedCategory ?? "<none>"} score={prediction.Score}");

var scores = classifier.GetScores("team update today");
foreach (var score in scores)
{
    Console.WriteLine($"{score.Key}: {score.Value}");
}
```

## Core functionality

- `Train(category, text)`: learns token counts for a category.
- `Untrain(category, text)`: removes learned influence for a text sample.
- `Classify(text)`: returns best category and score as `ClassificationPrediction`.
- `GetScores(text)`: returns per-category score map.
- `GetSummaries()`: returns per-category `CategorySummary` data.
- `Reset()`: clears all in-memory learned state.
- `Save(Stream)` / `Load(Stream)`: persistence to and from streams.
- `SaveToFile(path)` / `LoadFromFile(path)`: persistence using absolute file paths.

## Persistence

`CScentamint` includes built-in JSON model persistence with schema versioning and validation.

Stream example:

```csharp
using var buffer = new MemoryStream();
classifier.Save(buffer);

buffer.Position = 0;
ITextClassifier restored = new InMemoryNaiveBayesClassifier();
restored.Load(buffer);
```

File example:

```csharp
classifier.SaveToFile("/tmp/cscentamint-model.bin");

ITextClassifier restored = new InMemoryNaiveBayesClassifier();
restored.LoadFromFile("/tmp/cscentamint-model.bin");
```

When using the default tokenizer, language and `removeStopWords` are persisted in the model and restored on `Load`. Legacy models without tokenizer config load successfully; the classifier’s tokenizer is unchanged when no tokenizer section exists.

File persistence behavior:

- Uses atomic replace strategy when saving (`SaveToFile`) to avoid partial writes.
- Uses `/tmp/cscentamint-model.bin` when path is null/whitespace.
- Requires absolute file paths when a path is provided.

## Tokenization and customization

The default tokenizer (`DefaultTextTokenizer`) pipeline:

- Unicode normalization (`NFKC`)
- lowercase normalization
- split on non-letter/non-digit boundaries
- language-specific stemming (default English)
- optional stopword filtering

Constructor: `DefaultTextTokenizer(string? language = "english", bool removeStopWords = false)`.

Supported languages for stemming and stopwords: arabic, armenian, basque, catalan, danish, dutch, english, finnish, french, german, greek, hindi, hungarian, indonesian, irish, italian, lithuanian, nepali, norwegian, porter, portuguese, romanian, russian, serbian, spanish, swedish, tamil, turkish, yiddish. Unknown languages fall back to English.

Stopwords API (`Cscentamint.Core.Stopwords`):

- `Stopwords.Get(lang)`: returns `IReadOnlySet<string>?` for the language, or null if unsupported
- `Stopwords.Supported(lang)`: returns true if the language has a stopword list
- `Stopwords.SupportedLanguages`: returns the list of supported languages

You can provide a custom tokenizer by implementing `ITextTokenizer`:

```csharp
public sealed class MyTokenizer : ITextTokenizer
{
    public IEnumerable<string> Tokenize(string text) => text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
}

ITextClassifier classifier = new InMemoryNaiveBayesClassifier(new MyTokenizer());
```

Or use the default tokenizer with options:

```csharp
ITextClassifier classifier = new InMemoryNaiveBayesClassifier("spanish", removeStopWords: true);
```

## Rules and behavior

- `Classify()` returns `PredictedCategory = null` and `Score = 0` when no category can be predicted.
- Scores are relative ranking values and are not calibrated probabilities.
- Category names accepted by `Train` and `Untrain` must match `^[a-zA-Z0-9_-]{1,64}$`.
- Category matching is case-insensitive.
- Input `text` null values are treated as empty text.
- `Load` validates model version, category names, token names, and tally/count invariants.
