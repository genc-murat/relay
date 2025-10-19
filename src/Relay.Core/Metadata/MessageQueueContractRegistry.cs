using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core
{
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
}