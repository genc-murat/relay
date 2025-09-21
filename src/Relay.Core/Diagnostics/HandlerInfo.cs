namespace Relay.Core.Diagnostics;

/// <summary>
/// Information about a specific handler registration
/// </summary>
public class HandlerInfo
{
    /// <summary>
    /// The request type this handler processes
    /// </summary>
    public string RequestType { get; set; } = string.Empty;
    
    /// <summary>
    /// The response type this handler returns (empty for notifications)
    /// </summary>
    public string ResponseType { get; set; } = string.Empty;
    
    /// <summary>
    /// The type containing the handler method
    /// </summary>
    public string HandlerType { get; set; } = string.Empty;
    
    /// <summary>
    /// The name of the handler method
    /// </summary>
    public string MethodName { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional name for named handlers
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Handler priority for execution order
    /// </summary>
    public int Priority { get; set; }
    
    /// <summary>
    /// Whether the handler is asynchronous
    /// </summary>
    public bool IsAsync { get; set; }
    
    /// <summary>
    /// Whether the handler returns a stream
    /// </summary>
    public bool IsStream { get; set; }
    
    /// <summary>
    /// Whether this is a notification handler
    /// </summary>
    public bool IsNotification { get; set; }
}