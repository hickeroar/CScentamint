namespace Cscentamint.Api.Infrastructure;

/// <summary>
/// Authentication configuration for optional bearer token enforcement.
/// </summary>
public sealed class AuthOptions
{
    /// <summary>
    /// Gets or sets the expected bearer token. When empty, auth is disabled.
    /// </summary>
    public string? Token { get; set; }
}
