using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.MessageQueue
{
    /// <summary>
    /// In-memory implementation of IMessageQueueConsumer for testing and development.
    /// </summary>
    public class InMemoryMessageQueueConsumer : IMessageQueueConsumer
    {
        private readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _queues = new();
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _consumers = new();
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryMessageQueueConsumer"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for resolving handlers.</param>
        public InMemoryMessageQueueConsumer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <inheritdoc />
        public async ValueTask StartConsumingAsync(string queueName, Func<object, CancellationToken, ValueTask> messageHandler, CancellationToken cancellationToken = default)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _consumers[queueName] = cts;

            // Start consuming in a background task
            _ = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        if (_queues.TryGetValue(queueName, out var queue) && queue.TryDequeue(out var json))
                        {
                            var wrapper = JsonSerializer.Deserialize<MessageWrapper>(json);
                            if (wrapper != null)
                            {
                                // Deserialize the actual message
                                var messageType = Type.GetType(wrapper.MessageType);
                                if (messageType != null)
                                {
                                    var message = JsonSerializer.Deserialize(wrapper.Content, messageType);
                                    if (message != null)
                                    {
                                        await messageHandler(message, cts.Token);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // No messages, wait a bit before checking again
                            await Task.Delay(100, cts.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Consumer was stopped
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Log error and continue
                        Console.WriteLine($"Error consuming message from queue {queueName}: {ex.Message}");
                    }
                }
            }, cts.Token);
        }

        /// <inheritdoc />
        public async ValueTask StartConsumingAsync(string exchangeName, string routingKey, Func<object, CancellationToken, ValueTask> messageHandler, CancellationToken cancellationToken = default)
        {
            // For simplicity, we'll treat exchange + routing key as a single queue name
            var queueName = $"{exchangeName}.{routingKey}";
            await StartConsumingAsync(queueName, messageHandler, cancellationToken);
        }

        /// <inheritdoc />
        public async ValueTask StopConsumingAsync(CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask; // Make method async for interface compliance

            foreach (var cts in _consumers.Values)
            {
                cts.Cancel();
            }

            _consumers.Clear();
        }

        // Helper method to add messages to a queue (for testing)
        internal void EnqueueMessage(string queueName, string json)
        {
            var queue = _queues.GetOrAdd(queueName, _ => new ConcurrentQueue<string>());
            queue.Enqueue(json);
        }
    }
}