using System;
using System.Data;
using System.Diagnostics;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Provides distributed tracing support for transaction operations using OpenTelemetry.
    /// </summary>
    /// <remarks>
    /// This class creates and manages Activity (span) instances for transaction operations,
    /// enabling distributed tracing across service boundaries. It integrates with OpenTelemetry
    /// to provide observability into transaction lifecycle, performance, and behavior.
    /// 
    /// The ActivitySource follows OpenTelemetry semantic conventions for database operations
    /// and adds transaction-specific attributes for enhanced observability.
    /// 
    /// Example usage:
    /// <code>
    /// using var activity = activitySource.StartTransactionActivity(
    ///     transactionId: "tx-123",
    ///     requestType: "CreateOrderCommand",
    ///     isolationLevel: IsolationLevel.ReadCommitted,
    ///     nestingLevel: 0,
    ///     isReadOnly: false
    /// );
    /// 
    /// try
    /// {
    ///     // Execute transaction
    ///     activity?.SetStatus(ActivityStatusCode.Ok);
    /// }
    /// catch (Exception ex)
    /// {
    ///     activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    ///     throw;
    /// }
    /// </code>
    /// </remarks>
    public class TransactionActivitySource
    {
        /// <summary>
        /// The name of the ActivitySource for transaction operations.
        /// </summary>
        public const string SourceName = "Relay.Core.Transactions";

        /// <summary>
        /// The version of the ActivitySource.
        /// </summary>
        public const string SourceVersion = "1.0.0";

        private readonly ActivitySource _activitySource;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionActivitySource"/> class.
        /// </summary>
        public TransactionActivitySource()
        {
            _activitySource = new ActivitySource(SourceName, SourceVersion);
        }

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
        /// <remarks>
        /// The returned Activity should be disposed when the transaction completes.
        /// If no listeners are registered for this ActivitySource, this method returns null.
        /// 
        /// The activity includes the following tags:
        /// - transaction.id: Unique transaction identifier
        /// - transaction.request_type: Request type name
        /// - transaction.isolation_level: Database isolation level
        /// - transaction.nesting_level: Transaction nesting depth
        /// - transaction.is_readonly: Whether transaction is read-only
        /// - transaction.timeout_seconds: Timeout duration (if configured)
        /// </remarks>
        public Activity? StartTransactionActivity(
            string transactionId,
            string requestType,
            IsolationLevel isolationLevel,
            int nestingLevel,
            bool isReadOnly,
            int? timeoutSeconds = null)
        {
            var activity = _activitySource.StartActivity(
                name: "relay.transaction",
                kind: ActivityKind.Internal);

            if (activity != null)
            {
                activity.SetTag("transaction.id", transactionId);
                activity.SetTag("transaction.request_type", requestType);
                activity.SetTag("transaction.isolation_level", isolationLevel.ToString());
                activity.SetTag("transaction.nesting_level", nestingLevel);
                activity.SetTag("transaction.is_readonly", isReadOnly);
                
                if (timeoutSeconds.HasValue)
                {
                    activity.SetTag("transaction.timeout_seconds", timeoutSeconds.Value);
                }
            }

            return activity;
        }

        /// <summary>
        /// Starts a new activity (span) for a savepoint operation.
        /// </summary>
        /// <param name="transactionId">The unique identifier for the transaction.</param>
        /// <param name="savepointName">The name of the savepoint.</param>
        /// <param name="operation">The savepoint operation (Create, Rollback, Release).</param>
        /// <returns>An Activity instance if tracing is enabled, otherwise null.</returns>
        /// <remarks>
        /// The returned Activity should be disposed when the savepoint operation completes.
        /// 
        /// The activity includes the following tags:
        /// - transaction.id: Parent transaction identifier
        /// - savepoint.name: Savepoint name
        /// - savepoint.operation: Operation type (Create, Rollback, Release)
        /// </remarks>
        public Activity? StartSavepointActivity(
            string transactionId,
            string savepointName,
            string operation)
        {
            var activity = _activitySource.StartActivity(
                name: "relay.transaction.savepoint",
                kind: ActivityKind.Internal);

            if (activity != null)
            {
                activity.SetTag("transaction.id", transactionId);
                activity.SetTag("savepoint.name", savepointName);
                activity.SetTag("savepoint.operation", operation);
            }

            return activity;
        }

        /// <summary>
        /// Starts a new activity (span) for a transaction retry operation.
        /// </summary>
        /// <param name="transactionId">The unique identifier for the transaction.</param>
        /// <param name="requestType">The type name of the request being processed.</param>
        /// <param name="attemptNumber">The current retry attempt number (1-based).</param>
        /// <param name="maxRetries">The maximum number of retry attempts.</param>
        /// <param name="delayMs">The delay before this retry attempt in milliseconds.</param>
        /// <returns>An Activity instance if tracing is enabled, otherwise null.</returns>
        /// <remarks>
        /// The returned Activity should be disposed when the retry attempt completes.
        /// 
        /// The activity includes the following tags:
        /// - transaction.id: Transaction identifier
        /// - transaction.request_type: Request type name
        /// - retry.attempt: Current attempt number
        /// - retry.max_attempts: Maximum retry attempts
        /// - retry.delay_ms: Delay before this attempt
        /// </remarks>
        public Activity? StartRetryActivity(
            string transactionId,
            string requestType,
            int attemptNumber,
            int maxRetries,
            int delayMs)
        {
            var activity = _activitySource.StartActivity(
                name: "relay.transaction.retry",
                kind: ActivityKind.Internal);

            if (activity != null)
            {
                activity.SetTag("transaction.id", transactionId);
                activity.SetTag("transaction.request_type", requestType);
                activity.SetTag("retry.attempt", attemptNumber);
                activity.SetTag("retry.max_attempts", maxRetries);
                activity.SetTag("retry.delay_ms", delayMs);
            }

            return activity;
        }

        /// <summary>
        /// Adds an event to the current activity indicating a transaction lifecycle event.
        /// </summary>
        /// <param name="activity">The activity to add the event to.</param>
        /// <param name="eventName">The name of the event (e.g., "BeforeCommit", "AfterRollback").</param>
        /// <param name="timestamp">The timestamp when the event occurred.</param>
        /// <remarks>
        /// This method is a no-op if the activity is null.
        /// Events provide additional context within a span's timeline.
        /// </remarks>
        public void AddTransactionEvent(Activity? activity, string eventName, DateTime timestamp)
        {
            activity?.AddEvent(new ActivityEvent(
                name: eventName,
                timestamp: timestamp,
                tags: new ActivityTagsCollection()));
        }

        /// <summary>
        /// Sets the status of an activity based on transaction outcome.
        /// </summary>
        /// <param name="activity">The activity to set the status on.</param>
        /// <param name="success">Whether the transaction completed successfully.</param>
        /// <param name="errorMessage">The error message if the transaction failed.</param>
        /// <remarks>
        /// This method is a no-op if the activity is null.
        /// Sets ActivityStatusCode.Ok for successful transactions and ActivityStatusCode.Error for failures.
        /// </remarks>
        public void SetTransactionStatus(Activity? activity, bool success, string? errorMessage = null)
        {
            if (activity == null) return;

            if (success)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
            }
            else
            {
                activity.SetStatus(ActivityStatusCode.Error, errorMessage ?? "Transaction failed");
            }
        }

        /// <summary>
        /// Records an exception in the current activity.
        /// </summary>
        /// <param name="activity">The activity to record the exception in.</param>
        /// <param name="exception">The exception that occurred.</param>
        /// <remarks>
        /// This method is a no-op if the activity is null.
        /// Records exception details as an event with standard OpenTelemetry exception attributes.
        /// </remarks>
        public void RecordException(Activity? activity, Exception exception)
        {
            if (activity == null) return;

            var tags = new ActivityTagsCollection
            {
                { "exception.type", exception.GetType().FullName },
                { "exception.message", exception.Message },
                { "exception.stacktrace", exception.StackTrace }
            };

            activity.AddEvent(new ActivityEvent("exception", tags: tags));
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        }

        /// <summary>
        /// Starts a transaction activity from configuration.
        /// </summary>
        public Activity? StartTransactionActivity(string requestType, ITransactionConfiguration configuration)
        {
            var timeoutSeconds = configuration.Timeout > TimeSpan.Zero && configuration.Timeout != System.Threading.Timeout.InfiniteTimeSpan
                ? (int?)configuration.Timeout.TotalSeconds
                : null;

            return StartTransactionActivity(
                Guid.NewGuid().ToString(),
                requestType,
                configuration.IsolationLevel,
                0,
                configuration.IsReadOnly,
                timeoutSeconds);
        }

        /// <summary>
        /// Records transaction success.
        /// </summary>
        public void RecordTransactionSuccess(Activity? activity, ITransactionContext context, TimeSpan duration)
        {
            if (activity == null) return;

            activity.SetTag("transaction.duration_ms", duration.TotalMilliseconds);
            activity.SetTag("transaction.status", "committed");
            activity.SetStatus(ActivityStatusCode.Ok);
        }

        /// <summary>
        /// Records transaction timeout.
        /// </summary>
        public void RecordTransactionTimeout(Activity? activity, ITransactionContext context, Exception exception)
        {
            if (activity == null) return;

            activity.SetTag("transaction.status", "timeout");
            RecordException(activity, exception);
        }

        /// <summary>
        /// Records transaction rollback.
        /// </summary>
        public void RecordTransactionRollback(Activity? activity, ITransactionContext context, Exception exception)
        {
            if (activity == null) return;

            activity.SetTag("transaction.status", "rolled_back");
            RecordException(activity, exception);
        }

        /// <summary>
        /// Records transaction failure.
        /// </summary>
        public void RecordTransactionFailure(Activity? activity, ITransactionContext context, Exception exception)
        {
            if (activity == null) return;

            activity.SetTag("transaction.status", "failed");
            RecordException(activity, exception);
        }
    }
}
