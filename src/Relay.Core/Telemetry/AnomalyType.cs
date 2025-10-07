namespace Relay.Core.Telemetry;

/// <summary>
/// Types of performance anomalies
/// </summary>
public enum AnomalyType
{
    SlowExecution,
    HighFailureRate,
    UnusualItemCount,
    MemorySpike,
    TimeoutExceeded
}
