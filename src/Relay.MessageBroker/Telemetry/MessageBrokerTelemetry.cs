using System.Diagnostics;

namespace Relay.MessageBroker.Telemetry;

/// <summary>
/// Message broker telemetry constants.
/// </summary>
public static class MessageBrokerTelemetry
{
    /// <summary>
    /// Activity source name for message broker operations.
    /// </summary>
    public const string ActivitySourceName = "Relay.MessageBroker";

    /// <summary>
    /// Activity source version.
    /// </summary>
    public const string ActivitySourceVersion = "1.0.0";

    /// <summary>
    /// Meter name for message broker metrics.
    /// </summary>
    public const string MeterName = "Relay.MessageBroker";

    /// <summary>
    /// Meter version.
    /// </summary>
    public const string MeterVersion = "1.0.0";

    /// <summary>
    /// Activity source for creating activities.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, ActivitySourceVersion);

    /// <summary>
    /// Span attribute keys.
    /// </summary>
    public static class Attributes
    {
        // Messaging attributes (OpenTelemetry semantic conventions)
        public const string MessagingSystem = "messaging.system";
        public const string MessagingDestination = "messaging.destination";
        public const string MessagingDestinationKind = "messaging.destination.kind";
        public const string MessagingOperation = "messaging.operation";
        public const string MessagingProtocol = "messaging.protocol";
        public const string MessagingProtocolVersion = "messaging.protocol.version";
        public const string MessagingUrl = "messaging.url";
        public const string MessagingMessageId = "messaging.message.id";
        public const string MessagingConversationId = "messaging.conversation.id";
        public const string MessagingPayloadSize = "messaging.message.payload_size_bytes";
        public const string MessagingPayloadCompressedSize = "messaging.message.payload_compressed_size_bytes";

        // Custom attributes
        public const string MessageType = "relay.message.type";
        public const string MessageVersion = "relay.message.version";
        public const string MessageCompressed = "relay.message.compressed";
        public const string MessageCompressionAlgorithm = "relay.message.compression.algorithm";
        public const string MessageCompressionRatio = "relay.message.compression.ratio";
        public const string MessageRetryCount = "relay.message.retry_count";
        public const string MessagePriority = "relay.message.priority";
        public const string MessageExpiration = "relay.message.expiration";
        public const string MessageRoutingKey = "relay.message.routing_key";
        public const string MessageExchange = "relay.message.exchange";
        public const string MessageQueue = "relay.message.queue";
        public const string MessageConsumerGroup = "relay.message.consumer_group";
        public const string MessagePartition = "relay.message.partition";
        public const string MessageOffset = "relay.message.offset";

        // Circuit breaker attributes
        public const string CircuitBreakerState = "relay.circuit_breaker.state";
        public const string CircuitBreakerName = "relay.circuit_breaker.name";

        // Error attributes
        public const string ErrorType = "error.type";
        public const string ErrorMessage = "error.message";
        public const string ErrorStackTrace = "error.stack_trace";
    }

    /// <summary>
    /// Span event names.
    /// </summary>
    public static class Events
    {
        public const string MessagePublished = "message.published";
        public const string MessageReceived = "message.received";
        public const string MessageProcessed = "message.processed";
        public const string MessageFailed = "message.failed";
        public const string MessageRetried = "message.retried";
        public const string MessageCompressed = "message.compressed";
        public const string MessageDecompressed = "message.decompressed";
        public const string CircuitBreakerOpened = "circuit_breaker.opened";
        public const string CircuitBreakerClosed = "circuit_breaker.closed";
        public const string CircuitBreakerHalfOpened = "circuit_breaker.half_opened";
    }

    /// <summary>
    /// Metric names.
    /// </summary>
    public static class Metrics
    {
        // Counters
        public const string MessagesPublished = "relay.messages.published";
        public const string MessagesReceived = "relay.messages.received";
        public const string MessagesProcessed = "relay.messages.processed";
        public const string MessagesFailed = "relay.messages.failed";
        public const string MessagesRetried = "relay.messages.retried";
        public const string MessagesCompressed = "relay.messages.compressed";
        public const string MessagesDecompressed = "relay.messages.decompressed";

        // Histograms
        public const string MessagePublishDuration = "relay.message.publish.duration";
        public const string MessageProcessDuration = "relay.message.process.duration";
        public const string MessagePayloadSize = "relay.message.payload.size";
        public const string MessageCompressionRatio = "relay.message.compression.ratio";

        // Gauges
        public const string CircuitBreakerState = "relay.circuit_breaker.state";
        public const string ActiveConnections = "relay.connections.active";
        public const string QueueSize = "relay.queue.size";
    }
}
