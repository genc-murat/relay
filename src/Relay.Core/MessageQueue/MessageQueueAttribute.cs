using System;

namespace Relay.Core.MessageQueue
{
    /// <summary>
    /// Attribute to mark handlers that should be exposed as message queue consumers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class MessageQueueAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the message queue.
        /// </summary>
        public string QueueName { get; }

        /// <summary>
        /// Gets or sets the exchange name (for RabbitMQ, etc.).
        /// </summary>
        public string? ExchangeName { get; set; }

        /// <summary>
        /// Gets or sets the routing key.
        /// </summary>
        public string? RoutingKey { get; set; }

        /// <summary>
        /// Gets or sets whether to automatically acknowledge messages.
        /// </summary>
        public bool AutoAck { get; set; } = true;

        /// <summary>
        /// Gets or sets the prefetch count for the consumer.
        /// </summary>
        public ushort PrefetchCount { get; set; } = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageQueueAttribute"/> class.
        /// </summary>
        /// <param name="queueName">The name of the message queue.</param>
        public MessageQueueAttribute(string queueName)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                throw new ArgumentException("Queue name cannot be null or empty.", nameof(queueName));
            }

            QueueName = queueName;
        }
    }
}