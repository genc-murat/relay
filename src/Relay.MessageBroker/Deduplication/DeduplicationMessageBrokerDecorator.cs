using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Relay.MessageBroker.Deduplication;

/// <summary>
/// Decorator that adds message deduplication capabilities to an IMessageBroker implementation.
/// </summary>
public sealed class DeduplicationMessageBrokerDecorator : IMessageBroker
{
    private readonly IMessageBroker _innerBroker;
    private readonly IDeduplicationCache _deduplicationCache;
    private readonly DeduplicationOptions _options;
    private readonly ILogger<DeduplicationMessageBrokerDecorator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeduplicationMessageBrokerDecorator"/> class.
    /// </summary>
    /// <param name="innerBroker">The inner message broker to decorate.</param>
    /// <param name="deduplicationCache">The deduplication cache.</param>
    /// <param name="options">The deduplication options.</param>
    /// <param name="logger">The logger.</param>
    public DeduplicationMessageBrokerDecorator(
        IMessageBroker innerBroker,
        IDeduplicationCache deduplicationCache,
        IOptions<DeduplicationOptions> options,
        ILogger<DeduplicationMessageBrokerDecorator> logger)
    {
        _innerBroker = innerBroker ?? throw new ArgumentNullException(nameof(innerBroker));
        _deduplicationCache = deduplicationCache ?? throw new ArgumentNullException(nameof(deduplicationCache));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _options.Validate();

        _logger.LogInformation(
            "DeduplicationMessageBrokerDecorator initialized. Deduplication enabled: {Enabled}, Strategy: {Strategy}",
            _options.Enabled,
            _options.Strategy);
    }

    /// <inheritdoc/>
    public async ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        // If deduplication is disabled, publish directly
        if (!_options.Enabled)
        {
            await _innerBroker.PublishAsync(message, options, cancellationToken);
            return;
        }

        // Generate message hash based on strategy
        var messageHash = GenerateMessageHash(message, options);

        // Check if message is a duplicate
        var isDuplicate = await _deduplicationCache.IsDuplicateAsync(messageHash, cancellationToken);

        if (isDuplicate)
        {
            _logger.LogInformation(
                "Duplicate message detected and discarded. Type: {MessageType}, Hash: {MessageHash}",
                typeof(TMessage).Name,
                messageHash);

            // Discard the duplicate message
            return;
        }

        try
        {
            // Publish the message
            await _innerBroker.PublishAsync(message, options, cancellationToken);

            // Add message hash to cache after successful publish
            await _deduplicationCache.AddAsync(messageHash, _options.Window, cancellationToken);

            _logger.LogTrace(
                "Message published and added to deduplication cache. Type: {MessageType}, Hash: {MessageHash}",
                typeof(TMessage).Name,
                messageHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish message. Type: {MessageType}, Hash: {MessageHash}",
                typeof(TMessage).Name,
                messageHash);

            // Re-throw to allow the broker's error handling to take over
            throw;
        }
    }

    /// <inheritdoc/>
    public ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Subscription is not affected by deduplication, delegate to inner broker
        return _innerBroker.SubscribeAsync(handler, options, cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        return _innerBroker.StartAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        return _innerBroker.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the deduplication metrics.
    /// </summary>
    /// <returns>The deduplication metrics.</returns>
    public DeduplicationMetrics GetMetrics()
    {
        return _deduplicationCache.GetMetrics();
    }

    /// <summary>
    /// Generates a message hash based on the configured strategy.
    /// </summary>
    private string GenerateMessageHash<TMessage>(TMessage message, PublishOptions? options)
    {
        return _options.Strategy switch
        {
            DeduplicationStrategy.ContentHash => GenerateContentHash(message),
            DeduplicationStrategy.MessageId => ExtractMessageId(options),
            DeduplicationStrategy.Custom => GenerateCustomHash(message),
            _ => throw new InvalidOperationException($"Unknown deduplication strategy: {_options.Strategy}")
        };
    }

    /// <summary>
    /// Generates a content-based hash using SHA256.
    /// </summary>
    private string GenerateContentHash<TMessage>(TMessage message)
    {
        var serializedMessage = JsonSerializer.SerializeToUtf8Bytes(message);
        return DeduplicationCache.GenerateContentHash(serializedMessage);
    }

    /// <summary>
    /// Extracts the message ID from publish options headers.
    /// </summary>
    private string ExtractMessageId(PublishOptions? options)
    {
        if (options?.Headers != null && options.Headers.TryGetValue("MessageId", out var messageId))
        {
            return messageId?.ToString() ?? throw new InvalidOperationException(
                "MessageId header is null when using MessageId deduplication strategy");
        }

        throw new InvalidOperationException(
            "MessageId header not found in publish options when using MessageId deduplication strategy");
    }

    /// <summary>
    /// Generates a custom hash using the configured custom hash function.
    /// </summary>
    private string GenerateCustomHash<TMessage>(TMessage message)
    {
        if (_options.CustomHashFunction == null)
        {
            throw new InvalidOperationException(
                "CustomHashFunction is not configured when using Custom deduplication strategy");
        }

        var serializedMessage = JsonSerializer.SerializeToUtf8Bytes(message);
        return _options.CustomHashFunction(serializedMessage);
    }
}
