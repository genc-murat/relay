namespace Relay.MessageBroker;

using System.Collections.Generic;
using Relay.Core.Validation.Interfaces;

/// <summary>
/// Publishing options for message brokers.
/// </summary>
public sealed class PublishOptions
{
    /// <summary>
    /// Gets or sets the routing key or topic name.
    /// </summary>
    public string? RoutingKey { get; set; }

    /// <summary>
    /// Gets or sets the exchange name (for RabbitMQ).
    /// </summary>
    public string? Exchange { get; set; }

    /// <summary>
    /// Gets or sets custom headers.
    /// </summary>
    public Dictionary<string, object>? Headers { get; set; }

    /// <summary>
    /// Gets or sets the message priority (0-9).
    /// </summary>
    public byte? Priority { get; set; }

    /// <summary>
    /// Gets or sets the message expiration time.
    /// </summary>
    public TimeSpan? Expiration { get; set; }

    /// <summary>
    /// Gets or sets whether the message should be persisted.
    /// </summary>
    public bool Persistent { get; set; } = true;

    /// <summary>
    /// Gets or sets the validator for the message.
    /// </summary>
    public IValidator<object>? Validator { get; set; }

    /// <summary>
    /// Gets or sets the JSON schema contract for validation.
    /// </summary>
    public object? Schema { get; set; }
}
