using System;
using System.Collections.Generic;

namespace Relay.Core.Diagnostics.Tracing;

/// <summary>
/// Represents a complete trace of a request execution
/// </summary>
public class RequestTrace
{
    /// <summary>
    /// Unique identifier for this request trace
    /// </summary>
    public Guid RequestId { get; set; }

    /// <summary>
    /// The type of request being traced
    /// </summary>
    public Type RequestType { get; set; } = null!;

    /// <summary>
    /// When the request started processing
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// When the request finished processing (null if still in progress)
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// Total duration of request processing
    /// </summary>
    public TimeSpan? TotalDuration => EndTime - StartTime;

    /// <summary>
    /// Individual steps in the request processing pipeline
    /// </summary>
    public List<TraceStep> Steps { get; set; } = new();

    /// <summary>
    /// Exception that occurred during processing (if any)
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Whether the request has completed processing
    /// </summary>
    public bool IsCompleted => EndTime.HasValue;

    /// <summary>
    /// Whether the request completed successfully
    /// </summary>
    public bool IsSuccessful => IsCompleted && Exception == null;

    /// <summary>
    /// Optional correlation ID for distributed tracing
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Additional metadata about the request
    /// </summary>
    public Dictionary<string, object?> Metadata { get; set; } = new();
}