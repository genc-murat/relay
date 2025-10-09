namespace Relay.MessageBroker;

/// <summary>
/// Configuration options for message broker.
/// </summary>
public sealed class MessageBrokerOptions
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
    public Relay.Core.Telemetry.UnifiedTelemetryOptions? Telemetry { get; set; }

    /// <summary>
    /// Gets or sets the saga options.
    /// </summary>
    public Saga.SagaOptions? Saga { get; set; }
}

/// <summary>
/// RabbitMQ-specific options.
/// </summary>
public sealed class RabbitMQOptions
{
    /// <summary>
    /// Gets or sets the hostname.
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the port.
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Gets or sets the virtual host.
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Gets or sets the connection timeout.
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets whether to use SSL.
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// Gets or sets the prefetch count for consumers.
    /// </summary>
    public ushort PrefetchCount { get; set; } = 10;

    /// <summary>
    /// Gets or sets the exchange type (direct, topic, fanout, headers).
    /// </summary>
    public string ExchangeType { get; set; } = "topic";
}

/// <summary>
/// Kafka-specific options.
/// </summary>
public sealed class KafkaOptions
{
    /// <summary>
    /// Gets or sets the bootstrap servers.
    /// </summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Gets or sets the consumer group ID.
    /// </summary>
    public string ConsumerGroupId { get; set; } = "relay-consumer-group";

    /// <summary>
    /// Gets or sets the auto-offset reset policy (earliest, latest).
    /// </summary>
    public string AutoOffsetReset { get; set; } = "earliest";

    /// <summary>
    /// Gets or sets whether to enable auto-commit.
    /// </summary>
    public bool EnableAutoCommit { get; set; } = false;

    /// <summary>
    /// Gets or sets the session timeout.
    /// </summary>
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the compression type (none, gzip, snappy, lz4, zstd).
    /// </summary>
    public string CompressionType { get; set; } = "none";

    /// <summary>
    /// Gets or sets the number of partitions for new topics.
    /// </summary>
    public int DefaultPartitions { get; set; } = 3;

    /// <summary>
    /// Gets or sets the replication factor for new topics.
    /// </summary>
    public short ReplicationFactor { get; set; } = 1;
}

/// <summary>
/// Message serializer types.
/// </summary>
public enum MessageSerializerType
{
    Json,
    MessagePack,
    Protobuf,
    Avro
}

/// <summary>
/// Retry policy for message processing.
/// </summary>
public sealed class RetryPolicy
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the initial retry delay.
    /// </summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the maximum retry delay.
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the backoff multiplier.
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets whether to use exponential backoff.
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;
}

/// <summary>
/// Azure Service Bus-specific options.
/// </summary>
public sealed class AzureServiceBusOptions
{
    /// <summary>
    /// Gets or sets the connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fully qualified namespace (e.g., myservicebus.servicebus.windows.net).
    /// </summary>
    public string? FullyQualifiedNamespace { get; set; }

    /// <summary>
    /// Gets or sets the default queue or topic name.
    /// </summary>
    public string DefaultEntityName { get; set; } = "relay-messages";

    /// <summary>
    /// Gets or sets the entity type (Queue or Topic).
    /// </summary>
    public AzureEntityType EntityType { get; set; } = AzureEntityType.Queue;

    /// <summary>
    /// Gets or sets the subscription name (for topics).
    /// </summary>
    public string? SubscriptionName { get; set; }

    /// <summary>
    /// Gets or sets the maximum concurrent calls.
    /// </summary>
    public int MaxConcurrentCalls { get; set; } = 10;

    /// <summary>
    /// Gets or sets the prefetch count.
    /// </summary>
    public int PrefetchCount { get; set; } = 10;

    /// <summary>
    /// Gets or sets the session enabled flag.
    /// </summary>
    public bool SessionsEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the auto-complete messages flag.
    /// </summary>
    public bool AutoCompleteMessages { get; set; } = false;

    /// <summary>
    /// Gets or sets the message time to live.
    /// </summary>
    public TimeSpan? MessageTimeToLive { get; set; }
}

/// <summary>
/// Azure Service Bus entity type.
/// </summary>
public enum AzureEntityType
{
    Queue,
    Topic
}

/// <summary>
/// AWS SQS/SNS-specific options.
/// </summary>
public sealed class AwsSqsSnsOptions
{
    /// <summary>
    /// Gets or sets the AWS region.
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Gets or sets the AWS access key ID.
    /// </summary>
    public string? AccessKeyId { get; set; }

    /// <summary>
    /// Gets or sets the AWS secret access key.
    /// </summary>
    public string? SecretAccessKey { get; set; }

    /// <summary>
    /// Gets or sets the default SQS queue URL.
    /// </summary>
    public string? DefaultQueueUrl { get; set; }

    /// <summary>
    /// Gets or sets the default SNS topic ARN.
    /// </summary>
    public string? DefaultTopicArn { get; set; }

    /// <summary>
    /// Gets or sets the SQS visibility timeout.
    /// </summary>
    public TimeSpan VisibilityTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the maximum number of messages to receive.
    /// </summary>
    public int MaxNumberOfMessages { get; set; } = 10;

    /// <summary>
    /// Gets or sets the wait time for long polling.
    /// </summary>
    public TimeSpan WaitTimeSeconds { get; set; } = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Gets or sets whether to use FIFO queues.
    /// </summary>
    public bool UseFifo { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to use FIFO queue.
    /// </summary>
    public bool UseFifoQueue { get; set; } = false;

    /// <summary>
    /// Gets or sets the message group ID for FIFO queues.
    /// </summary>
    public string? MessageGroupId { get; set; }

    /// <summary>
    /// Gets or sets the message deduplication ID for FIFO queues.
    /// </summary>
    public string? MessageDeduplicationId { get; set; }

    /// <summary>
    /// Gets or sets whether to auto-delete messages after processing.
    /// </summary>
    public bool AutoDeleteMessages { get; set; } = true;

    /// <summary>
    /// Gets or sets the message retention period.
    /// </summary>
    public TimeSpan MessageRetentionPeriod { get; set; } = TimeSpan.FromDays(4);
}

/// <summary>
/// NATS-specific options.
/// </summary>
public sealed class NatsOptions
{
    /// <summary>
    /// Gets or sets the NATS server URLs.
    /// </summary>
    public string[] Servers { get; set; } = new[] { "nats://localhost:4222" };

    /// <summary>
    /// Gets or sets the connection name.
    /// </summary>
    public string Name { get; set; } = "relay-nats-client";

    /// <summary>
    /// Gets or sets the client name.
    /// </summary>
    public string? ClientName { get; set; }

    /// <summary>
    /// Gets or sets the username for authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password for authentication.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the token for authentication.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets whether to use TLS.
    /// </summary>
    public bool UseTls { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum reconnect attempts.
    /// </summary>
    public int MaxReconnects { get; set; } = 10;

    /// <summary>
    /// Gets or sets the reconnect wait time.
    /// </summary>
    public TimeSpan ReconnectWait { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets or sets whether to use JetStream.
    /// </summary>
    public bool UseJetStream { get; set; } = false;

    /// <summary>
    /// Gets or sets the JetStream stream name.
    /// </summary>
    public string? StreamName { get; set; }

    /// <summary>
    /// Gets or sets the JetStream consumer name.
    /// </summary>
    public string? ConsumerName { get; set; }

    /// <summary>
    /// Gets or sets whether to auto-acknowledge messages.
    /// </summary>
    public bool AutoAck { get; set; } = true;

    /// <summary>
    /// Gets or sets the acknowledgment policy.
    /// </summary>
    public NatsAckPolicy AckPolicy { get; set; } = NatsAckPolicy.Explicit;

    /// <summary>
    /// Gets or sets the maximum pending acknowledgments.
    /// </summary>
    public int MaxAckPending { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the fetch batch size.
    /// </summary>
    public int FetchBatchSize { get; set; } = 10;
}

/// <summary>
/// NATS acknowledgment policy.
/// </summary>
public enum NatsAckPolicy
{
    None,
    Explicit,
    All
}

/// <summary>
/// Redis Streams-specific options.
/// </summary>
public sealed class RedisStreamsOptions
{
    /// <summary>
    /// Gets or sets the Redis connection string.
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Gets or sets the default stream name.
    /// </summary>
    public string DefaultStreamName { get; set; } = "relay:stream";

    /// <summary>
    /// Gets or sets the consumer group name.
    /// </summary>
    public string ConsumerGroupName { get; set; } = "relay-consumer-group";

    /// <summary>
    /// Gets or sets the consumer name.
    /// </summary>
    public string ConsumerName { get; set; } = "relay-consumer";

    /// <summary>
    /// Gets or sets the maximum number of messages to read.
    /// </summary>
    public int MaxMessagesToRead { get; set; } = 10;

    /// <summary>
    /// Gets or sets the read timeout.
    /// </summary>
    public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the maximum stream length.
    /// </summary>
    public int? MaxStreamLength { get; set; }

    /// <summary>
    /// Gets or sets whether to create consumer group if not exists.
    /// </summary>
    public bool CreateConsumerGroupIfNotExists { get; set; } = true;

    /// <summary>
    /// Gets or sets the database number.
    /// </summary>
    public int Database { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether to auto-acknowledge messages.
    /// </summary>
    public bool AutoAcknowledge { get; set; } = true;

    /// <summary>
    /// Gets or sets the connect timeout.
    /// </summary>
    public TimeSpan? ConnectTimeout { get; set; }

    /// <summary>
    /// Gets or sets the sync timeout.
    /// </summary>
    public TimeSpan? SyncTimeout { get; set; }
}
