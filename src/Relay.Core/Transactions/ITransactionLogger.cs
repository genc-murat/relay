using System;
using System.Data;
using Microsoft.Extensions.Logging;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Provides structured logging for transaction operations.
    /// </summary>
    public interface ITransactionLogger
    {
        /// <summary>
        /// Logs the beginning of a transaction.
        /// </summary>
        void LogTransactionBegin(
            string transactionId,
            string requestType,
            IsolationLevel isolationLevel,
            int nestingLevel,
            int? timeoutSeconds);

        /// <summary>
        /// Logs successful transaction commit.
        /// </summary>
        void LogTransactionCommit(
            string transactionId,
            string requestType,
            double durationMs);

        /// <summary>
        /// Logs transaction rollback.
        /// </summary>
        void LogTransactionRollback(
            string transactionId,
            string requestType,
            double durationMs);

        /// <summary>
        /// Logs transaction failure.
        /// </summary>
        void LogTransactionFailure(
            string transactionId,
            string requestType,
            double durationMs,
            Exception exception);

        /// <summary>
        /// Logs transaction timeout.
        /// </summary>
        void LogTransactionTimeout(
            string transactionId,
            string requestType,
            double durationMs,
            int timeoutSeconds);

        /// <summary>
        /// Logs savepoint creation.
        /// </summary>
        void LogSavepointCreated(
            string transactionId,
            string savepointName);

        /// <summary>
        /// Logs savepoint rollback.
        /// </summary>
        void LogSavepointRolledBack(
            string transactionId,
            string savepointName);

        /// <summary>
        /// Logs savepoint release.
        /// </summary>
        void LogSavepointReleased(
            string transactionId,
            string savepointName);

        /// <summary>
        /// Logs nested transaction detection.
        /// </summary>
        void LogNestedTransactionDetected(
            string transactionId,
            string requestType,
            int nestingLevel);

        /// <summary>
        /// Logs transaction retry attempt.
        /// </summary>
        void LogTransactionRetry(
            string requestType,
            int attemptNumber,
            int maxRetries,
            int delayMs);

        /// <summary>
        /// Logs transaction retry exhaustion.
        /// </summary>
        void LogTransactionRetryExhausted(
            string requestType,
            int maxRetries,
            Exception exception);

        /// <summary>
        /// Logs read-only transaction violation.
        /// </summary>
        void LogReadOnlyViolation(
            string transactionId,
            string requestType);

        /// <summary>
        /// Logs distributed transaction coordination.
        /// </summary>
        void LogDistributedTransaction(
            string transactionId,
            string requestType);

        /// <summary>
        /// Logs transaction event handler execution.
        /// </summary>
        void LogEventHandlerExecution(
            string transactionId,
            string eventName,
            int handlerCount);

        /// <summary>
        /// Logs transaction event handler failure.
        /// </summary>
        void LogEventHandlerFailure(
            string transactionId,
            string eventName,
            Exception exception);

        /// <summary>
        /// Logs transaction context suppression.
        /// </summary>
        void LogTransactionContextSuppressed(
            string transactionId);

        /// <summary>
        /// Logs transaction configuration resolution.
        /// </summary>
        void LogConfigurationResolved(
            string requestType,
            IsolationLevel isolationLevel,
            int? timeoutSeconds,
            bool isReadOnly,
            bool useDistributed);

        /// <summary>
        /// Logs missing transaction attribute error.
        /// </summary>
        void LogMissingTransactionAttribute(
            string requestType);

        /// <summary>
        /// Logs unspecified isolation level error.
        /// </summary>
        void LogUnspecifiedIsolationLevel(
            string requestType);

        /// <summary>
        /// Logs saving changes operation.
        /// </summary>
        void LogSavingChanges(string transactionId, string requestType, bool isNested);

        /// <summary>
        /// Logs nested transaction completed.
        /// </summary>
        void LogNestedTransactionCompleted(string requestType, string transactionId);

        /// <summary>
        /// Logs distributed transaction created.
        /// </summary>
        void LogDistributedTransactionCreated(string transactionId, string requestType, IsolationLevel isolationLevel);

        /// <summary>
        /// Logs distributed transaction committed.
        /// </summary>
        void LogDistributedTransactionCommitted(string transactionId, string requestType, IsolationLevel isolationLevel);
    }
}