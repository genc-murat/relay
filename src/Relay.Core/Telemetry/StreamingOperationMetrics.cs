using System;
using System.Collections.Generic;

namespace Relay.Core.Telemetry;

/// <summary>
/// Metrics for streaming operations
/// </summary>
public class StreamingOperationMetrics
{
    public string OperationId { get; set; } = string.Empty;
    public Type RequestType { get; set; } = null!;
    public Type ResponseType { get; set; } = null!;
    public string? HandlerName { get; set; }
    public TimeSpan Duration { get; set; }
    public long ItemCount { get; set; }
    public bool Success { get; set; }
    public Exception? Exception { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, object> Properties { get; set; } = new();
}
