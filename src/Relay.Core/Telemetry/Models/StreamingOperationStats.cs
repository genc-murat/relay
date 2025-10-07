using System;

namespace Relay.Core.Telemetry;

/// <summary>
/// Statistics for streaming operations
/// </summary>
public class StreamingOperationStats
{
    public Type RequestType { get; set; } = null!;
    public string? HandlerName { get; set; }
    public long TotalOperations { get; set; }
    public long SuccessfulOperations { get; set; }
    public long FailedOperations { get; set; }
    public double SuccessRate => TotalOperations > 0 ? (double)SuccessfulOperations / TotalOperations : 0;
    public TimeSpan AverageOperationTime { get; set; }
    public long TotalItemsStreamed { get; set; }
    public double AverageItemsPerOperation { get; set; }
    public double ItemsPerSecond { get; set; }
    public DateTimeOffset LastOperation { get; set; }
}
