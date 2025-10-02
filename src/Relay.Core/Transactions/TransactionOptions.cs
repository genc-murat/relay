using System;
using System.Transactions;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Configuration options for Transaction behavior.
    /// </summary>
    public class TransactionOptions
    {
        /// <summary>
        /// Gets or sets the transaction scope option. Default is Required.
        /// </summary>
        public TransactionScopeOption ScopeOption { get; set; } = TransactionScopeOption.Required;

        /// <summary>
        /// Gets or sets the transaction isolation level. Default is ReadCommitted.
        /// </summary>
        public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;

        /// <summary>
        /// Gets or sets the transaction timeout. Default is 1 minute.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);
    }
}
