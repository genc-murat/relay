namespace Relay.MessageBroker;

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