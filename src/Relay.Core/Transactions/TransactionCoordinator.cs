using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Coordinates transaction lifecycle operations including timeout enforcement, nested transaction management,
    /// and savepoint coordination.
    /// </summary>
    /// <remarks>
    /// The TransactionCoordinator is responsible for:
    /// - Beginning transactions with proper isolation levels
    /// - Enforcing transaction timeouts
    /// - Managing nested transaction scenarios
    /// - Coordinating savepoint operations
    /// - Committing and rolling back transactions
    /// 
    /// This class is used internally by the TransactionBehavior to manage transaction operations.
    /// </remarks>
    public sealed class TransactionCoordinator
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TransactionCoordinator> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionCoordinator"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for transaction operations.</param>
        /// <param name="logger">The logger for diagnostic output.</param>
        public TransactionCoordinator(IUnitOfWork unitOfWork, ILogger<TransactionCoordinator> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Begins a new transaction with the specified configuration and timeout enforcement.
        /// </summary>
        /// <param name="configuration">The transaction configuration.</param>
        /// <param name="requestType">The type of request being executed.</param>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        /// <returns>A tuple containing the database transaction, transaction context, and timeout cancellation token source.</returns>
        /// <exception cref="TransactionTimeoutException">Thrown when the transaction times out.</exception>
        public async Task<(IDbTransaction Transaction, ITransactionContext Context, CancellationTokenSource? TimeoutCts)> BeginTransactionAsync(
            ITransactionConfiguration configuration,
            string requestType,
            CancellationToken cancellationToken)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var transactionId = Guid.NewGuid().ToString();
            var startTime = DateTime.UtcNow;

            // Create timeout cancellation token source if timeout is configured
            CancellationTokenSource? timeoutCts = null;
            CancellationToken effectiveCancellationToken = cancellationToken;

            if (IsTimeoutEnabled(configuration.Timeout))
            {
                timeoutCts = CreateTimeoutCancellationTokenSource(configuration.Timeout, cancellationToken);
                effectiveCancellationToken = timeoutCts.Token;

                _logger.LogDebug(
                    "Transaction {TransactionId} for {RequestType} configured with timeout of {TimeoutSeconds} seconds",
                    transactionId,
                    requestType,
                    configuration.Timeout.TotalSeconds);
            }
            else
            {
                _logger.LogDebug(
                    "Transaction {TransactionId} for {RequestType} configured with no timeout",
                    transactionId,
                    requestType);
            }

            try
            {
                // Begin the database transaction with the specified isolation level
                _logger.LogInformation(
                    "Beginning transaction {TransactionId} for {RequestType} with isolation level {IsolationLevel}{ReadOnlyIndicator}",
                    transactionId,
                    requestType,
                    configuration.IsolationLevel,
                    configuration.IsReadOnly ? " (read-only)" : "");

                var transaction = await _unitOfWork.BeginTransactionAsync(
                    configuration.IsolationLevel,
                    effectiveCancellationToken);

                // Configure read-only transaction if specified
                if (configuration.IsReadOnly)
                {
                    _logger.LogDebug(
                        "Configuring transaction {TransactionId} as read-only",
                        transactionId);

                    ReadOnlyTransactionEnforcer.ConfigureReadOnlyTransaction(transaction, _logger);
                }

                var context = _unitOfWork.CurrentTransactionContext;

                if (context == null)
                {
                    throw new InvalidOperationException(
                        $"Transaction context was not created after beginning transaction {transactionId}");
                }

                _logger.LogDebug(
                    "Transaction {TransactionId} started successfully at {StartTime} with nesting level {NestingLevel}{ReadOnlyIndicator}",
                    transactionId,
                    startTime,
                    context.NestingLevel,
                    configuration.IsReadOnly ? " (read-only)" : "");

                return (transaction, context, timeoutCts);
            }
            catch (OperationCanceledException ex) when (timeoutCts?.IsCancellationRequested == true && !cancellationToken.IsCancellationRequested)
            {
                // Timeout occurred
                var elapsed = DateTime.UtcNow - startTime;
                
                _logger.LogError(
                    ex,
                    "Transaction {TransactionId} for {RequestType} timed out after {ElapsedSeconds} seconds (configured timeout: {TimeoutSeconds} seconds)",
                    transactionId,
                    requestType,
                    elapsed.TotalSeconds,
                    configuration.Timeout.TotalSeconds);

                throw new TransactionTimeoutException(
                    transactionId,
                    configuration.Timeout,
                    elapsed,
                    requestType,
                    ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to begin transaction {TransactionId} for {RequestType}",
                    transactionId,
                    requestType);

                timeoutCts?.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Executes an operation within a transaction with timeout enforcement.
        /// </summary>
        /// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="context">The transaction context.</param>
        /// <param name="timeoutCts">The timeout cancellation token source.</param>
        /// <param name="timeout">The configured timeout duration.</param>
        /// <param name="requestType">The type of request being executed.</param>
        /// <param name="cancellationToken">The original cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="TransactionTimeoutException">Thrown when the operation times out.</exception>
        public async Task<TResult> ExecuteWithTimeoutAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            ITransactionContext context,
            CancellationTokenSource? timeoutCts,
            TimeSpan timeout,
            string requestType,
            CancellationToken cancellationToken)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var effectiveCancellationToken = timeoutCts?.Token ?? cancellationToken;

            try
            {
                return await operation(effectiveCancellationToken);
            }
            catch (OperationCanceledException ex) when (timeoutCts?.IsCancellationRequested == true && !cancellationToken.IsCancellationRequested)
            {
                // Timeout occurred during operation execution
                var elapsed = DateTime.UtcNow - context.StartedAt;

                _logger.LogError(
                    ex,
                    "Transaction {TransactionId} for {RequestType} timed out during execution after {ElapsedSeconds} seconds",
                    context.TransactionId,
                    requestType,
                    elapsed.TotalSeconds);

                throw new TransactionTimeoutException(
                    context.TransactionId,
                    timeout,
                    elapsed,
                    requestType,
                    ex);
            }
        }

        /// <summary>
        /// Commits the transaction.
        /// </summary>
        /// <param name="transaction">The database transaction to commit.</param>
        /// <param name="context">The transaction context.</param>
        /// <param name="requestType">The type of request being executed.</param>
        public async Task CommitTransactionAsync(IDbTransaction transaction, ITransactionContext context, string requestType, CancellationToken cancellationToken = default)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var elapsed = DateTime.UtcNow - context.StartedAt;

            _logger.LogInformation(
                "Committing transaction {TransactionId} for {RequestType} after {ElapsedSeconds} seconds",
                context.TransactionId,
                requestType,
                elapsed.TotalSeconds);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogDebug(
                "Transaction {TransactionId} committed successfully",
                context.TransactionId);
        }

        /// <summary>
        /// Rolls back the transaction.
        /// </summary>
        /// <param name="transaction">The database transaction to roll back.</param>
        /// <param name="context">The transaction context.</param>
        /// <param name="requestType">The type of request being executed.</param>
        /// <param name="exception">The exception that caused the rollback.</param>
        public async Task RollbackTransactionAsync(
            IDbTransaction transaction,
            ITransactionContext context,
            string requestType,
            Exception? exception = null,
            CancellationToken cancellationToken = default)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var elapsed = DateTime.UtcNow - context.StartedAt;

            if (exception != null)
            {
                _logger.LogWarning(
                    exception,
                    "Rolling back transaction {TransactionId} for {RequestType} after {ElapsedSeconds} seconds due to exception",
                    context.TransactionId,
                    requestType,
                    elapsed.TotalSeconds);
            }
            else
            {
                _logger.LogInformation(
                    "Rolling back transaction {TransactionId} for {RequestType} after {ElapsedSeconds} seconds",
                    context.TransactionId,
                    requestType,
                    elapsed.TotalSeconds);
            }

            await transaction.RollbackAsync(cancellationToken);

            _logger.LogDebug(
                "Transaction {TransactionId} rolled back successfully",
                context.TransactionId);
        }

        /// <summary>
        /// Determines whether timeout enforcement is enabled for the given timeout value.
        /// </summary>
        /// <param name="timeout">The timeout value to check.</param>
        /// <returns>True if timeout enforcement is enabled; otherwise, false.</returns>
        private static bool IsTimeoutEnabled(TimeSpan timeout)
        {
            // Timeout is disabled if it's zero or infinite
            return timeout > TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan;
        }

        /// <summary>
        /// Creates a cancellation token source with the specified timeout, linked to the original cancellation token.
        /// </summary>
        /// <param name="timeout">The timeout duration.</param>
        /// <param name="cancellationToken">The original cancellation token to link.</param>
        /// <returns>A cancellation token source that will be cancelled after the timeout or when the original token is cancelled.</returns>
        private static CancellationTokenSource CreateTimeoutCancellationTokenSource(
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);
            return timeoutCts;
        }
    }
}
