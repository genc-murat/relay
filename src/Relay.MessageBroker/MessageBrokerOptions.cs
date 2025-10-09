namespace Relay.MessageBroker;

/// <summary>
/// Configuration options for message broker.
/// </summary>
public sealed class MessageBrokerOptions : CommonMessageBrokerOptions
{
    /// <summary>
    /// Gets or sets the message broker type.
    /// </summary>
    public MessageBrokerType BrokerType { get; set; } = MessageBrokerType.RabbitMQ;

    /// <summary>
    /// Gets or sets the connection string.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets RabbitMQ-specific options.
    /// </summary>
    public RabbitMQOptions? RabbitMQ { get; set; }

    /// <summary>
    /// Gets or sets Kafka-specific options.
    /// </summary>
    public KafkaOptions? Kafka { get; set; }

    /// <summary>
    /// Gets or sets Azure Service Bus-specific options.
    /// </summary>
    public AzureServiceBusOptions? AzureServiceBus { get; set; }

    /// <summary>
    /// Gets or sets AWS SQS/SNS-specific options.
    /// </summary>
    public AwsSqsSnsOptions? AwsSqsSns { get; set; }

    /// <summary>
    /// Gets or sets NATS-specific options.
    /// </summary>
    public NatsOptions? Nats { get; set; }

    /// <summary>
    /// Gets or sets Redis Streams-specific options.
    /// </summary>
    public RedisStreamsOptions? RedisStreams { get; set; }
}


