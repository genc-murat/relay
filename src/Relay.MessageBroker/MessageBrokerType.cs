namespace Relay.MessageBroker;

/// <summary>
/// Supported message broker types.
/// </summary>
public enum MessageBrokerType
{
    /// <summary>
    /// RabbitMQ message broker.
    /// </summary>
    RabbitMQ,

    /// <summary>
    /// Apache Kafka message broker.
    /// </summary>
    Kafka,

    /// <summary>
    /// Azure Service Bus message broker.
    /// </summary>
    AzureServiceBus,

    /// <summary>
    /// AWS SQS/SNS message broker.
    /// </summary>
    AwsSqsSns,

    /// <summary>
    /// NATS message broker.
    /// </summary>
    Nats,

    /// <summary>
    /// Redis Streams message broker.
    /// </summary>
    RedisStreams,

    /// <summary>
    /// In-memory message broker for testing.
    /// </summary>
    InMemory
}
