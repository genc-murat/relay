using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Relay.MessageBroker.Inbox;

/// <summary>
/// Decorator that implements the Inbox pattern for idempotent message processing.
/// </summary>
public sealed class InboxMessageBrokerDecorator : IMessageBroker
{
    private readonly IMessageBroker _innerBroker;
    private readonly IInboxStore _inboxStore;
    private readonly InboxOptions _options;
    private readonly ILogger<InboxMessageBrokerDecorator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxMessageBrokerDecorator"/> class.
    /// </summary>
    /// <param name="innerBroker">The inner message broker to decorate.</param>
    /// <param name="inboxStore">The inbox store.</param>
    /// <param name="options">The inbox options.</param>
    /// <param name="logger">The logger.</param>
    public InboxMessageBrokerDecorator(
        IMessageBroker innerBroker,
        IInboxStore inboxStore,
        IOptions<InboxOptions> options,
        ILogger<InboxMessageBrokerDecorator> logger)
    {
        _innerBroker = innerBroker ?? throw new ArgumentNullException(nameof(innerBroker));
        _inboxStore = inboxStore ?? throw new ArgumentNullException(nameof(inboxStore));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Publishing is not affected by inbox pattern, delegate to inner broker
        return _innerBroker.PublishAsync(message, options, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        // If inbox is not enabled, delegate directly to inner broker
        if (!_options.Enabled)
        {
            return _innerBroker.SubscribeAsync(handler, options, cancellationToken);
        }

        // Wrap the handler with inbox logic
        var wrappedHandler = CreateInboxHandler(handler);

        return _innerBroker.SubscribeAsync(wrappedHandler, options, cancellationToken);
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

    private Func<TMessage, MessageContext, CancellationToken, ValueTask> CreateInboxHandler<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> originalHandler)
    {
        return async (message, context, ct) =>
        {
            // Extract message ID from context
            var messageId = context.MessageId;

            if (string.IsNullOrWhiteSpace(messageId))
            {
                _logger.LogWarning(
                    "Message of type {MessageType} has no MessageId, processing without inbox check",
                    typeof(TMessage).Name);

                // Process the message without inbox check
                await originalHandler(message, context, ct);
                return;
            }

            // Check if message has already been processed
            var exists = await _inboxStore.ExistsAsync(messageId, ct);

            if (exists)
            {
                _logger.LogInformation(
                    "Message {MessageId} of type {MessageType} already processed, skipping",
                    messageId,
                    typeof(TMessage).Name);

                // Acknowledge the message to remove it from the queue
                if (context.Acknowledge != null)
                {
                    await context.Acknowledge();
                }

                return;
            }

            try
            {
                // Process the message
                await originalHandler(message, context, ct);

                // Store message ID in inbox after successful processing
                var inboxMessage = new InboxMessage
                {
                    MessageId = messageId,
                    MessageType = typeof(TMessage).Name,
                    ConsumerName = _options.ConsumerName ?? Environment.MachineName
                };

                await _inboxStore.StoreAsync(inboxMessage, ct);

                _logger.LogDebug(
                    "Message {MessageId} of type {MessageType} processed and stored in inbox",
                    messageId,
                    typeof(TMessage).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process message {MessageId} of type {MessageType}",
                    messageId,
                    typeof(TMessage).Name);

                // Re-throw to allow the broker's error handling to take over
                throw;
            }
        };
    }
}
