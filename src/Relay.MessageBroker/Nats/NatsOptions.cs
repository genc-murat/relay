namespace Relay.MessageBroker;

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