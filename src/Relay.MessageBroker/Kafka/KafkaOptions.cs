namespace Relay.MessageBroker;

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