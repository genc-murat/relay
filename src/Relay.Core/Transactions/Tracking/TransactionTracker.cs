using System;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Transactions;

namespace Relay.Core.Transactions.Tracking
{
    /// <summary>
    /// Handles transaction metrics and activity tracking in a centralized manner.
    /// </summary>
    public class TransactionTracker
    {
        private readonly TransactionMetricsCollector _metricsCollector;
        private readonly TransactionActivitySource _activitySource;
        private readonly TransactionLogger _transactionLogger;
        private readonly ILogger<TransactionTracker> _logger;

        public TransactionTracker(
            TransactionMetricsCollector metricsCollector,
            TransactionActivitySource activitySource,
            TransactionLogger transactionLogger,
            ILogger<TransactionTracker> logger)
        {
            _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
            _transactionLogger = transactionLogger ?? throw new ArgumentNullException(nameof(transactionLogger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Records successful transaction metrics and activity.
        /// </summary>
        public void RecordSuccess(
            IsolationLevel isolationLevel,
            string requestType,
            Stopwatch stopwatch,
            System.Diagnostics.Activity activity,
            ITransactionContext context)
        {
            stopwatch.Stop();
            _metricsCollector.RecordTransactionSuccess(isolationLevel, requestType, stopwatch.Elapsed);
            _activitySource.RecordTransactionSuccess(activity, context, stopwatch.Elapsed);
        }

        /// <summary>
        /// Records successful distributed transaction metrics.
        /// </summary>
        public void RecordDistributedSuccess(
            IsolationLevel isolationLevel,
            string requestType,
            Stopwatch stopwatch,
            string transactionId)
        {
            stopwatch.Stop();
            _metricsCollector.RecordTransactionSuccess(isolationLevel, requestType, stopwatch.Elapsed);
            _transactionLogger.LogDistributedTransactionCommitted(transactionId, requestType, isolationLevel);
        }

        /// <summary>
        /// Records transaction timeout metrics and activity.
        /// </summary>
        public void RecordTimeout(
            string requestType,
            Stopwatch stopwatch,
            System.Diagnostics.Activity activity,
            ITransactionContext context,
            TransactionTimeoutException exception)
        {
            stopwatch.Stop();
            _metricsCollector.RecordTransactionTimeout(requestType, stopwatch.Elapsed);
            _activitySource.RecordTransactionTimeout(activity, context, exception);
        }

        /// <summary>
        /// Records transaction rollback metrics and activity.
        /// </summary>
        public void RecordRollback(
            string requestType,
            Stopwatch stopwatch,
            System.Diagnostics.Activity activity,
            ITransactionContext context,
            Exception exception)
        {
            stopwatch.Stop();
            _metricsCollector.RecordTransactionRollback(requestType, stopwatch.Elapsed);
            _activitySource.RecordTransactionRollback(activity, context, exception);
        }

        /// <summary>
        /// Records transaction failure metrics and activity.
        /// </summary>
        public void RecordFailure(
            string requestType,
            Stopwatch stopwatch,
            System.Diagnostics.Activity activity,
            ITransactionContext context,
            Exception exception)
        {
            stopwatch.Stop();
            _metricsCollector.RecordTransactionFailure(requestType, stopwatch.Elapsed);
            _activitySource.RecordTransactionFailure(activity, context, exception);
        }

        /// <summary>
        /// Records distributed transaction rollback metrics.
        /// </summary>
        public void RecordDistributedRollback(
            string requestType,
            Stopwatch stopwatch,
            Exception exception)
        {
            stopwatch.Stop();
            _metricsCollector.RecordTransactionRollback(requestType, stopwatch.Elapsed);
        }

        /// <summary>
        /// Records distributed transaction failure metrics.
        /// </summary>
        public void RecordDistributedFailure(
            string requestType,
            Stopwatch stopwatch,
            string transactionId,
            string requestTypeName,
            IsolationLevel isolationLevel,
            Exception exception)
        {
            stopwatch.Stop();
            _metricsCollector.RecordTransactionFailure(requestType, stopwatch.Elapsed);

            _logger.LogError(exception,
                "Distributed transaction {TransactionId} for {RequestName} with isolation level {IsolationLevel} failed. Rolling back.",
                transactionId,
                requestTypeName,
                isolationLevel);
        }

        /// <summary>
        /// Logs distributed transaction creation.
        /// </summary>
        public void LogDistributedTransactionCreated(
            string transactionId,
            string requestType,
            IsolationLevel isolationLevel)
        {
            _transactionLogger.LogDistributedTransactionCreated(transactionId, requestType, isolationLevel);
        }

        /// <summary>
        /// Logs distributed transaction before commit warning.
        /// </summary>
        public void LogDistributedBeforeCommitWarning(
            TransactionEventHandlerException exception,
            string transactionId,
            string requestType)
        {
            _logger.LogWarning(exception,
                "Distributed transaction {TransactionId} for {RequestName} rolling back due to BeforeCommit event handler failure",
                transactionId,
                requestType);
        }
    }
}