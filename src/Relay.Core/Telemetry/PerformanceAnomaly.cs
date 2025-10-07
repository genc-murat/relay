using System;

namespace Relay.Core.Telemetry;

/// <summary>
/// Represents a performance anomaly
/// </summary>
public class PerformanceAnomaly
{
    public string OperationId { get; set; } = string.Empty;
    public Type RequestType { get; set; } = null!;
    public string? HandlerName { get; set; }
    public AnomalyType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public TimeSpan ActualDuration { get; set; }
    public TimeSpan ExpectedDuration { get; set; }
    public double Severity { get; set; }
    public DateTimeOffset DetectedAt { get; set; } = DateTimeOffset.UtcNow;
}
