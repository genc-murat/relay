using System;
using System.Collections.Generic;
using System.Data;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Provides context information for transaction lifecycle events.
    /// </summary>
    /// <remarks>
    /// This class is passed to transaction event handlers and contains metadata about the transaction
    /// at the time the event occurred. It allows event handlers to make decisions based on transaction
    /// characteristics such as isolation level, nesting level, and custom metadata.
    /// 
    /// Example usage in an event handler:
    /// <code>
    /// public class AuditEventHandler : ITransactionEventHandler
    /// {
    ///     public async Task OnAfterCommitAsync(TransactionEventContext context, CancellationToken ct)
    ///     {
    ///         await _auditLog.WriteAsync($"Transaction {context.TransactionId} committed at {context.Timestamp}");
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public class TransactionEventContext
    {
        /// <summary>
        /// Gets or sets the unique identifier for the transaction.
        /// </summary>
        /// <remarks>
        /// This ID is used for correlation across logs, traces, and metrics.
        /// </remarks>
        public string TransactionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type name of the request being processed.
        /// </summary>
        /// <remarks>
        /// This is the fully qualified type name of the ITransactionalRequest implementation.
        /// Useful for filtering and categorizing transaction events by request type.
        /// </remarks>
        public string RequestType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the isolation level of the transaction.
        /// </summary>
        /// <remarks>
        /// The isolation level determines the locking behavior and consistency guarantees.
        /// Event handlers can use this to implement different logic based on isolation level.
        /// </remarks>
        public IsolationLevel IsolationLevel { get; set; }

        /// <summary>
        /// Gets or sets the nesting level of the transaction.
        /// </summary>
        /// <remarks>
        /// - Level 0: Outermost transaction
        /// - Level 1+: Nested transaction
        /// 
        /// Event handlers can use this to determine if they should execute logic only for
        /// the outermost transaction or for all nested transactions.
        /// </remarks>
        public int NestingLevel { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the event occurred.
        /// </summary>
        /// <remarks>
        /// This is the UTC timestamp when the transaction event was raised.
        /// Useful for calculating durations and ordering events.
        /// </remarks>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets a dictionary of custom metadata associated with the transaction.
        /// </summary>
        /// <remarks>
        /// This dictionary can be used to pass custom data between event handlers or to store
        /// additional context information about the transaction. Event handlers can read and write
        /// to this dictionary to share state.
        /// 
        /// Example:
        /// <code>
        /// context.Metadata["UserId"] = currentUser.Id;
        /// context.Metadata["OperationType"] = "BulkUpdate";
        /// </code>
        /// </remarks>
        public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
