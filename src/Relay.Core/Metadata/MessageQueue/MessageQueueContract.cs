using System;
using System.Collections.Generic;

namespace Relay.Core.Metadata.MessageQueue
{
    /// <summary>
    /// Represents a message queue contract for a handler.
    /// </summary>
    public class MessageQueueContract
    {
        /// <summary>
        /// Gets or sets the name of the message queue.
        /// </summary>
        public string QueueName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the exchange name (for RabbitMQ, etc.).
        /// </summary>
        public string? ExchangeName { get; set; }

        /// <summary>
        /// Gets or sets the routing key.
        /// </summary>
        public string? RoutingKey { get; set; }

        /// <summary>
        /// Gets or sets the message type.
        /// </summary>
        public Type MessageType { get; set; } = null!;

        /// <summary>
        /// Gets or sets the response type (if applicable).
        /// </summary>
        public Type? ResponseType { get; set; }

        /// <summary>
        /// Gets or sets the handler type.
        /// </summary>
        public Type HandlerType { get; set; } = null!;

        /// <summary>
        /// Gets or sets the handler method name.
        /// </summary>
        public string HandlerMethodName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message schema.
        /// </summary>
        public JsonSchemaContract? MessageSchema { get; set; }

        /// <summary>
        /// Gets or sets the response schema (if applicable).
        /// </summary>
        public JsonSchemaContract? ResponseSchema { get; set; }

        /// <summary>
        /// Gets or sets the message queue provider type.
        /// </summary>
        public MessageQueueProvider Provider { get; set; } = MessageQueueProvider.Generic;

        /// <summary>
        /// Gets or sets additional properties for the contract.
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}