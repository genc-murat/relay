using System;

namespace Relay.Core.Diagnostics;

/// <summary>
/// Represents a single step in request processing
/// </summary>
public class TraceStep
{
    /// <summary>
    /// Name or description of this step
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// When this step occurred
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>
    /// How long this step took to execute
    /// </summary>
    public TimeSpan Duration { get; set; }
    
    /// <summary>
    /// Additional metadata about this step
    /// </summary>
    public object? Metadata { get; set; }
    
    /// <summary>
    /// The type of handler that executed this step (if applicable)
    /// </summary>
    public string? HandlerType { get; set; }
    
    /// <summary>
    /// Exception that occurred during this step (if any)
    /// </summary>
    public Exception? Exception { get; set; }
    
    /// <summary>
    /// The category of this step (Handler, Pipeline, Validation, etc.)
    /// </summary>
    public string Category { get; set; } = "Unknown";
    
    /// <summary>
    /// Whether this step completed successfully
    /// </summary>
    public bool IsSuccessful => Exception == null;
}