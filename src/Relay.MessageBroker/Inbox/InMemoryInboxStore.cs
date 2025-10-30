using System.Collections.Concurrent;

namespace Relay.MessageBroker.Inbox;

/// <summary>
/// In-memory implementation of the inbox store for testing purposes.
/// </summary>
public sealed class InMemoryInboxStore : IInboxStore
{
    private readonly ConcurrentDictionary<string, InboxMessage> _messages = new();

    /// <inheritdoc />
    public ValueTask<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        return ValueTask.FromResult(_messages.ContainsKey(messageId));
    }

    /// <inheritdoc />
    public ValueTask StoreAsync(InboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.MessageId);

        message.ProcessedAt = DateTimeOffset.UtcNow;
        _messages[message.MessageId] = message;

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<int> CleanupExpiredAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTimeOffset.UtcNow - retentionPeriod;
        var expiredKeys = _messages
            .Where(kvp => kvp.Value.ProcessedAt < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        var removedCount = 0;
        foreach (var key in expiredKeys)
        {
            if (_messages.TryRemove(key, out _))
            {
                removedCount++;
            }
        }

        return ValueTask.FromResult(removedCount);
    }

    /// <summary>
    /// Gets all messages in the store (for testing purposes).
    /// </summary>
    public IReadOnlyCollection<InboxMessage> GetAll() => _messages.Values.ToList();

    /// <summary>
    /// Clears all messages from the store (for testing purposes).
    /// </summary>
    public void Clear() => _messages.Clear();
}
