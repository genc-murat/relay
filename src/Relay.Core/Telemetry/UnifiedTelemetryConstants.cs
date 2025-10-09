using System.Diagnostics;

namespace Relay.Core.Telemetry;

/// <summary>
/// Unified telemetry constants for all Relay components
/// </summary>
public static class UnifiedTelemetryConstants
{
    /// <summary>
    /// Activity source name for Relay operations
    /// </summary>
    public const string ActivitySourceName = "Relay";

    /// <summary>
    /// Activity source version
    /// </summary>
    public const string ActivitySourceVersion = "1.0.0";

    /// <summary>
    /// Meter name for Relay metrics
    /// </summary>
    public const string MeterName = "Relay";

    /// <summary>
    /// Meter version
    /// </summary>
    public const string MeterVersion = "1.0.0";

    /// <summary>
    /// Activity source for creating activities
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, ActivitySourceVersion);

    /// <summary>
    /// Component names for different Relay parts
    /// </summary>
    public static class Components
    {
        public const string Core = "Relay.Core";
        public const string MessageBroker = "Relay.MessageBroker";
        public const string SourceGenerator = "Relay.SourceGenerator";
        public const string CLI = "Relay.CLI";
    }

    /// <summary>
    /// Unified span attribute keys
    /// </summary>
    public static class Attributes
    {
        // Core attributes
        public const string Component = "relay.component";
        public const string OperationType = "relay.operation_type";
        public const string CorrelationId = "relay.correlation_id";
        public const string RequestType = "relay.request_type";
        public const string ResponseType = "relay.response_type";
        public const string HandlerName = "relay.handler_name";
        public const string Duration = "relay.duration_ms";
        public const string Success = "relay.success";
        public const string ExceptionType = "relay.exception_type";
        public const string ExceptionMessage = "relay.exception_message";

        // Message broker specific attributes (from MessageBrokerTelemetry)
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

        // Message broker custom attributes
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

        // Notification attributes
        public const string NotificationType = "relay.notification_type";
        public const string HandlerCount = "relay.handler_count";

        // Streaming attributes
        public const string ItemCount = "relay.item_count";
        public const string ItemsPerSecond = "relay.items_per_second";

        // Circuit breaker attributes
        public const string CircuitBreakerState = "relay.circuit_breaker.state";
        public const string CircuitBreakerName = "relay.circuit_breaker.name";

        // Error attributes
        public const string ErrorType = "error.type";
        public const string ErrorMessage = "error.message";
        public const string ErrorStackTrace = "error.stack_trace";
    }

    /// <summary>
    /// Unified span event names
    /// </summary>
    public static class Events
    {
        // Core events
        public const string HandlerStarted = "handler.started";
        public const string HandlerCompleted = "handler.completed";
        public const string HandlerFailed = "handler.failed";
        public const string NotificationPublished = "notification.published";
        public const string NotificationFailed = "notification.failed";
        public const string StreamingStarted = "streaming.started";
        public const string StreamingCompleted = "streaming.completed";
        public const string StreamingFailed = "streaming.failed";

        // Message broker events (from MessageBrokerTelemetry)
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
    /// Unified metric names
    /// </summary>
    public static class Metrics
    {
        // Core metrics
        public const string HandlersExecuted = "relay.handlers.executed";
        public const string HandlersSucceeded = "relay.handlers.succeeded";
        public const string HandlersFailed = "relay.handlers.failed";
        public const string NotificationsPublished = "relay.notifications.published";
        public const string StreamingOperations = "relay.streaming.operations";
        public const string HandlerDuration = "relay.handler.duration";
        public const string NotificationDuration = "relay.notification.duration";
        public const string StreamingDuration = "relay.streaming.duration";

        // Message broker metrics (from MessageBrokerTelemetry)
        public const string MessagesPublished = "relay.messages.published";
        public const string MessagesReceived = "relay.messages.received";
        public const string MessagesProcessed = "relay.messages.processed";
        public const string MessagesFailed = "relay.messages.failed";
        public const string MessagesRetried = "relay.messages.retried";
        public const string MessagesCompressed = "relay.messages.compressed";
        public const string MessagesDecompressed = "relay.messages.decompressed";
        public const string MessagePublishDuration = "relay.message.publish.duration";
        public const string MessageProcessDuration = "relay.message.process.duration";
        public const string MessagePayloadSize = "relay.message.payload.size";
        public const string MessageCompressionRatio = "relay.message.compression.ratio";

        // System metrics
        public const string CircuitBreakerState = "relay.circuit_breaker.state";
        public const string ActiveConnections = "relay.connections.active";
        public const string QueueSize = "relay.queue.size";
    }
}