using Microsoft.Extensions.Configuration;
using Cscentamint.Api.Infrastructure;
using Xunit;

namespace Cscentamint.Api.IntegrationTests;

/// <summary>
/// Tests for ServerOptionsBuilder.Build.
/// </summary>
public sealed class ServerOptionsBuilderTests
{
    /// <summary>
    /// Verifies default values when no config or env is set.
    /// </summary>
    [Fact]
    public void Build_NoConfigOrEnv_ReturnsDefaults()
    {
        var config = new ConfigurationBuilder().Build();
        string? GetEnv(string _) => null;
        var options = ServerOptionsBuilder.Build(config, GetEnv);

        Assert.Equal("0.0.0.0", options.Host);
        Assert.Equal("8000", options.Port);
        Assert.False(options.Verbose);
    }

    /// <summary>
    /// Verifies config overrides defaults.
    /// </summary>
    [Fact]
    public void Build_WithConfig_UsesConfigValues()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["host"] = "127.0.0.1",
                ["port"] = "9000",
                ["verbose"] = "true"
            })
            .Build();

        var options = ServerOptionsBuilder.Build(config);

        Assert.Equal("127.0.0.1", options.Host);
        Assert.Equal("9000", options.Port);
        Assert.True(options.Verbose);
    }

    /// <summary>
    /// Verifies env overrides config.
    /// </summary>
    [Fact]
    public void Build_WithEnv_EnvOverridesConfig()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["host"] = "127.0.0.1",
                ["port"] = "9000"
            })
            .Build();

        string? GetEnv(string key) => key switch
        {
            "CSCENTAMINT_HOST" => "10.0.0.1",
            "CSCENTAMINT_PORT" => "7000",
            _ => null
        };

        var options = ServerOptionsBuilder.Build(config, GetEnv);

        Assert.Equal("10.0.0.1", options.Host);
        Assert.Equal("7000", options.Port);
    }

    /// <summary>
    /// Verifies Verbose is true for "1", "true", "yes" (case-insensitive).
    /// </summary>
    [Theory]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("True")]
    [InlineData("yes")]
    [InlineData("YES")]
    public void Build_VerboseTruthyValues_ReturnsVerboseTrue(string value)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["verbose"] = value })
            .Build();

        var options = ServerOptionsBuilder.Build(config);

        Assert.True(options.Verbose);
    }

    /// <summary>
    /// Verifies Verbose is false for "false", "0", or empty.
    /// </summary>
    [Theory]
    [InlineData("false")]
    [InlineData("0")]
    [InlineData("")]
    public void Build_VerboseFalsyValues_ReturnsVerboseFalse(string value)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["verbose"] = value })
            .Build();

        string? GetEnv(string _) => null;
        var options = ServerOptionsBuilder.Build(config, GetEnv);

        Assert.False(options.Verbose);
    }

    /// <summary>
    /// Verifies CSCENTAMINT_VERBOSE env with "yes" returns Verbose true.
    /// </summary>
    [Fact]
    public void Build_EnvVerboseYes_ReturnsVerboseTrue()
    {
        var config = new ConfigurationBuilder().Build();
        string? GetEnv(string key) => key == "CSCENTAMINT_VERBOSE" ? "yes" : null;
        var options = ServerOptionsBuilder.Build(config, GetEnv);
        Assert.True(options.Verbose);
    }

    /// <summary>
    /// Verifies CSCENTAMINT_VERBOSE env overrides config.
    /// </summary>
    [Fact]
    public void Build_EnvVerboseTrue_OverridesConfig()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["verbose"] = "false" })
            .Build();

        string? GetEnv(string key) => key == "CSCENTAMINT_VERBOSE" ? "true" : null;

        var options = ServerOptionsBuilder.Build(config, GetEnv);

        Assert.True(options.Verbose);
    }

    /// <summary>
    /// Verifies whitespace-only env is treated as unset and config is used.
    /// </summary>
    [Fact]
    public void Build_EnvWhitespaceOnly_FallsBackToConfig()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["host"] = "127.0.0.2",
                ["port"] = "8888"
            })
            .Build();

        string? GetEnv(string key) => key switch
        {
            "CSCENTAMINT_HOST" => "   ",
            "CSCENTAMINT_PORT" => "",
            _ => null
        };

        var options = ServerOptionsBuilder.Build(config, GetEnv);

        Assert.Equal("127.0.0.2", options.Host);
        Assert.Equal("8888", options.Port);
    }

    /// <summary>
    /// Verifies non-truthy string for verbose returns false.
    /// </summary>
    [Fact]
    public void Build_VerboseInvalidValue_ReturnsVerboseFalse()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["verbose"] = "x" })
            .Build();

        string? GetEnv(string _) => null;
        var options = ServerOptionsBuilder.Build(config, GetEnv);

        Assert.False(options.Verbose);
    }
}
