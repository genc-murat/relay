namespace Relay.MessageBroker;

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