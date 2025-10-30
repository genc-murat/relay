using System.Collections.Concurrent;

namespace Relay.MessageBroker.PoisonMessage;

/// <summary>
/// In-memory implementation of poison message store for testing and development.
/// </summary>
public sealed class InMemoryPoisonMessageStore : IPoisonMessageStore
{
    private readonly ConcurrentDictionary<Guid, PoisonMessage> _messages = new();

    /// <inheritdoc />
    public ValueTask StoreAsync(PoisonMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.Id == Guid.Empty)
        {
            message.Id = Guid.NewGuid();
        }

        _messages[message.Id] = message;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<PoisonMessage?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        _messages.TryGetValue(messageId, out var message);
        return ValueTask.FromResult(message);
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<PoisonMessage>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<IEnumerable<PoisonMessage>>(_messages.Values.ToList());
    }

    /// <inheritdoc />
    public ValueTask RemoveAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        _messages.TryRemove(messageId, out _);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<int> CleanupExpiredAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTimeOffset.UtcNow - retentionPeriod;
        var expiredMessages = _messages.Values
            .Where(m => m.LastFailureAt < cutoffTime)
            .ToList();

        foreach (var message in expiredMessages)
        {
            _messages.TryRemove(message.Id, out _);
        }

        return ValueTask.FromResult(expiredMessages.Count);
    }

    /// <inheritdoc />
    public ValueTask UpdateAsync(PoisonMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (_messages.ContainsKey(message.Id))
        {
            _messages[message.Id] = message;
        }

        return ValueTask.CompletedTask;
    }
}
