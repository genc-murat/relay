namespace Relay.Core.Configuration
{
    /// <summary>
    /// Configuration options for message queue integration.
    /// </summary>
    public class MessageQueueOptions
    {
        /// <summary>
        /// Gets or sets whether to enable message queue integration.
        /// </summary>
        public bool EnableMessageQueueIntegration { get; set; } = false;

        /// <summary>
        /// Gets or sets the default message queue implementation.
        /// </summary>
        public string DefaultMessageQueue { get; set; } = "InMemory";

        /// <summary>
        /// Gets or sets whether to automatically retry failed message processing.
        /// </summary>
        public bool EnableAutomaticRetry { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for failed message processing.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the dead letter queue name for failed messages.
        /// </summary>
        public string DeadLetterQueueName { get; set; } = "dead-letter";

        /// <summary>
        /// Gets or sets whether to automatically acknowledge messages.
        /// </summary>
        public bool AutoAck { get; set; } = true;

        /// <summary>
        /// Gets or sets the prefetch count for consumers.
        /// </summary>
        public ushort PrefetchCount { get; set; } = 1;
    }
}