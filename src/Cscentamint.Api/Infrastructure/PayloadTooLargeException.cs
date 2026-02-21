namespace Cscentamint.Api.Infrastructure;

/// <summary>
/// Indicates a request payload exceeded the allowed size limit.
/// </summary>
public sealed class PayloadTooLargeException : Exception
{
    /// <summary>
    /// Initializes a new payload-too-large exception with the default message.
    /// </summary>
    public PayloadTooLargeException()
        : base("request body too large")
    {
    }

    /// <summary>
    /// Initializes a new payload-too-large exception with a custom message.
    /// </summary>
    /// <param name="message">Custom exception message.</param>
    public PayloadTooLargeException(string message)
        : base(message)
    {
    }
}
