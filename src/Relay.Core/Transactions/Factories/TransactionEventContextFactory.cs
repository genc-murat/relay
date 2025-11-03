using System;
using System.Collections.Generic;
using Relay.Core.Transactions;

namespace Relay.Core.Transactions.Factories
{
    /// <summary>
    /// Default implementation of transaction event context factory.
    /// </summary>
    public class TransactionEventContextFactory : ITransactionEventContextFactory
    {
        public TransactionEventContext CreateEventContext(string requestType, ITransactionConfiguration configuration)
        {
            return new TransactionEventContext
            {
                TransactionId = Guid.NewGuid().ToString(),
                RequestType = requestType,
                IsolationLevel = configuration.IsolationLevel,
                NestingLevel = 0,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["IsReadOnly"] = configuration.IsReadOnly,
                    ["Timeout"] = configuration.Timeout,
                    ["UseDistributedTransaction"] = configuration.UseDistributedTransaction
                }
            };
        }
    }
}