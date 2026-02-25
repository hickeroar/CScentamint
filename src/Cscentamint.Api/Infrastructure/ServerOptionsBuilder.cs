namespace Cscentamint.Api.Infrastructure;

/// <summary>
/// Builds <see cref="ServerOptions"/> from configuration and environment variables (CSCENTAMINT_*).
/// </summary>
public static class ServerOptionsBuilder
{
    /// <summary>
    /// Builds server options from configuration, with environment variables (CSCENTAMINT_HOST, etc.) as overrides.
    /// </summary>
    /// <param name="configuration">Application configuration (includes command-line).</param>
    /// <param name="getEnv">Optional environment variable getter; defaults to <see cref="Environment.GetEnvironmentVariable(string)"/>.</param>
    /// <returns>Configured options. Env wins over config; config wins over defaults.</returns>
    public static ServerOptions Build(
        IConfiguration configuration,
        Func<string, string?>? getEnv = null)
    {
        getEnv ??= Environment.GetEnvironmentVariable;

        var host = GetString(getEnv, configuration, "CSCENTAMINT_HOST", "host", "0.0.0.0");
        var port = GetString(getEnv, configuration, "CSCENTAMINT_PORT", "port", "8000");
        var verbose = GetBool(getEnv, configuration, "CSCENTAMINT_VERBOSE", "verbose", false);

        return new ServerOptions
        {
            Host = host,
            Port = port,
            Verbose = verbose
        };
    }

    private static string GetString(
        Func<string, string?> getEnv,
        IConfiguration configuration,
        string envKey,
        string configKey,
        string defaultValue)
    {
        var env = getEnv(envKey);
        if (!string.IsNullOrWhiteSpace(env))
        {
            return env.Trim();
        }

        var config = configuration[configKey];
        if (!string.IsNullOrWhiteSpace(config))
        {
            return config.Trim();
        }

        return defaultValue;
    }

    private static bool GetBool(
        Func<string, string?> getEnv,
        IConfiguration configuration,
        string envKey,
        string configKey,
        bool defaultValue)
    {
        var env = getEnv(envKey);
        if (!string.IsNullOrWhiteSpace(env))
        {
            return IsTruthy(env.Trim());
        }

        var config = configuration[configKey];
        if (!string.IsNullOrWhiteSpace(config))
        {
            return IsTruthy(config.Trim());
        }

        return defaultValue;
    }

    private static bool IsTruthy(string value)
    {
        // Caller guarantees non-empty (GetBool only passes Trim() of non-whitespace). No separate length check.
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }
}
