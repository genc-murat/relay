namespace Relay.Core;

/// <summary>
/// Exception thrown when multiple handlers are found for a request that expects a single handler.
/// </summary>
public class MultipleHandlersException : RelayException
{
    /// <summary>
    /// Initializes a new instance of the MultipleHandlersException class.
    /// </summary>
    /// <param name="requestType">The type name of the request.</param>
    public MultipleHandlersException(string requestType)
        : base(requestType, null, $"Multiple handlers found for request type '{requestType}'. Use named handlers or ensure only one handler is registered.")
    {
    }
}