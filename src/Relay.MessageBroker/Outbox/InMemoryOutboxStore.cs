using System.Collections.Concurrent;

namespace Relay.MessageBroker.Outbox;

/// <summary>
/// In-memory implementation of the outbox store for testing purposes.
/// </summary>
public sealed class InMemoryOutboxStore : IOutboxStore
{
    private readonly ConcurrentDictionary<Guid, OutboxMessage> _messages = new();

    /// <inheritdoc />
    public ValueTask<OutboxMessage> StoreAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.Id == Guid.Empty)
        {
            message.Id = Guid.NewGuid();
        }

        message.CreatedAt = DateTimeOffset.UtcNow;
        message.Status = OutboxMessageStatus.Pending;

        _messages[message.Id] = message;

        return ValueTask.FromResult(message);
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var pending = _messages.Values
            .Where(m => m.Status == OutboxMessageStatus.Pending)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToList();

        return ValueTask.FromResult<IEnumerable<OutboxMessage>>(pending);
    }

    /// <inheritdoc />
    public ValueTask MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(messageId, out var message))
        {
            message.Status = OutboxMessageStatus.Published;
            message.PublishedAt = DateTimeOffset.UtcNow;
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(messageId, out var message))
        {
            message.RetryCount++;
            message.LastError = error;
            message.Status = OutboxMessageStatus.Failed;
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<OutboxMessage>> GetFailedAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var failed = _messages.Values
            .Where(m => m.Status == OutboxMessageStatus.Failed)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToList();

        return ValueTask.FromResult<IEnumerable<OutboxMessage>>(failed);
    }

    /// <summary>
    /// Gets all messages in the store (for testing purposes).
    /// </summary>
    public IReadOnlyCollection<OutboxMessage> GetAll() => _messages.Values.ToList();

    /// <summary>
    /// Clears all messages from the store (for testing purposes).
    /// </summary>
    public void Clear() => _messages.Clear();
}
