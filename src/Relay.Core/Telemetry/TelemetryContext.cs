using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Relay.Core.Telemetry;

/// <summary>
/// Context information for telemetry operations
/// </summary>
public class TelemetryContext
{
    /// <summary>
    /// Unique identifier for this request
    /// </summary>
    public string RequestId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Correlation ID for distributed tracing
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Type of the request being processed
    /// </summary>
    public Type RequestType { get; set; } = null!;

    /// <summary>
    /// Type of the response (if applicable)
    /// </summary>
    public Type? ResponseType { get; set; }

    /// <summary>
    /// Name of the handler processing the request
    /// </summary>
    public string? HandlerName { get; set; }

    /// <summary>
    /// Start time of the operation
    /// </summary>
    public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Additional properties for telemetry
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Associated activity for distributed tracing
    /// </summary>
    public Activity? Activity { get; set; }

    /// <summary>
    /// Creates a new telemetry context
    /// </summary>
    public static TelemetryContext Create(Type requestType, Type? responseType = null, string? handlerName = null, string? correlationId = null, Activity? activity = null)
    {
        return new TelemetryContext
        {
            RequestType = requestType,
            ResponseType = responseType,
            HandlerName = handlerName,
            CorrelationId = correlationId,
            Activity = activity
        };
    }
}