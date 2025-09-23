using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.MessageQueue
{
    /// <summary>
    /// In-memory implementation of IMessageQueuePublisher for testing and development.
    /// </summary>
    public class InMemoryMessageQueuePublisher : IMessageQueuePublisher
    {
        private readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _queues = new();

        /// <inheritdoc />
        public async ValueTask PublishAsync(string queueName, object message, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask; // Make method async for interface compliance

            var queue = _queues.GetOrAdd(queueName, _ => new ConcurrentQueue<string>());

            var wrapper = new MessageWrapper
            {
                MessageType = message.GetType().FullName ?? message.GetType().Name,
                Content = JsonSerializer.Serialize(message),
                CorrelationId = Activity.Current?.Id ?? Guid.NewGuid().ToString()
            };

            var json = JsonSerializer.Serialize(wrapper);
            queue.Enqueue(json);
        }

        /// <inheritdoc />
        public async ValueTask PublishAsync(string exchangeName, string routingKey, object message, CancellationToken cancellationToken = default)
        {
            // For simplicity, we'll treat exchange + routing key as a single queue name
            var queueName = $"{exchangeName}.{routingKey}";
            await PublishAsync(queueName, message, cancellationToken);
        }

        // Helper method to get messages from a queue (for testing)
        internal bool TryDequeueMessage(string queueName, out string? message)
        {
            message = null;
            return _queues.TryGetValue(queueName, out var queue) && queue.TryDequeue(out message);
        }
    }
}