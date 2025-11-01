namespace Relay.MessageBroker;

/// <summary>
/// Configuration profiles for message broker.
/// </summary>
public enum MessageBrokerProfile
{
    /// <summary>
    /// Development profile with in-memory stores and minimal features.
    /// </summary>
    Development,

    /// <summary>
    /// Production profile with all reliability and observability features enabled.
    /// </summary>
    Production,

    /// <summary>
    /// High throughput profile optimized for performance.
    /// </summary>
    HighThroughput,

    /// <summary>
    /// High reliability profile with all resilience patterns enabled.
    /// </summary>
    HighReliability
}
