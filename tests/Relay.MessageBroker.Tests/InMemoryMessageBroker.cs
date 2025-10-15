using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Relay.MessageBroker.Tests;

/// <summary>
/// In-memory message broker implementation for testing purposes.
/// </summary>
public sealed class InMemoryMessageBroker : IMessageBroker
{
    private readonly ConcurrentDictionary<Type, ConcurrentBag<SubscriptionInfo>> _subscriptions = new();
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
                Headers = options?.Headers,
                Acknowledge = () => ValueTask.CompletedTask,
                Reject = (requeue) => ValueTask.CompletedTask
            };

            // Create a snapshot of subscriptions to avoid concurrent modification issues
            var subscriptionSnapshot = subscriptions.ToList();
            foreach (var subscription in subscriptionSnapshot)
            {
                // Check if routing keys match
                if (MatchesRoutingKey(subscription.Options.RoutingKey, options?.RoutingKey))
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

        var bag = _subscriptions.GetOrAdd(typeof(TMessage), _ => new ConcurrentBag<SubscriptionInfo>());
        bag.Add(subscriptionInfo);

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

    private static bool MatchesRoutingKey(string? subscriptionKey, string? messageKey)
    {
        // If no routing key specified in subscription, match all
        if (string.IsNullOrEmpty(subscriptionKey))
            return true;

        // If no routing key in message, only match if subscription also has none
        if (string.IsNullOrEmpty(messageKey))
            return string.IsNullOrEmpty(subscriptionKey);

        // Simple wildcard matching: * matches any sequence of characters
        if (subscriptionKey.Contains('*'))
        {
            var pattern = "^" + Regex.Escape(subscriptionKey).Replace("\\*", ".*") + "$";
            return Regex.IsMatch(messageKey, pattern);
        }

        // Exact match
        return subscriptionKey == messageKey;
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
