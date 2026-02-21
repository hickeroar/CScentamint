using System.Text.Json;
using Xunit;

namespace Cscentamint.Core.UnitTests;

/// <summary>
/// Unit tests for classifier persistence behavior.
/// </summary>
public sealed class PersistenceTests
{
    private const string DefaultModelFilePath = "/tmp/cscentamint-model.bin";

    /// <summary>
    /// Verifies save and load round-trips classifier behavior.
    /// </summary>
    [Fact]
    public void SaveAndLoad_RoundTrip_PreservesClassification()
    {
        var source = new InMemoryNaiveBayesClassifier();
        source.Train("spam", "buy now offer");
        source.Train("ham", "meeting notes calendar");

        using var stream = new MemoryStream();
        source.Save(stream);

        stream.Position = 0;
        var loaded = new InMemoryNaiveBayesClassifier();
        loaded.Load(stream);

        var prediction = loaded.Classify("buy now");
        Assert.Equal("spam", prediction.PredictedCategory);
        Assert.True(prediction.Score > 0f);
    }

    /// <summary>
    /// Verifies save requires a writable stream.
    /// </summary>
    [Fact]
    public void Save_NonWritableStream_ThrowsArgumentException()
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        using var stream = new NonWritableMemoryStream();

        var ex = Assert.Throws<ArgumentException>(() => classifier.Save(stream));
        Assert.Equal("destination", ex.ParamName);
    }

    /// <summary>
    /// Verifies load requires a readable stream.
    /// </summary>
    [Fact]
    public void Load_NonReadableStream_ThrowsArgumentException()
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        using var stream = new NonReadableMemoryStream();

        var ex = Assert.Throws<ArgumentException>(() => classifier.Load(stream));
        Assert.Equal("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies invalid model versions are rejected.
    /// </summary>
    [Fact]
    public void Load_InvalidVersion_ThrowsInvalidDataException()
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        using var stream = CreateModelStream(new PersistedModelState
        {
            Version = 99,
            Categories = new Dictionary<string, PersistedCategoryState>()
        });

        var ex = Assert.Throws<InvalidDataException>(() => classifier.Load(stream));
        Assert.Contains("Unsupported model version", ex.Message);
    }

    /// <summary>
    /// Verifies invalid category names are rejected during load.
    /// </summary>
    [Fact]
    public void Load_InvalidCategoryName_ThrowsInvalidDataException()
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        using var stream = CreateModelStream(new PersistedModelState
        {
            Version = 1,
            Categories = new Dictionary<string, PersistedCategoryState>
            {
                ["!!!"] = new() { Tally = 1, Tokens = new Dictionary<string, int> { ["token"] = 1 } }
            }
        });

        var ex = Assert.Throws<InvalidDataException>(() => classifier.Load(stream));
        Assert.Contains("Invalid category name", ex.Message);
    }

    /// <summary>
    /// Verifies negative tallies are rejected.
    /// </summary>
    [Fact]
    public void Load_NegativeTally_ThrowsInvalidDataException()
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        using var stream = CreateModelStream(new PersistedModelState
        {
            Version = 1,
            Categories = new Dictionary<string, PersistedCategoryState>
            {
                ["spam"] = new() { Tally = -1, Tokens = new Dictionary<string, int>() }
            }
        });

        var ex = Assert.Throws<InvalidDataException>(() => classifier.Load(stream));
        Assert.Contains("Invalid tally", ex.Message);
    }

    /// <summary>
    /// Verifies empty token names are rejected.
    /// </summary>
    [Fact]
    public void Load_EmptyTokenName_ThrowsInvalidDataException()
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        using var stream = CreateModelStream(new PersistedModelState
        {
            Version = 1,
            Categories = new Dictionary<string, PersistedCategoryState>
            {
                ["spam"] = new() { Tally = 1, Tokens = new Dictionary<string, int> { [""] = 1 } }
            }
        });

        var ex = Assert.Throws<InvalidDataException>(() => classifier.Load(stream));
        Assert.Contains("Invalid token name", ex.Message);
    }

    /// <summary>
    /// Verifies non-positive token counts are rejected.
    /// </summary>
    [Fact]
    public void Load_InvalidTokenCount_ThrowsInvalidDataException()
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        using var stream = CreateModelStream(new PersistedModelState
        {
            Version = 1,
            Categories = new Dictionary<string, PersistedCategoryState>
            {
                ["spam"] = new() { Tally = 1, Tokens = new Dictionary<string, int> { ["token"] = 0 } }
            }
        });

        var ex = Assert.Throws<InvalidDataException>(() => classifier.Load(stream));
        Assert.Contains("Invalid token count", ex.Message);
    }

    /// <summary>
    /// Verifies token sums must match persisted tally.
    /// </summary>
    [Fact]
    public void Load_TallyMismatch_ThrowsInvalidDataException()
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        using var stream = CreateModelStream(new PersistedModelState
        {
            Version = 1,
            Categories = new Dictionary<string, PersistedCategoryState>
            {
                ["spam"] = new()
                {
                    Tally = 2,
                    Tokens = new Dictionary<string, int> { ["token"] = 1 }
                }
            }
        });

        var ex = Assert.Throws<InvalidDataException>(() => classifier.Load(stream));
        Assert.Contains("Invalid tally", ex.Message);
    }

    /// <summary>
    /// Verifies file save/load round-trip with an absolute path.
    /// </summary>
    [Fact]
    public void SaveToFileAndLoadFromFile_RoundTrip_Works()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cscentamint-{Guid.NewGuid():N}.bin");
        try
        {
            var source = new InMemoryNaiveBayesClassifier();
            source.Train("music", "guitar riff");
            source.SaveToFile(path);

            var loaded = new InMemoryNaiveBayesClassifier();
            loaded.LoadFromFile(path);

            Assert.Equal("music", loaded.Classify("guitar").PredictedCategory);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    /// <summary>
    /// Verifies default path branch can save and load state.
    /// </summary>
    [Fact]
    public void SaveToFile_NullPath_UsesDefaultPath()
    {
        try
        {
            var classifier = new InMemoryNaiveBayesClassifier();
            classifier.Train("news", "breaking update");
            classifier.SaveToFile();

            var loaded = new InMemoryNaiveBayesClassifier();
            loaded.LoadFromFile();

            Assert.Equal("news", loaded.Classify("breaking").PredictedCategory);
        }
        finally
        {
            if (File.Exists(DefaultModelFilePath))
            {
                File.Delete(DefaultModelFilePath);
            }
        }
    }

    /// <summary>
    /// Verifies file persistence requires absolute paths.
    /// </summary>
    [Fact]
    public void SaveOrLoad_NonAbsolutePath_ThrowsArgumentException()
    {
        var classifier = new InMemoryNaiveBayesClassifier();

        Assert.Throws<ArgumentException>(() => classifier.SaveToFile("relative.bin"));
        Assert.Throws<ArgumentException>(() => classifier.LoadFromFile("relative.bin"));
    }

    /// <summary>
    /// Verifies deserializing null model payload is rejected.
    /// </summary>
    [Fact]
    public void Load_NullPayload_ThrowsInvalidDataException()
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("null"));

        var ex = Assert.Throws<InvalidDataException>(() => classifier.Load(stream));
        Assert.Contains("Unable to deserialize persisted model", ex.Message);
    }

    /// <summary>
    /// Verifies path resolution guards root-only save paths.
    /// </summary>
    [Fact]
    public void SaveToFile_RootPathWithoutDirectory_ThrowsInvalidOperationException()
    {
        var classifier = new InMemoryNaiveBayesClassifier();

        Assert.Throws<InvalidOperationException>(() => classifier.SaveToFile("/"));
    }

    /// <summary>
    /// Verifies temporary persistence files are cleaned up when file replacement fails.
    /// </summary>
    [Fact]
    public void SaveToFile_OnMoveFailure_DeletesTempFile()
    {
        var classifier = new InMemoryNaiveBayesClassifier();
        classifier.Train("spam", "buy now");

        var directory = Path.Combine(Path.GetTempPath(), $"cscentamint-dir-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            Assert.ThrowsAny<Exception>(() => classifier.SaveToFile(directory));
            Assert.Empty(Directory.GetFiles(directory, ".cscentamint-*.tmp"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static MemoryStream CreateModelStream(PersistedModelState state)
    {
        var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, state);
        stream.Position = 0;
        return stream;
    }

    private sealed class NonWritableMemoryStream : MemoryStream
    {
        public override bool CanWrite => false;
    }

    private sealed class NonReadableMemoryStream : MemoryStream
    {
        public override bool CanRead => false;
    }
}
