using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Relay.MessageBroker.Tests;

/// <summary>
/// In-memory message broker implementation for testing purposes.
/// </summary>
public sealed class InMemoryMessageBroker : IMessageBroker
{
    private readonly ConcurrentDictionary<Type, List<SubscriptionInfo>> _subscriptions = new();
    private readonly ConcurrentBag<PublishedMessage> _publishedMessages = new();
    private bool _isStarted;

    public IReadOnlyList<PublishedMessage> PublishedMessages => _publishedMessages.ToList();

    public ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var publishedMessage = new PublishedMessage
        {
            Message = message,
            MessageType = typeof(TMessage),
            Options = options,
            Timestamp = DateTimeOffset.UtcNow
        };

        _publishedMessages.Add(publishedMessage);

        // If started, immediately dispatch to subscribers
        if (_isStarted && _subscriptions.TryGetValue(typeof(TMessage), out var subscriptions))
        {
            var context = new MessageContext
            {
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = publishedMessage.Timestamp,
                RoutingKey = options?.RoutingKey,
                Exchange = options?.Exchange,
                Headers = options?.Headers
            };

            foreach (var subscription in subscriptions)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await subscription.Handler(message!, context, cancellationToken);
                    }
                    catch
                    {
                        // Swallow exceptions in test broker
                    }
                }, cancellationToken);
            }
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var subscriptionInfo = new SubscriptionInfo
        {
            MessageType = typeof(TMessage),
            Handler = async (msg, ctx, ct) => await handler((TMessage)msg, ctx, ct),
            Options = options ?? new SubscriptionOptions()
        };

        _subscriptions.AddOrUpdate(
            typeof(TMessage),
            _ => new List<SubscriptionInfo> { subscriptionInfo },
            (_, list) =>
            {
                list.Add(subscriptionInfo);
                return list;
            });

        return ValueTask.CompletedTask;
    }

    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        _isStarted = true;
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        _isStarted = false;
        return ValueTask.CompletedTask;
    }

    public void Clear()
    {
        _publishedMessages.Clear();
        _subscriptions.Clear();
    }

    private sealed class SubscriptionInfo
    {
        public required Type MessageType { get; init; }
        public required Func<object, MessageContext, CancellationToken, ValueTask> Handler { get; init; }
        public required SubscriptionOptions Options { get; init; }
    }

    public sealed class PublishedMessage
    {
        public required object Message { get; init; }
        public required Type MessageType { get; init; }
        public PublishOptions? Options { get; init; }
        public DateTimeOffset Timestamp { get; init; }
    }
}
