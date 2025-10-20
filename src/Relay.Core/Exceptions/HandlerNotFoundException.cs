namespace Relay.Core;

/// <summary>
/// Exception thrown when no handler is found for a request.
/// </summary>
public class HandlerNotFoundException : RelayException
{
    /// <summary>
    /// Initializes a new instance of the HandlerNotFoundException class.
    /// </summary>
    /// <param name="requestType">The type name of the request.</param>
    public HandlerNotFoundException(string requestType)
        : base(requestType, null, $"No handler found for request type '{requestType}'")
    {
    }

    /// <summary>
    /// Initializes a new instance of the HandlerNotFoundException class with a handler name.
    /// </summary>
    /// <param name="requestType">The type name of the request.</param>
    /// <param name="handlerName">The name of the requested handler.</param>
    public HandlerNotFoundException(string requestType, string handlerName)
        : base(requestType, handlerName, $"No handler named '{handlerName}' found for request type '{requestType}'")
    {
    }
}
