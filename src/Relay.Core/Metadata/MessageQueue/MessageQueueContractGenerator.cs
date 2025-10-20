using System.Collections.Generic;
using System.Linq;
using Relay.Core.Metadata.Endpoints;

namespace Relay.Core.Metadata.MessageQueue
{
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

            if (!string.IsNullOrWhiteSpace(options.QueuePrefix))
            {
                queueName = $"{options.QueuePrefix}.{queueName}";
            }

            if (!string.IsNullOrWhiteSpace(endpoint.Version))
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

            if (!string.IsNullOrWhiteSpace(endpoint.Version))
            {
                routingKey = $"{endpoint.Version}.{routingKey}";
            }

            return routingKey;
        }

        private static string ToKebabCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
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
}