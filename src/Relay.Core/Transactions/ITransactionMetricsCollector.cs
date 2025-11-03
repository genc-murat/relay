using System.Collections.Generic;
using System.Data;
using System;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Interface for collecting and aggregating metrics for transaction operations.
    /// </summary>
    public interface ITransactionMetricsCollector
    {
        /// <summary>
        /// Records a successful transaction completion.
        /// </summary>
        /// <param name="isolationLevel">The isolation level used for the transaction.</param>
        /// <param name="requestType">The type name of the request being processed.</param>
        /// <param name="duration">The duration of the transaction.</param>
        void RecordTransactionSuccess(IsolationLevel isolationLevel, string requestType, TimeSpan duration);

        /// <summary>
        /// Records a failed transaction.
        /// </summary>
        /// <param name="requestType">The type name of the request being processed.</param>
        /// <param name="duration">The duration before the transaction failed.</param>
        void RecordTransactionFailure(string requestType, TimeSpan duration);

        /// <summary>
        /// Records an explicitly rolled back transaction.
        /// </summary>
        /// <param name="requestType">The type name of the request being processed.</param>
        /// <param name="duration">The duration before the transaction was rolled back.</param>
        void RecordTransactionRollback(string requestType, TimeSpan duration);

        /// <summary>
        /// Records a transaction timeout.
        /// </summary>
        /// <param name="requestType">The type name of the request being processed.</param>
        /// <param name="duration">The duration before the transaction timed out.</param>
        void RecordTransactionTimeout(string requestType, TimeSpan duration);

        /// <summary>
        /// Records a savepoint creation operation.
        /// </summary>
        /// <param name="savepointName">The name of the savepoint created.</param>
        void RecordSavepointCreated(string savepointName);

        /// <summary>
        /// Records a savepoint rollback operation.
        /// </summary>
        /// <param name="savepointName">The name of the savepoint rolled back to.</param>
        void RecordSavepointRolledBack(string savepointName);

        /// <summary>
        /// Records a savepoint release operation.
        /// </summary>
        /// <param name="savepointName">The name of the savepoint released.</param>
        void RecordSavepointReleased(string savepointName);

        /// <summary>
        /// Gets the current transaction metrics snapshot.
        /// </summary>
        /// <returns>A snapshot of the current transaction metrics.</returns>
        TransactionMetrics GetMetrics();

        /// <summary>
        /// Gets transaction counts grouped by request type.
        /// </summary>
        /// <returns>A dictionary mapping request type names to transaction counts.</returns>
        Dictionary<string, long> GetTransactionsByRequestType();

        /// <summary>
        /// Resets all collected metrics to zero.
        /// </summary>
        void Reset();
    }
}