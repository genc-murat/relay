using System;
using System.Data;
using Microsoft.Extensions.Logging;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Provides structured logging for transaction operations.
    /// </summary>
    /// <remarks>
    /// This class encapsulates all transaction-related logging with structured context.
    /// It uses high-performance logging patterns with compile-time source generation
    /// to minimize allocations and improve performance.
    /// 
    /// All log entries include transaction context (ID, isolation level, nesting level)
    /// for correlation and filtering in log aggregation systems.
    /// 
    /// Example usage:
    /// <code>
    /// var logger = serviceProvider.GetRequiredService&lt;ILogger&lt;TransactionLogger&gt;&gt;();
    /// var transactionLogger = new TransactionLogger(logger);
    /// 
    /// transactionLogger.LogTransactionBegin(
    ///     transactionId: "tx-123",
    ///     requestType: "CreateOrderCommand",
    ///     isolationLevel: IsolationLevel.ReadCommitted,
    ///     nestingLevel: 0,
    ///     timeoutSeconds: 30
    /// );
    /// </code>
    /// </remarks>
    public partial class TransactionLogger
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionLogger"/> class.
        /// </summary>
        /// <param name="logger">The logger instance to use for writing log entries.</param>
        public TransactionLogger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Logs the beginning of a transaction.
        /// </summary>
        [LoggerMessage(
            EventId = 1001,
            Level = LogLevel.Debug,
            Message = "Transaction {TransactionId} beginning for {RequestType} with isolation level {IsolationLevel}, nesting level {NestingLevel}, timeout {TimeoutSeconds}s")]
        public partial void LogTransactionBegin(
            string transactionId,
            string requestType,
            IsolationLevel isolationLevel,
            int nestingLevel,
            int? timeoutSeconds);

        /// <summary>
        /// Logs successful transaction commit.
        /// </summary>
        [LoggerMessage(
            EventId = 1002,
            Level = LogLevel.Information,
            Message = "Transaction {TransactionId} committed successfully for {RequestType} in {DurationMs}ms")]
        public partial void LogTransactionCommit(
            string transactionId,
            string requestType,
            double durationMs);

        /// <summary>
        /// Logs transaction rollback.
        /// </summary>
        [LoggerMessage(
            EventId = 1003,
            Level = LogLevel.Warning,
            Message = "Transaction {TransactionId} rolled back for {RequestType} after {DurationMs}ms")]
        public partial void LogTransactionRollback(
            string transactionId,
            string requestType,
            double durationMs);

        /// <summary>
        /// Logs transaction failure.
        /// </summary>
        [LoggerMessage(
            EventId = 1004,
            Level = LogLevel.Error,
            Message = "Transaction {TransactionId} failed for {RequestType} after {DurationMs}ms")]
        public partial void LogTransactionFailure(
            string transactionId,
            string requestType,
            double durationMs,
            Exception exception);

        /// <summary>
        /// Logs transaction timeout.
        /// </summary>
        [LoggerMessage(
            EventId = 1005,
            Level = LogLevel.Error,
            Message = "Transaction {TransactionId} timed out for {RequestType} after {DurationMs}ms (timeout: {TimeoutSeconds}s)")]
        public partial void LogTransactionTimeout(
            string transactionId,
            string requestType,
            double durationMs,
            int timeoutSeconds);

        /// <summary>
        /// Logs savepoint creation.
        /// </summary>
        [LoggerMessage(
            EventId = 1006,
            Level = LogLevel.Debug,
            Message = "Savepoint {SavepointName} created in transaction {TransactionId}")]
        public partial void LogSavepointCreated(
            string transactionId,
            string savepointName);

        /// <summary>
        /// Logs savepoint rollback.
        /// </summary>
        [LoggerMessage(
            EventId = 1007,
            Level = LogLevel.Warning,
            Message = "Rolled back to savepoint {SavepointName} in transaction {TransactionId}")]
        public partial void LogSavepointRolledBack(
            string transactionId,
            string savepointName);

        /// <summary>
        /// Logs savepoint release.
        /// </summary>
        [LoggerMessage(
            EventId = 1008,
            Level = LogLevel.Debug,
            Message = "Savepoint {SavepointName} released in transaction {TransactionId}")]
        public partial void LogSavepointReleased(
            string transactionId,
            string savepointName);

        /// <summary>
        /// Logs nested transaction detection.
        /// </summary>
        [LoggerMessage(
            EventId = 1009,
            Level = LogLevel.Debug,
            Message = "Nested transaction detected for {RequestType} in transaction {TransactionId}, reusing existing transaction (nesting level: {NestingLevel})")]
        public partial void LogNestedTransactionDetected(
            string transactionId,
            string requestType,
            int nestingLevel);

        /// <summary>
        /// Logs transaction retry attempt.
        /// </summary>
        [LoggerMessage(
            EventId = 1010,
            Level = LogLevel.Warning,
            Message = "Retrying transaction for {RequestType} (attempt {AttemptNumber}/{MaxRetries}) after {DelayMs}ms delay")]
        public partial void LogTransactionRetry(
            string requestType,
            int attemptNumber,
            int maxRetries,
            int delayMs);

        /// <summary>
        /// Logs transaction retry exhaustion.
        /// </summary>
        [LoggerMessage(
            EventId = 1011,
            Level = LogLevel.Error,
            Message = "Transaction retry exhausted for {RequestType} after {MaxRetries} attempts")]
        public partial void LogTransactionRetryExhausted(
            string requestType,
            int maxRetries,
            Exception exception);

        /// <summary>
        /// Logs read-only transaction violation.
        /// </summary>
        [LoggerMessage(
            EventId = 1012,
            Level = LogLevel.Error,
            Message = "Read-only transaction violation detected in transaction {TransactionId} for {RequestType}")]
        public partial void LogReadOnlyViolation(
            string transactionId,
            string requestType);

        /// <summary>
        /// Logs distributed transaction coordination.
        /// </summary>
        [LoggerMessage(
            EventId = 1013,
            Level = LogLevel.Information,
            Message = "Distributed transaction coordinated for {RequestType} in transaction {TransactionId}")]
        public partial void LogDistributedTransaction(
            string transactionId,
            string requestType);

        /// <summary>
        /// Logs transaction event handler execution.
        /// </summary>
        [LoggerMessage(
            EventId = 1014,
            Level = LogLevel.Debug,
            Message = "Executing {EventName} event handlers for transaction {TransactionId} ({HandlerCount} handlers)")]
        public partial void LogEventHandlerExecution(
            string transactionId,
            string eventName,
            int handlerCount);

        /// <summary>
        /// Logs transaction event handler failure.
        /// </summary>
        [LoggerMessage(
            EventId = 1015,
            Level = LogLevel.Error,
            Message = "Event handler failed during {EventName} for transaction {TransactionId}")]
        public partial void LogEventHandlerFailure(
            string transactionId,
            string eventName,
            Exception exception);

        /// <summary>
        /// Logs transaction context suppression.
        /// </summary>
        [LoggerMessage(
            EventId = 1016,
            Level = LogLevel.Debug,
            Message = "Transaction context suppressed for operation in transaction {TransactionId}")]
        public partial void LogTransactionContextSuppressed(
            string transactionId);

        /// <summary>
        /// Logs transaction configuration resolution.
        /// </summary>
        [LoggerMessage(
            EventId = 1017,
            Level = LogLevel.Debug,
            Message = "Resolved transaction configuration for {RequestType}: IsolationLevel={IsolationLevel}, Timeout={TimeoutSeconds}s, ReadOnly={IsReadOnly}, Distributed={UseDistributed}")]
        public partial void LogConfigurationResolved(
            string requestType,
            IsolationLevel isolationLevel,
            int? timeoutSeconds,
            bool isReadOnly,
            bool useDistributed);

        /// <summary>
        /// Logs missing transaction attribute error.
        /// </summary>
        [LoggerMessage(
            EventId = 1018,
            Level = LogLevel.Error,
            Message = "TransactionAttribute is required but missing on {RequestType}. All ITransactionalRequest implementations must have [Transaction(IsolationLevel)] attribute.")]
        public partial void LogMissingTransactionAttribute(
            string requestType);

        /// <summary>
        /// Logs unspecified isolation level error.
        /// </summary>
        [LoggerMessage(
            EventId = 1019,
            Level = LogLevel.Error,
            Message = "IsolationLevel.Unspecified is not allowed for {RequestType}. Explicit isolation level must be specified in TransactionAttribute.")]
        public partial void LogUnspecifiedIsolationLevel(
            string requestType);

        /// <summary>
        /// Logs saving changes operation.
        /// </summary>
        public void LogSavingChanges(string transactionId, string requestType, bool isNested)
        {
            if (isNested)
            {
                _logger.LogInformation(
                    "Saving changes for nested transaction {RequestType} in transaction {TransactionId}",
                    requestType,
                    transactionId);
            }
            else
            {
                _logger.LogInformation(
                    "Saving changes for transaction {TransactionId} in {RequestType}",
                    transactionId,
                    requestType);
            }
        }

        /// <summary>
        /// Logs nested transaction completed.
        /// </summary>
        public void LogNestedTransactionCompleted(string requestType, string transactionId)
        {
            _logger.LogInformation(
                "Nested transaction {RequestType} completed successfully. Transaction {TransactionId} will be committed by outermost transaction.",
                requestType,
                transactionId);
        }

        /// <summary>
        /// Logs distributed transaction created.
        /// </summary>
        public void LogDistributedTransactionCreated(string transactionId, string requestType, IsolationLevel isolationLevel)
        {
            _logger.LogInformation(
                "Distributed transaction {TransactionId} created for {RequestType} with isolation level {IsolationLevel}",
                transactionId,
                requestType,
                isolationLevel);
        }

        /// <summary>
        /// Logs distributed transaction committed.
        /// </summary>
        public void LogDistributedTransactionCommitted(string transactionId, string requestType, IsolationLevel isolationLevel)
        {
            _logger.LogInformation(
                "Distributed transaction {TransactionId} committed for {RequestType} with isolation level {IsolationLevel}",
                transactionId,
                requestType,
                isolationLevel);
        }
    }
}
