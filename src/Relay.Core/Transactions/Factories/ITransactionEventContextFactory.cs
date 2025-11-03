using System;
using Relay.Core.Transactions;

namespace Relay.Core.Transactions.Factories
{
    /// <summary>
    /// Factory for creating transaction event contexts.
    /// </summary>
    public interface ITransactionEventContextFactory
    {
        /// <summary>
        /// Creates a transaction event context for the given request type and configuration.
        /// </summary>
        /// <param name="requestType">The type of the request.</param>
        /// <param name="configuration">The transaction configuration.</param>
        /// <returns>A new transaction event context.</returns>
        TransactionEventContext CreateEventContext(string requestType, ITransactionConfiguration configuration);
    }
}