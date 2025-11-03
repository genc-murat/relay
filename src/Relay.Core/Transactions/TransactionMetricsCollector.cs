using System;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Collects and aggregates metrics for transaction operations.
    /// </summary>
    /// <remarks>
    /// This class provides thread-safe metrics collection for transaction operations.
    /// It tracks transaction counts, durations, success/failure rates, and operation-specific metrics.
    /// All operations are thread-safe and use lock-free concurrent collections for high performance.
    /// 
    /// Example usage:
    /// <code>
    /// var collector = new TransactionMetricsCollector();
    /// 
    /// // Record transaction start
    /// var stopwatch = Stopwatch.StartNew();
    /// 
    /// try
    /// {
    ///     // Execute transaction
    ///     collector.RecordTransactionSuccess(IsolationLevel.ReadCommitted, "CreateOrderCommand", stopwatch.Elapsed);
    /// }
    /// catch (TransactionTimeoutException)
    /// {
    ///     collector.RecordTransactionTimeout("CreateOrderCommand", stopwatch.Elapsed);
    /// }
    /// catch (Exception)
    /// {
    ///     collector.RecordTransactionFailure("CreateOrderCommand", stopwatch.Elapsed);
    /// }
    /// 
    /// // Get current metrics
    /// var metrics = collector.GetMetrics();
    /// Console.WriteLine($"Success Rate: {metrics.SuccessRate:P}");
    /// </code>
    /// </remarks>
    public class TransactionMetricsCollector
    {
        private long _totalTransactions;
        private long _successfulTransactions;
        private long _failedTransactions;
        private long _rolledBackTransactions;
        private long _timeoutTransactions;
        private long _totalDurationTicks;
        
        private readonly ConcurrentDictionary<string, long> _transactionsByIsolationLevel = new();
        private readonly ConcurrentDictionary<string, long> _transactionsByRequestType = new();
        private readonly ConcurrentDictionary<string, long> _savepointOperations = new();

        /// <summary>
        /// Records a successful transaction completion.
        /// </summary>
        /// <param name="isolationLevel">The isolation level used for the transaction.</param>
        /// <param name="requestType">The type name of the request being processed.</param>
        /// <param name="duration">The duration of the transaction.</param>
        /// <remarks>
        /// This method is thread-safe and can be called concurrently from multiple threads.
        /// </remarks>
        public void RecordTransactionSuccess(IsolationLevel isolationLevel, string requestType, TimeSpan duration)
        {
            System.Threading.Interlocked.Increment(ref _totalTransactions);
            System.Threading.Interlocked.Increment(ref _successfulTransactions);
            System.Threading.Interlocked.Add(ref _totalDurationTicks, duration.Ticks);
            
            _transactionsByIsolationLevel.AddOrUpdate(isolationLevel.ToString(), 1, (_, count) => count + 1);
            _transactionsByRequestType.AddOrUpdate(requestType, 1, (_, count) => count + 1);
        }

        /// <summary>
        /// Records a failed transaction.
        /// </summary>
        /// <param name="requestType">The type name of the request being processed.</param>
        /// <param name="duration">The duration before the transaction failed.</param>
        /// <remarks>
        /// This method is thread-safe and can be called concurrently from multiple threads.
        /// Failed transactions are those that threw an exception during execution.
        /// </remarks>
        public void RecordTransactionFailure(string requestType, TimeSpan duration)
        {
            System.Threading.Interlocked.Increment(ref _totalTransactions);
            System.Threading.Interlocked.Increment(ref _failedTransactions);
            System.Threading.Interlocked.Add(ref _totalDurationTicks, duration.Ticks);
            
            _transactionsByRequestType.AddOrUpdate(requestType, 1, (_, count) => count + 1);
        }

        /// <summary>
        /// Records an explicitly rolled back transaction.
        /// </summary>
        /// <param name="requestType">The type name of the request being processed.</param>
        /// <param name="duration">The duration before the transaction was rolled back.</param>
        /// <remarks>
        /// This method is thread-safe and can be called concurrently from multiple threads.
        /// Rolled back transactions are those that were explicitly rolled back due to business logic,
        /// validation failures, or event handler failures.
        /// </remarks>
        public void RecordTransactionRollback(string requestType, TimeSpan duration)
        {
            System.Threading.Interlocked.Increment(ref _totalTransactions);
            System.Threading.Interlocked.Increment(ref _rolledBackTransactions);
            System.Threading.Interlocked.Add(ref _totalDurationTicks, duration.Ticks);
            
            _transactionsByRequestType.AddOrUpdate(requestType, 1, (_, count) => count + 1);
        }

        /// <summary>
        /// Records a transaction timeout.
        /// </summary>
        /// <param name="requestType">The type name of the request being processed.</param>
        /// <param name="duration">The duration before the transaction timed out.</param>
        /// <remarks>
        /// This method is thread-safe and can be called concurrently from multiple threads.
        /// Timeout transactions are those that exceeded their configured timeout duration.
        /// </remarks>
        public void RecordTransactionTimeout(string requestType, TimeSpan duration)
        {
            System.Threading.Interlocked.Increment(ref _totalTransactions);
            System.Threading.Interlocked.Increment(ref _timeoutTransactions);
            System.Threading.Interlocked.Add(ref _totalDurationTicks, duration.Ticks);
            
            _transactionsByRequestType.AddOrUpdate(requestType, 1, (_, count) => count + 1);
        }

        /// <summary>
        /// Records a savepoint creation operation.
        /// </summary>
        /// <param name="savepointName">The name of the savepoint created.</param>
        /// <remarks>
        /// This method is thread-safe and can be called concurrently from multiple threads.
        /// </remarks>
        public void RecordSavepointCreated(string savepointName)
        {
            _savepointOperations.AddOrUpdate("Created", 1, (_, count) => count + 1);
        }

        /// <summary>
        /// Records a savepoint rollback operation.
        /// </summary>
        /// <param name="savepointName">The name of the savepoint rolled back to.</param>
        /// <remarks>
        /// This method is thread-safe and can be called concurrently from multiple threads.
        /// </remarks>
        public void RecordSavepointRolledBack(string savepointName)
        {
            _savepointOperations.AddOrUpdate("RolledBack", 1, (_, count) => count + 1);
        }

        /// <summary>
        /// Records a savepoint release operation.
        /// </summary>
        /// <param name="savepointName">The name of the savepoint released.</param>
        /// <remarks>
        /// This method is thread-safe and can be called concurrently from multiple threads.
        /// </remarks>
        public void RecordSavepointReleased(string savepointName)
        {
            _savepointOperations.AddOrUpdate("Released", 1, (_, count) => count + 1);
        }

        /// <summary>
        /// Gets the current transaction metrics snapshot.
        /// </summary>
        /// <returns>A snapshot of the current transaction metrics.</returns>
        /// <remarks>
        /// This method returns a point-in-time snapshot of the metrics.
        /// The returned object is immutable and will not reflect subsequent metric updates.
        /// </remarks>
        public TransactionMetrics GetMetrics()
        {
            var totalTransactions = System.Threading.Interlocked.Read(ref _totalTransactions);
            var totalDurationTicks = System.Threading.Interlocked.Read(ref _totalDurationTicks);
            
            return new TransactionMetrics
            {
                TotalTransactions = totalTransactions,
                SuccessfulTransactions = System.Threading.Interlocked.Read(ref _successfulTransactions),
                FailedTransactions = System.Threading.Interlocked.Read(ref _failedTransactions),
                RolledBackTransactions = System.Threading.Interlocked.Read(ref _rolledBackTransactions),
                TimeoutTransactions = System.Threading.Interlocked.Read(ref _timeoutTransactions),
                AverageDurationMs = totalTransactions > 0 
                    ? TimeSpan.FromTicks(totalDurationTicks / totalTransactions).TotalMilliseconds 
                    : 0,
                TransactionsByIsolationLevel = _transactionsByIsolationLevel.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                SavepointOperations = _savepointOperations.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }

        /// <summary>
        /// Gets transaction counts grouped by request type.
        /// </summary>
        /// <returns>A dictionary mapping request type names to transaction counts.</returns>
        /// <remarks>
        /// This method returns a snapshot of the current request type statistics.
        /// Useful for identifying which request types generate the most transactions.
        /// </remarks>
        public System.Collections.Generic.Dictionary<string, long> GetTransactionsByRequestType()
        {
            return _transactionsByRequestType.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Resets all collected metrics to zero.
        /// </summary>
        /// <remarks>
        /// This method is thread-safe but should be used with caution as it will clear all historical data.
        /// Typically used for testing or when implementing metric rotation strategies.
        /// </remarks>
        public void Reset()
        {
            System.Threading.Interlocked.Exchange(ref _totalTransactions, 0);
            System.Threading.Interlocked.Exchange(ref _successfulTransactions, 0);
            System.Threading.Interlocked.Exchange(ref _failedTransactions, 0);
            System.Threading.Interlocked.Exchange(ref _rolledBackTransactions, 0);
            System.Threading.Interlocked.Exchange(ref _timeoutTransactions, 0);
            System.Threading.Interlocked.Exchange(ref _totalDurationTicks, 0);
            
            _transactionsByIsolationLevel.Clear();
            _transactionsByRequestType.Clear();
            _savepointOperations.Clear();
        }
    }
}
