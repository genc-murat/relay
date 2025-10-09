namespace Relay.MessageBroker;

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