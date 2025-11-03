using System.Collections.Generic;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Represents collected metrics for transaction operations.
    /// </summary>
    /// <remarks>
    /// This class aggregates transaction statistics for monitoring and observability purposes.
    /// It tracks transaction counts, success/failure rates, durations, and operation-specific metrics.
    /// These metrics are typically exposed through health check endpoints or exported to monitoring systems.
    /// 
    /// Example usage:
    /// <code>
    /// var metrics = metricsCollector.GetMetrics();
    /// Console.WriteLine($"Success Rate: {metrics.SuccessRate:P}");
    /// Console.WriteLine($"Average Duration: {metrics.AverageDurationMs}ms");
    /// Console.WriteLine($"Timeout Rate: {metrics.TimeoutRate:P}");
    /// </code>
    /// </remarks>
    public class TransactionMetrics
    {
        /// <summary>
        /// Gets or sets the total number of transactions started.
        /// </summary>
        /// <remarks>
        /// This includes all transactions regardless of outcome (successful, failed, rolled back, timed out).
        /// </remarks>
        public long TotalTransactions { get; set; }

        /// <summary>
        /// Gets or sets the number of successfully committed transactions.
        /// </summary>
        /// <remarks>
        /// A transaction is considered successful when it commits without errors.
        /// </remarks>
        public long SuccessfulTransactions { get; set; }

        /// <summary>
        /// Gets or sets the number of failed transactions.
        /// </summary>
        /// <remarks>
        /// This includes transactions that failed due to exceptions during execution,
        /// but excludes transactions that were explicitly rolled back or timed out.
        /// </remarks>
        public long FailedTransactions { get; set; }

        /// <summary>
        /// Gets or sets the number of explicitly rolled back transactions.
        /// </summary>
        /// <remarks>
        /// This includes transactions that were rolled back due to business logic decisions,
        /// validation failures, or event handler failures during BeforeCommit.
        /// </remarks>
        public long RolledBackTransactions { get; set; }

        /// <summary>
        /// Gets or sets the number of transactions that exceeded their timeout.
        /// </summary>
        /// <remarks>
        /// Timeout transactions are automatically rolled back and throw a TransactionTimeoutException.
        /// High timeout rates may indicate performance issues or inappropriately short timeout values.
        /// </remarks>
        public long TimeoutTransactions { get; set; }

        /// <summary>
        /// Gets or sets the average duration of transactions in milliseconds.
        /// </summary>
        /// <remarks>
        /// This is calculated as the mean duration of all completed transactions.
        /// Useful for identifying performance trends and detecting slow transactions.
        /// </remarks>
        public double AverageDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the transaction counts grouped by isolation level.
        /// </summary>
        /// <remarks>
        /// The dictionary key is the isolation level name (e.g., "ReadCommitted", "Serializable"),
        /// and the value is the count of transactions using that isolation level.
        /// 
        /// Example:
        /// <code>
        /// {
        ///     "ReadCommitted": 1500,
        ///     "Serializable": 250,
        ///     "Snapshot": 100
        /// }
        /// </code>
        /// </remarks>
        public Dictionary<string, long> TransactionsByIsolationLevel { get; set; } = new();

        /// <summary>
        /// Gets or sets the counts of savepoint operations.
        /// </summary>
        /// <remarks>
        /// The dictionary key is the operation type (e.g., "Created", "RolledBack", "Released"),
        /// and the value is the count of that operation.
        /// 
        /// Example:
        /// <code>
        /// {
        ///     "Created": 500,
        ///     "RolledBack": 50,
        ///     "Released": 450
        /// }
        /// </code>
        /// 
        /// This helps track savepoint usage patterns and identify scenarios where partial rollbacks are common.
        /// </remarks>
        public Dictionary<string, long> SavepointOperations { get; set; } = new();

        /// <summary>
        /// Gets the success rate as a value between 0 and 1.
        /// </summary>
        /// <remarks>
        /// Calculated as SuccessfulTransactions / TotalTransactions.
        /// Returns 0 if no transactions have been executed.
        /// </remarks>
        public double SuccessRate => TotalTransactions > 0 
            ? (double)SuccessfulTransactions / TotalTransactions 
            : 0;

        /// <summary>
        /// Gets the failure rate as a value between 0 and 1.
        /// </summary>
        /// <remarks>
        /// Calculated as FailedTransactions / TotalTransactions.
        /// Returns 0 if no transactions have been executed.
        /// </remarks>
        public double FailureRate => TotalTransactions > 0 
            ? (double)FailedTransactions / TotalTransactions 
            : 0;

        /// <summary>
        /// Gets the timeout rate as a value between 0 and 1.
        /// </summary>
        /// <remarks>
        /// Calculated as TimeoutTransactions / TotalTransactions.
        /// Returns 0 if no transactions have been executed.
        /// High timeout rates may indicate performance issues.
        /// </remarks>
        public double TimeoutRate => TotalTransactions > 0 
            ? (double)TimeoutTransactions / TotalTransactions 
            : 0;
    }
}
