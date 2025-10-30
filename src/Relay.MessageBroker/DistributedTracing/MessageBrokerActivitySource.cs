using System.Diagnostics;

namespace Relay.MessageBroker.DistributedTracing;

/// <summary>
/// Provides the ActivitySource for message broker operations.
/// </summary>
public static class MessageBrokerActivitySource
{
    /// <summary>
    /// The name of the activity source.
    /// </summary>
    public const string SourceName = "Relay.MessageBroker";

    /// <summary>
    /// The version of the activity source.
    /// </summary>
    public const string Version = "1.0.0";

    /// <summary>
    /// The ActivitySource instance for message broker operations.
    /// </summary>
    public static readonly ActivitySource Instance = new(SourceName, Version);

    /// <summary>
    /// Span attribute names.
    /// </summary>
    public static class AttributeNames
    {
        public const string MessageType = "messaging.message_type";
        public const string MessageSize = "messaging.message_size";
        public const string BrokerType = "messaging.broker_type";
        public const string RoutingKey = "messaging.routing_key";
        public const string Exchange = "messaging.exchange";
        public const string CorrelationId = "messaging.correlation_id";
        public const string ProcessingDuration = "messaging.processing_duration_ms";
        public const string Operation = "messaging.operation";
        public const string Destination = "messaging.destination";
        public const string System = "messaging.system";
    }

    /// <summary>
    /// Operation names.
    /// </summary>
    public static class Operations
    {
        public const string Publish = "publish";
        public const string Consume = "consume";
        public const string Process = "process";
    }
}
