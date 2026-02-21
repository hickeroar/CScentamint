namespace Cscentamint.Api.Infrastructure;

/// <summary>
/// Tracks API readiness for probe endpoints.
/// </summary>
public sealed class ReadinessState
{
    private bool _isReady = true;

    /// <summary>
    /// Gets whether the API is currently ready to serve requests.
    /// </summary>
    public bool IsReady => _isReady;

    /// <summary>
    /// Marks the API as not ready.
    /// </summary>
    public void MarkNotReady()
    {
        _isReady = false;
    }
}
