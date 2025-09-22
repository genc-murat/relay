using System;
using System.Collections.Generic;

namespace Relay.Core.MessageQueue
{
    /// <summary>
    /// Wrapper for messages sent through the message queue.
    /// </summary>
    public class MessageWrapper
    {
        /// <summary>
        /// Gets or sets the unique identifier of the message.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the timestamp when the message was created.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the type of the message.
        /// </summary>
        public string MessageType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the serialized message content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the correlation ID for the message.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the reply-to address for the message.
        /// </summary>
        public string? ReplyTo { get; set; }

        /// <summary>
        /// Gets or sets additional properties for the message.
        /// </summary>
        public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}