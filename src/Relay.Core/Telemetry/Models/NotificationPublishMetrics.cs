using System;
using System.Collections.Generic;

namespace Relay.Core.Telemetry;

/// <summary>
/// Metrics for notification publishing
/// </summary>
public class NotificationPublishMetrics
{
    public string OperationId { get; set; } = string.Empty;
    public Type NotificationType { get; set; } = null!;
    public int HandlerCount { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public Exception? Exception { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, object> Properties { get; set; } = new();
}
