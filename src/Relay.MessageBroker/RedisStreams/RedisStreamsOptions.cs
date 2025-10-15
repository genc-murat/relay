namespace Relay.MessageBroker;

/// <summary>
/// Redis Streams-specific options.
/// </summary>
public sealed class RedisStreamsOptions
{
    /// <summary>
    /// Gets or sets the Redis connection string.
    /// </summary>
    public string? ConnectionString { get; set; } = "localhost:6379";

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