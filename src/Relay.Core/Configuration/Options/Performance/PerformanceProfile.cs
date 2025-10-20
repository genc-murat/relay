namespace Relay.Core.Configuration.Options.Performance;

/// <summary>
/// Performance profile presets
/// </summary>
public enum PerformanceProfile
{
    /// <summary>
    /// Optimize for memory usage
    /// </summary>
    LowMemory,

    /// <summary>
    /// Balanced performance and memory
    /// </summary>
    Balanced,

    /// <summary>
    /// Optimize for maximum throughput
    /// </summary>
    HighThroughput,

    /// <summary>
    /// Optimize for lowest latency
    /// </summary>
    UltraLowLatency,

    /// <summary>
    /// Custom configuration
    /// </summary>
    Custom
}
