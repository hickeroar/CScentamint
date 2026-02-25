using Cscentamint.Api.Infrastructure;
using Xunit;

namespace Cscentamint.Api.IntegrationTests;

/// <summary>
/// Tests for CommandLineHelp.TryShowHelp.
/// </summary>
public sealed class CommandLineHelpTests
{
    /// <summary>
    /// Verifies --help triggers help output and returns true.
    /// </summary>
    [Fact]
    public void TryShowHelp_WithDoubleDashHelp_ReturnsTrueAndWritesHelp()
    {
        using var writer = new StringWriter();
        var result = CommandLineHelp.TryShowHelp(["--help"], writer);

        Assert.True(result);
        var output = writer.ToString();
        Assert.Contains("CScentamint API", output);
        Assert.Contains("--host", output);
        Assert.Contains("--port", output);
        Assert.Contains("CSCENTAMINT_HOST", output);
    }

    /// <summary>
    /// Verifies -h triggers help output and returns true.
    /// </summary>
    [Fact]
    public void TryShowHelp_WithShortHelp_ReturnsTrueAndWritesHelp()
    {
        using var writer = new StringWriter();
        var result = CommandLineHelp.TryShowHelp(["-h"], writer);

        Assert.True(result);
        var output = writer.ToString();
        Assert.Contains("--verbose", output);
    }

    /// <summary>
    /// Verifies args without help flag return false and write nothing.
    /// </summary>
    [Fact]
    public void TryShowHelp_WithoutHelpFlag_ReturnsFalse()
    {
        using var writer = new StringWriter();
        var result = CommandLineHelp.TryShowHelp(["--port", "9000"], writer);

        Assert.False(result);
        Assert.Empty(writer.ToString());
    }

    /// <summary>
    /// Verifies null args return false.
    /// </summary>
    [Fact]
    public void TryShowHelp_NullArgs_ReturnsFalse()
    {
        using var writer = new StringWriter();
        var result = CommandLineHelp.TryShowHelp(null!, writer);

        Assert.False(result);
        Assert.Empty(writer.ToString());
    }

    /// <summary>
    /// Verifies empty args return false.
    /// </summary>
    [Fact]
    public void TryShowHelp_EmptyArgs_ReturnsFalse()
    {
        using var writer = new StringWriter();
        var result = CommandLineHelp.TryShowHelp([], writer);

        Assert.False(result);
        Assert.Empty(writer.ToString());
    }

    /// <summary>
    /// Verifies help is shown when --help appears after other args.
    /// </summary>
    [Fact]
    public void TryShowHelp_HelpNotFirst_StillReturnsTrue()
    {
        using var writer = new StringWriter();
        var result = CommandLineHelp.TryShowHelp(["--port", "8000", "--help"], writer);

        Assert.True(result);
        Assert.Contains("--port", writer.ToString());
    }

    /// <summary>
    /// Verifies the help exit path in Program is executed when entry point is invoked with --help (covers the return branch).
    /// </summary>
    [Fact]
    public void Program_WhenInvokedWithHelp_ExitsWithoutStartingServer()
    {
        var assembly = typeof(Program).Assembly;
        var entryPoint = assembly.EntryPoint;
        Assert.NotNull(entryPoint);

        var previousOut = Console.Out;
        try
        {
            using var capture = new StringWriter();
            Console.SetOut(capture);
            var args = new object[] { new[] { "--help" } };
            entryPoint.Invoke(null, args);
            Assert.Contains("--host", capture.ToString());
        }
        finally
        {
            Console.SetOut(previousOut);
        }
    }
}
