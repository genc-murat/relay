using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core
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

    /// <summary>
    /// Represents the type of message queue provider.
    /// </summary>
    public enum MessageQueueProvider
    {
        /// <summary>
        /// Generic message queue provider.
        /// </summary>
        Generic,

        /// <summary>
        /// RabbitMQ message queue provider.
        /// </summary>
        RabbitMQ,

        /// <summary>
        /// Azure Service Bus message queue provider.
        /// </summary>
        AzureServiceBus,

        /// <summary>
        /// Amazon SQS message queue provider.
        /// </summary>
        AmazonSQS,

        /// <summary>
        /// Apache Kafka message queue provider.
        /// </summary>
        Kafka
    }

    /// <summary>
    /// Registry for message queue contracts generated at compile time.
    /// </summary>
    public static class MessageQueueContractRegistry
    {
        private static readonly Dictionary<Type, List<MessageQueueContract>> _contractsByMessageType = new();
        private static readonly List<MessageQueueContract> _allContracts = new();

        /// <summary>
        /// Gets all registered message queue contracts.
        /// </summary>
        public static IReadOnlyList<MessageQueueContract> AllContracts => _allContracts.AsReadOnly();

        /// <summary>
        /// Registers a message queue contract.
        /// </summary>
        /// <param name="contract">The contract to register.</param>
        public static void RegisterContract(MessageQueueContract contract)
        {
            if (!_contractsByMessageType.ContainsKey(contract.MessageType))
            {
                _contractsByMessageType[contract.MessageType] = new List<MessageQueueContract>();
            }

            _contractsByMessageType[contract.MessageType].Add(contract);
            _allContracts.Add(contract);
        }

        /// <summary>
        /// Gets message queue contracts for a specific message type.
        /// </summary>
        /// <param name="messageType">The message type to get contracts for.</param>
        /// <returns>The contracts for the message type, or empty list if none found.</returns>
        public static IReadOnlyList<MessageQueueContract> GetContractsForMessageType(Type messageType)
        {
            return _contractsByMessageType.TryGetValue(messageType, out var contracts)
                ? contracts.AsReadOnly()
                : Array.Empty<MessageQueueContract>();
        }

        /// <summary>
        /// Gets message queue contracts for a specific message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type to get contracts for.</typeparam>
        /// <returns>The contracts for the message type, or empty list if none found.</returns>
        public static IReadOnlyList<MessageQueueContract> GetContractsForMessageType<TMessage>()
        {
            return GetContractsForMessageType(typeof(TMessage));
        }

        /// <summary>
        /// Gets message queue contracts by provider type.
        /// </summary>
        /// <param name="provider">The provider type to filter by.</param>
        /// <returns>The contracts for the specified provider.</returns>
        public static IReadOnlyList<MessageQueueContract> GetContractsByProvider(MessageQueueProvider provider)
        {
            return _allContracts.Where(c => c.Provider == provider).ToList().AsReadOnly();
        }

        /// <summary>
        /// Clears all registered contracts. Used for testing.
        /// </summary>
        public static void Clear()
        {
            _contractsByMessageType.Clear();
            _allContracts.Clear();
        }
    }

    /// <summary>
    /// Generates message queue contracts from endpoint metadata.
    /// </summary>
    public static class MessageQueueContractGenerator
    {
        /// <summary>
        /// Generates message queue contracts from all registered endpoint metadata.
        /// </summary>
        /// <param name="options">Options for generating the contracts.</param>
        /// <returns>The generated message queue contracts.</returns>
        public static List<MessageQueueContract> GenerateContracts(MessageQueueGenerationOptions? options = null)
        {
            options ??= new MessageQueueGenerationOptions();

            var endpoints = EndpointMetadataRegistry.AllEndpoints.ToList();
            return GenerateContracts(endpoints, options);
        }

        /// <summary>
        /// Generates message queue contracts from the specified endpoint metadata.
        /// </summary>
        /// <param name="endpoints">The endpoint metadata to generate contracts from.</param>
        /// <param name="options">Options for generating the contracts.</param>
        /// <returns>The generated message queue contracts.</returns>
        public static List<MessageQueueContract> GenerateContracts(IEnumerable<EndpointMetadata> endpoints, MessageQueueGenerationOptions? options = null)
        {
            options ??= new MessageQueueGenerationOptions();

            var contracts = new List<MessageQueueContract>();

            foreach (var endpoint in endpoints)
            {
                var contract = new MessageQueueContract
                {
                    QueueName = GenerateQueueName(endpoint, options),
                    ExchangeName = GenerateExchangeName(endpoint, options),
                    RoutingKey = GenerateRoutingKey(endpoint, options),
                    MessageType = endpoint.RequestType,
                    ResponseType = endpoint.ResponseType,
                    HandlerType = endpoint.HandlerType,
                    HandlerMethodName = endpoint.HandlerMethodName,
                    MessageSchema = endpoint.RequestSchema,
                    ResponseSchema = endpoint.ResponseSchema,
                    Provider = options.DefaultProvider
                };

                contracts.Add(contract);
            }

            return contracts;
        }

        private static string GenerateQueueName(EndpointMetadata endpoint, MessageQueueGenerationOptions options)
        {
            var typeName = endpoint.RequestType.Name;

            // Remove common suffixes
            if (typeName.EndsWith("Request"))
                typeName = typeName.Substring(0, typeName.Length - 7);
            else if (typeName.EndsWith("Command"))
                typeName = typeName.Substring(0, typeName.Length - 7);
            else if (typeName.EndsWith("Query"))
                typeName = typeName.Substring(0, typeName.Length - 5);

            var queueName = ToKebabCase(typeName);

            if (!string.IsNullOrEmpty(options.QueuePrefix))
            {
                queueName = $"{options.QueuePrefix}.{queueName}";
            }

            if (!string.IsNullOrEmpty(endpoint.Version))
            {
                queueName = $"{queueName}.{endpoint.Version}";
            }

            return queueName;
        }

        private static string? GenerateExchangeName(EndpointMetadata endpoint, MessageQueueGenerationOptions options)
        {
            if (options.DefaultProvider != MessageQueueProvider.RabbitMQ)
            {
                return null;
            }

            return options.DefaultExchange ?? "relay.exchange";
        }

        private static string? GenerateRoutingKey(EndpointMetadata endpoint, MessageQueueGenerationOptions options)
        {
            if (options.DefaultProvider != MessageQueueProvider.RabbitMQ)
            {
                return null;
            }

            var typeName = endpoint.RequestType.Name;
            var routingKey = ToKebabCase(typeName);

            if (!string.IsNullOrEmpty(endpoint.Version))
            {
                routingKey = $"{endpoint.Version}.{routingKey}";
            }

            return routingKey;
        }

        private static string ToKebabCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = new System.Text.StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                var c = input[i];
                if (char.IsUpper(c) && i > 0)
                {
                    result.Append('-');
                }
                result.Append(char.ToLowerInvariant(c));
            }
            return result.ToString();
        }
    }

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