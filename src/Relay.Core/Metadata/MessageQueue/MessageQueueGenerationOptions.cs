namespace Relay.Core.Metadata.MessageQueue
{
    /// <summary>
    /// Options for generating message queue contracts.
    /// </summary>
    public class MessageQueueGenerationOptions
    {
        /// <summary>
        /// Gets or sets the default message queue provider.
        /// </summary>
        public MessageQueueProvider DefaultProvider { get; set; } = MessageQueueProvider.Generic;

        /// <summary>
        /// Gets or sets the prefix for queue names.
        /// </summary>
        public string? QueuePrefix { get; set; }

        /// <summary>
        /// Gets or sets the default exchange name (for RabbitMQ).
        /// </summary>
        public string? DefaultExchange { get; set; }

        /// <summary>
        /// Gets or sets whether to include version information in queue names.
        /// </summary>
        public bool IncludeVersionInQueueName { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include handler information in contracts.
        /// </summary>
        public bool IncludeHandlerInfo { get; set; } = true;
    }
}