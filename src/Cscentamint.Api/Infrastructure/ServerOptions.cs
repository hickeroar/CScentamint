namespace Cscentamint.Api.Infrastructure;

/// <summary>
/// Server configuration for host binding, port, and verbose request/response logging.
/// </summary>
public sealed class ServerOptions
{
    /// <summary>
    /// Host interface to bind. Default is 0.0.0.0.
    /// </summary>
    public string Host { get; set; } = "0.0.0.0";

    /// <summary>
    /// Port to bind. Default is 8000.
    /// </summary>
    public string Port { get; set; } = "8000";

    /// <summary>
    /// When true, log request method/path and body preview, then response status and body preview to stderr.
    /// </summary>
    public bool Verbose { get; set; }
}
