namespace Cscentamint.Api.Infrastructure;

/// <summary>
/// Command-line help for the API server. Used to print options and exit before starting the host.
/// </summary>
public static class CommandLineHelp
{
    private static readonly string HelpText = """
        CScentamint API - Trainable naive Bayesian text classification server.

        Usage:
          dotnet run --project src/Cscentamint.Api/Cscentamint.Api.csproj [options]

        CLI options:
          --host              Host interface to bind. (default: 0.0.0.0)
          --port              Port to bind. (default: 8000)
          --auth-token        Optional bearer token for non-probe endpoints.
          --language          Language code for stemmer and stop words. (default: english)
          --remove-stop-words Filter common stop words (the, is, and, etc.).
          --verbose           Log requests, responses, and classifier operations to stderr.
          --help, -h          Show this help.

        Environment variable equivalents:
          CSCENTAMINT_HOST
          CSCENTAMINT_PORT
          CSCENTAMINT_AUTH_TOKEN
          CSCENTAMINT_LANGUAGE
          CSCENTAMINT_REMOVE_STOP_WORDS   (1, true, yes = enabled)
          CSCENTAMINT_VERBOSE             (1, true, yes = enabled)
        """;

    /// <summary>
    /// If args contain --help or -h, writes help to <paramref name="output"/> and returns true.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="output">Where to write help text (e.g. Console.Out).</param>
    /// <returns>True if help was shown (caller should exit); false otherwise.</returns>
    public static bool TryShowHelp(string[] args, TextWriter output)
    {
        if (args == null || args.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "-h", StringComparison.Ordinal))
            {
                output.Write(HelpText);
                return true;
            }
        }

        return false;
    }
}
