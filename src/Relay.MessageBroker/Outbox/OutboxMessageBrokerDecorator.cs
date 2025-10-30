using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Relay.MessageBroker.Outbox;

/// <summary>
/// Decorator that implements the Outbox pattern for reliable message publishing.
/// </summary>
public sealed class OutboxMessageBrokerDecorator : IMessageBroker
{
    private readonly IMessageBroker _innerBroker;
    private readonly IOutboxStore _outboxStore;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxMessageBrokerDecorator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxMessageBrokerDecorator"/> class.
    /// </summary>
    /// <param name="innerBroker">The inner message broker to decorate.</param>
    /// <param name="outboxStore">The outbox store.</param>
    /// <param name="options">The outbox options.</param>
    /// <param name="logger">The logger.</param>
    public OutboxMessageBrokerDecorator(
        IMessageBroker innerBroker,
        IOutboxStore outboxStore,
        IOptions<OutboxOptions> options,
        ILogger<OutboxMessageBrokerDecorator> logger)
    {
        _innerBroker = innerBroker ?? throw new ArgumentNullException(nameof(innerBroker));
        _outboxStore = outboxStore ?? throw new ArgumentNullException(nameof(outboxStore));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        // If outbox is not enabled, delegate directly to inner broker
        if (!_options.Enabled)
        {
            await _innerBroker.PublishAsync(message, options, cancellationToken);
            return;
        }

        // Serialize the message using System.Text.Json (same as BaseMessageBroker)
        var payload = JsonSerializer.SerializeToUtf8Bytes(message);

        // Create outbox message
        var outboxMessage = new OutboxMessage
        {
            MessageType = typeof(TMessage).Name,
            Payload = payload,
            RoutingKey = options?.RoutingKey,
            Exchange = options?.Exchange,
            Headers = options?.Headers
        };

        // Store in outbox
        await _outboxStore.StoreAsync(outboxMessage, cancellationToken);

        _logger.LogDebug(
            "Stored message of type {MessageType} in outbox with ID {MessageId}",
            typeof(TMessage).Name,
            outboxMessage.Id);
    }

    /// <inheritdoc />
    public ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Subscription is not affected by outbox pattern, delegate to inner broker
        return _innerBroker.SubscribeAsync(handler, options, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        // Delegate to inner broker
        return _innerBroker.StartAsync(cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        // Delegate to inner broker
        return _innerBroker.StopAsync(cancellationToken);
    }
}
