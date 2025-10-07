using System;
using System.Collections.Generic;

namespace Relay.Core.Telemetry;

/// <summary>
/// Metrics for handler execution
/// </summary>
public class HandlerExecutionMetrics
{
    public string OperationId { get; set; } = string.Empty;
    public Type RequestType { get; set; } = null!;
    public Type? ResponseType { get; set; }
    public string? HandlerName { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public Exception? Exception { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, object> Properties { get; set; } = new();
}
