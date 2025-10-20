using System;

namespace Relay.Core;

/// <summary>
/// Base exception for Relay framework errors.
/// </summary>
public class RelayException : Exception
{
    /// <summary>
    /// Gets the type name of the request that caused the exception.
    /// </summary>
    public string RequestType { get; }

    /// <summary>
    /// Gets the name of the handler, if applicable.
    /// </summary>
    public string? HandlerName { get; }

    /// <summary>
    /// Initializes a new instance of the RelayException class.
    /// </summary>
    /// <param name="requestType">The type name of the request.</param>
    /// <param name="handlerName">The name of the handler, if applicable.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception, if any.</param>
    public RelayException(string requestType, string? handlerName, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
        HandlerName = handlerName;
    }

    /// <summary>
    /// Returns a string representation of the exception including request type and handler name.
    /// </summary>
    public override string ToString()
    {
        var result = base.ToString();
        if (!string.IsNullOrWhiteSpace(RequestType))
        {
            result = $"RequestType: {RequestType}\n{result}";
        }
        if (!string.IsNullOrWhiteSpace(HandlerName))
        {
            result = $"HandlerName: {HandlerName}\n{result}";
        }
        return result;
    }
}
