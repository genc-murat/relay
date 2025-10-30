using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Relay.MessageBroker.PoisonMessage;

/// <summary>
/// Handles poison messages by tracking failures and moving messages to poison queue when threshold is exceeded.
/// </summary>
public sealed class PoisonMessageHandler : IPoisonMessageHandler
{
    private readonly IPoisonMessageStore _store;
    private readonly PoisonMessageOptions _options;
    private readonly ILogger<PoisonMessageHandler> _logger;
    private readonly ConcurrentDictionary<string, MessageFailureTracker> _failureTrackers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PoisonMessageHandler"/> class.
    /// </summary>
    /// <param name="store">The poison message store.</param>
    /// <param name="options">The poison message options.</param>
    /// <param name="logger">The logger.</param>
    public PoisonMessageHandler(
        IPoisonMessageStore store,
        IOptions<PoisonMessageOptions> options,
        ILogger<PoisonMessageHandler> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async ValueTask HandleAsync(PoisonMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.Id == Guid.Empty)
        {
            message.Id = Guid.NewGuid();
        }

        await _store.StoreAsync(message, cancellationToken);

        _logger.LogWarning(
            "Poison message stored. MessageId: {MessageId}, MessageType: {MessageType}, FailureCount: {FailureCount}, Errors: {Errors}",
            message.OriginalMessageId,
            message.MessageType,
            message.FailureCount,
            string.Join("; ", message.Errors));
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<PoisonMessage>> GetPoisonMessagesAsync(CancellationToken cancellationToken = default)
    {
        return await _store.GetAllAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ReprocessAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _store.GetByIdAsync(messageId, cancellationToken);
        
        if (message == null)
        {
            _logger.LogWarning("Poison message not found for reprocessing. MessageId: {MessageId}", messageId);
            return;
        }

        _logger.LogInformation(
            "Reprocessing poison message. MessageId: {MessageId}, MessageType: {MessageType}",
            message.OriginalMessageId,
            message.MessageType);

        // Remove from poison queue to allow reprocessing
        await _store.RemoveAsync(messageId, cancellationToken);

        // Clear failure tracker for this message
        if (!string.IsNullOrEmpty(message.OriginalMessageId))
        {
            _failureTrackers.TryRemove(message.OriginalMessageId, out _);
        }

        _logger.LogInformation(
            "Poison message removed from queue for reprocessing. MessageId: {MessageId}",
            message.OriginalMessageId);
    }

    /// <inheritdoc />
    public async ValueTask<bool> TrackFailureAsync(
        string messageId,
        string messageType,
        byte[] payload,
        string error,
        MessageContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageId);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(error);

        var tracker = _failureTrackers.GetOrAdd(messageId, _ => new MessageFailureTracker
        {
            MessageId = messageId,
            MessageType = messageType,
            Payload = payload,
            Context = context,
            FirstFailureAt = DateTimeOffset.UtcNow
        });

        tracker.FailureCount++;
        tracker.Errors.Add($"[{DateTimeOffset.UtcNow:O}] {error}");
        tracker.LastFailureAt = DateTimeOffset.UtcNow;

        _logger.LogWarning(
            "Message processing failure tracked. MessageId: {MessageId}, MessageType: {MessageType}, FailureCount: {FailureCount}/{Threshold}",
            messageId,
            messageType,
            tracker.FailureCount,
            _options.FailureThreshold);

        if (tracker.FailureCount >= _options.FailureThreshold)
        {
            _logger.LogError(
                "Message exceeded failure threshold. Moving to poison queue. MessageId: {MessageId}, MessageType: {MessageType}, FailureCount: {FailureCount}",
                messageId,
                messageType,
                tracker.FailureCount);

            var poisonMessage = new PoisonMessage
            {
                Id = Guid.NewGuid(),
                MessageType = messageType,
                Payload = payload,
                FailureCount = tracker.FailureCount,
                Errors = tracker.Errors.ToList(),
                FirstFailureAt = tracker.FirstFailureAt,
                LastFailureAt = tracker.LastFailureAt,
                OriginalMessageId = messageId,
                CorrelationId = context.CorrelationId,
                Headers = context.Headers,
                RoutingKey = context.RoutingKey,
                Exchange = context.Exchange
            };

            await HandleAsync(poisonMessage, cancellationToken);

            // Remove from failure tracker
            _failureTrackers.TryRemove(messageId, out _);

            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public async ValueTask<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        var removedCount = await _store.CleanupExpiredAsync(_options.RetentionPeriod, cancellationToken);

        if (removedCount > 0)
        {
            _logger.LogInformation(
                "Cleaned up {Count} expired poison messages older than {RetentionPeriod}",
                removedCount,
                _options.RetentionPeriod);
        }

        return removedCount;
    }

    /// <summary>
    /// Internal class to track message failures.
    /// </summary>
    private sealed class MessageFailureTracker
    {
        public string MessageId { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public byte[] Payload { get; set; } = Array.Empty<byte>();
        public MessageContext Context { get; set; } = new();
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTimeOffset FirstFailureAt { get; set; }
        public DateTimeOffset LastFailureAt { get; set; }
    }
}
