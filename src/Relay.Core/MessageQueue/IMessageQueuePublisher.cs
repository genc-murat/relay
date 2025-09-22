using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.MessageQueue
{
    /// <summary>
    /// Interface for message queue publishers.
    /// </summary>
    public interface IMessageQueuePublisher
    {
        /// <summary>
        /// Publishes a message to the message queue.
        /// </summary>
        /// <param name="queueName">The name of the queue.</param>
        /// <param name="message">The message to publish.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask representing the completion of the operation.</returns>
        ValueTask PublishAsync(string queueName, object message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a message to the message queue with a routing key.
        /// </summary>
        /// <param name="exchangeName">The name of the exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to publish.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask representing the completion of the operation.</returns>
        ValueTask PublishAsync(string exchangeName, string routingKey, object message, CancellationToken cancellationToken = default);
    }
}