using System;
using System.Data;
using System.Diagnostics;

namespace Relay.Core.Transactions;

/// <summary>
/// Provides distributed tracing support for transaction operations using OpenTelemetry.
/// </summary>
public interface ITransactionActivitySource
{
    /// <summary>
    /// Starts a new activity (span) for a transaction operation.
    /// </summary>
    /// <param name="transactionId">The unique identifier for the transaction.</param>
    /// <param name="requestType">The type name of the request being processed.</param>
    /// <param name="isolationLevel">The isolation level of the transaction.</param>
    /// <param name="nestingLevel">The nesting level of the transaction (0 for outermost).</param>
    /// <param name="isReadOnly">Whether the transaction is read-only.</param>
    /// <param name="timeoutSeconds">The timeout duration in seconds, or null if no timeout.</param>
    /// <returns>An Activity instance if tracing is enabled, otherwise null.</returns>
    Activity? StartTransactionActivity(
        string transactionId,
        string requestType,
        IsolationLevel isolationLevel,
        int nestingLevel,
        bool isReadOnly,
        int? timeoutSeconds = null);

    /// <summary>
    /// Starts a new activity (span) for a savepoint operation.
    /// </summary>
    /// <param name="transactionId">The unique identifier for the transaction.</param>
    /// <param name="savepointName">The name of the savepoint.</param>
    /// <param name="operation">The savepoint operation (Create, Rollback, Release).</param>
    /// <returns>An Activity instance if tracing is enabled, otherwise null.</returns>
    Activity? StartSavepointActivity(
        string transactionId,
        string savepointName,
        string operation);

    /// <summary>
    /// Starts a new activity (span) for a transaction retry operation.
    /// </summary>
    /// <param name="transactionId">The unique identifier for the transaction.</param>
    /// <param name="requestType">The type name of the request being processed.</param>
    /// <param name="attemptNumber">The current retry attempt number (1-based).</param>
    /// <param name="maxRetries">The maximum number of retry attempts.</param>
    /// <param name="delayMs">The delay before this retry attempt in milliseconds.</param>
    /// <returns>An Activity instance if tracing is enabled, otherwise null.</returns>
    Activity? StartRetryActivity(
        string transactionId,
        string requestType,
        int attemptNumber,
        int maxRetries,
        int delayMs);

    /// <summary>
    /// Adds an event to the current activity indicating a transaction lifecycle event.
    /// </summary>
    /// <param name="activity">The activity to add the event to.</param>
    /// <param name="eventName">The name of the event (e.g., "BeforeCommit", "AfterRollback").</param>
    /// <param name="timestamp">The timestamp when the event occurred.</param>
    void AddTransactionEvent(Activity? activity, string eventName, DateTime timestamp);

    /// <summary>
    /// Sets the status of an activity based on transaction outcome.
    /// </summary>
    /// <param name="activity">The activity to set the status on.</param>
    /// <param name="success">Whether the transaction completed successfully.</param>
    /// <param name="errorMessage">The error message if the transaction failed.</param>
    void SetTransactionStatus(Activity? activity, bool success, string? errorMessage = null);

    /// <summary>
    /// Records an exception in the current activity.
    /// </summary>
    /// <param name="activity">The activity to record the exception in.</param>
    /// <param name="exception">The exception that occurred.</param>
    void RecordException(Activity? activity, Exception? exception);

    /// <summary>
    /// Starts a transaction activity from configuration.
    /// </summary>
    Activity? StartTransactionActivity(string requestType, ITransactionConfiguration configuration);

    /// <summary>
    /// Records transaction success.
    /// </summary>
    void RecordTransactionSuccess(Activity? activity, ITransactionContext context, TimeSpan duration);

    /// <summary>
    /// Records transaction timeout.
    /// </summary>
    void RecordTransactionTimeout(Activity? activity, ITransactionContext context, Exception exception);

    /// <summary>
    /// Records transaction rollback.
    /// </summary>
    void RecordTransactionRollback(Activity? activity, ITransactionContext context, Exception exception);

    /// <summary>
    /// Records transaction failure.
    /// </summary>
    void RecordTransactionFailure(Activity? activity, ITransactionContext context, Exception exception);
}