using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.MessageQueue
{
    /// <summary>
    /// Interface for message queue consumers.
    /// </summary>
    public interface IMessageQueueConsumer
    {
        /// <summary>
        /// Starts consuming messages from the message queue.
        /// </summary>
        /// <param name="queueName">The name of the queue.</param>
        /// <param name="messageHandler">The handler for processing messages.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask representing the completion of the operation.</returns>
        ValueTask StartConsumingAsync(string queueName, Func<object, CancellationToken, ValueTask> messageHandler, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts consuming messages from the message queue with a routing key.
        /// </summary>
        /// <param name="exchangeName">The name of the exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="messageHandler">The handler for processing messages.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask representing the completion of the operation.</returns>
        ValueTask StartConsumingAsync(string exchangeName, string routingKey, Func<object, CancellationToken, ValueTask> messageHandler, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops consuming messages from the message queue.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask representing the completion of the operation.</returns>
        ValueTask StopConsumingAsync(CancellationToken cancellationToken = default);
    }
}