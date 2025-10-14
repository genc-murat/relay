namespace Relay.MessageBroker;

/// <summary>
/// Common configuration options for all message brokers.
/// </summary>
public class CommonMessageBrokerOptions
{
    /// <summary>
    /// Gets or sets the default exchange name (for RabbitMQ).
    /// </summary>
    public string DefaultExchange { get; set; } = "relay.events";

    /// <summary>
    /// Gets or sets the default routing key pattern.
    /// </summary>
    public string DefaultRoutingKeyPattern { get; set; } = "{MessageType}";

    /// <summary>
    /// Gets or sets whether to automatically publish handler results.
    /// </summary>
    public bool AutoPublishResults { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable message serialization.
    /// </summary>
    public bool EnableSerialization { get; set; } = true;

    /// <summary>
    /// Gets or sets the message serializer type.
    /// </summary>
    public MessageSerializerType SerializerType { get; set; } = MessageSerializerType.Json;

    /// <summary>
    /// Gets or sets the retry policy.
    /// </summary>
    public RetryPolicy? RetryPolicy { get; set; }

    /// <summary>
    /// Gets or sets the circuit breaker options.
    /// </summary>
    public CircuitBreaker.CircuitBreakerOptions? CircuitBreaker { get; set; }

    /// <summary>
    /// Gets or sets the compression options.
    /// </summary>
    public Compression.CompressionOptions? Compression { get; set; }

    /// <summary>
    /// Gets or sets the telemetry options.
    /// </summary>
    public Relay.Core.Telemetry.RelayTelemetryOptions? Telemetry { get; set; }

    /// <summary>
    /// Gets or sets the saga options.
    /// </summary>
    public Saga.SagaOptions? Saga { get; set; }
}