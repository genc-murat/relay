using System;
using System.Data;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Represents the configuration for a transaction, including isolation level, timeout, and retry policy.
    /// </summary>
    /// <remarks>
    /// This interface defines the mandatory configuration properties that must be specified for every transaction.
    /// All properties are required and non-nullable to ensure explicit transaction behavior.
    /// </remarks>
    public interface ITransactionConfiguration
    {
        /// <summary>
        /// Gets the isolation level for the transaction.
        /// This property is mandatory and must be explicitly specified.
        /// </summary>
        /// <remarks>
        /// The isolation level determines the locking behavior and consistency guarantees for the transaction.
        /// Common values include:
        /// - ReadUncommitted: Allows dirty reads
        /// - ReadCommitted: Prevents dirty reads
        /// - RepeatableRead: Prevents dirty and non-repeatable reads
        /// - Serializable: Full isolation, prevents all anomalies
        /// - Snapshot: Uses row versioning for consistency
        /// </remarks>
        IsolationLevel IsolationLevel { get; }

        /// <summary>
        /// Gets the timeout duration for the transaction.
        /// This property is mandatory and must be explicitly specified.
        /// </summary>
        /// <remarks>
        /// The timeout prevents long-running transactions from blocking resources indefinitely.
        /// Use TimeSpan.Zero or Timeout.InfiniteTimeSpan to disable timeout enforcement.
        /// Default recommendation is 30 seconds for most operations.
        /// </remarks>
        TimeSpan Timeout { get; }

        /// <summary>
        /// Gets a value indicating whether this transaction is read-only.
        /// </summary>
        /// <remarks>
        /// Read-only transactions can be optimized by the database engine and prevent accidental data modifications.
        /// When true, any attempt to save changes will throw a ReadOnlyTransactionViolationException.
        /// </remarks>
        bool IsReadOnly { get; }

        /// <summary>
        /// Gets a value indicating whether this transaction should use distributed transaction coordination.
        /// </summary>
        /// <remarks>
        /// When true, the transaction will use TransactionScope for distributed transaction coordination
        /// across multiple databases or resources. This requires System.Transactions support.
        /// </remarks>
        bool UseDistributedTransaction { get; }

        /// <summary>
        /// Gets the retry policy for transient transaction failures.
        /// </summary>
        /// <remarks>
        /// When specified, the transaction system will automatically retry the operation on transient failures
        /// such as deadlocks or connection timeouts. Returns null if no retry policy is configured.
        /// </remarks>
        TransactionRetryPolicy? RetryPolicy { get; }
    }
}
