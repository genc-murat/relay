using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using IsolationLevel = System.Data.IsolationLevel;
using SystemTransactionOptions = System.Transactions.TransactionOptions;

namespace Relay.Core.Transactions;

/// <summary>
/// Coordinates distributed transactions using TransactionScope for operations that span multiple databases or resources.
/// </summary>
/// <remarks>
/// The DistributedTransactionCoordinator uses System.Transactions.TransactionScope to coordinate
/// transactions across multiple resource managers. This enables ACID guarantees across distributed operations.
/// 
/// Key features:
/// - Creates ambient transactions that flow through the call stack
/// - Supports async operations with TransactionScopeAsyncFlowOption
/// - Configurable timeout and isolation level
/// - Automatic commit/rollback coordination
/// 
/// Note: Distributed transactions require MSDTC (Microsoft Distributed Transaction Coordinator) or equivalent
/// transaction manager to be running on the system.
/// </remarks>
public sealed class DistributedTransactionCoordinator
{
    private readonly ILogger<DistributedTransactionCoordinator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedTransactionCoordinator"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    public DistributedTransactionCoordinator(ILogger<DistributedTransactionCoordinator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a distributed transaction scope with the specified configuration.
    /// </summary>
    /// <param name="configuration">The transaction configuration.</param>
    /// <param name="requestType">The type of request being executed.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>A tuple containing the transaction scope, transaction ID, and start time.</returns>
    /// <exception cref="DistributedTransactionException">Thrown when distributed transaction creation fails.</exception>
    public (TransactionScope Scope, string TransactionId, DateTime StartTime) CreateDistributedTransactionScope(
        ITransactionConfiguration configuration,
        string requestType,
        CancellationToken cancellationToken)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var transactionId = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;

        try
        {
            // Convert System.Data.IsolationLevel to System.Transactions.IsolationLevel
            var transactionIsolationLevel = ConvertIsolationLevel(configuration.IsolationLevel);

            // Configure transaction scope options
            var scopeOptions = new SystemTransactionOptions
            {
                IsolationLevel = transactionIsolationLevel,
                Timeout = IsTimeoutEnabled(configuration.Timeout) 
                    ? configuration.Timeout 
                    : TransactionManager.DefaultTimeout
            };

            _logger.LogInformation(
                "Creating distributed transaction scope {TransactionId} for {RequestType} with isolation level {IsolationLevel} and timeout {TimeoutSeconds} seconds",
                transactionId,
                requestType,
                configuration.IsolationLevel,
                scopeOptions.Timeout.TotalSeconds);

            // Create transaction scope with async flow enabled
            var scope = new TransactionScope(
                TransactionScopeOption.Required,
                scopeOptions,
                TransactionScopeAsyncFlowOption.Enabled);

            _logger.LogDebug(
                "Distributed transaction scope {TransactionId} created successfully at {StartTime}",
                transactionId,
                startTime);

            return (scope, transactionId, startTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create distributed transaction scope {TransactionId} for {RequestType}",
                transactionId,
                requestType);

            throw new DistributedTransactionException(
                $"Failed to create distributed transaction scope for request type '{requestType}'",
                transactionId,
                ex);
        }
    }

    /// <summary>
    /// Executes an operation within a distributed transaction scope with timeout enforcement.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="transactionId">The transaction ID for logging.</param>
    /// <param name="timeout">The configured timeout duration.</param>
    /// <param name="requestType">The type of request being executed.</param>
    /// <param name="startTime">The transaction start time.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="TransactionTimeoutException">Thrown when the operation times out.</exception>
    /// <exception cref="DistributedTransactionException">Thrown when the distributed transaction fails.</exception>
    public async Task<TResult> ExecuteInDistributedTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        string transactionId,
        TimeSpan timeout,
        string requestType,
        DateTime startTime,
        CancellationToken cancellationToken)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        CancellationTokenSource? timeoutCts = null;
        CancellationToken effectiveCancellationToken = cancellationToken;

        try
        {
            // Create timeout cancellation token if timeout is enabled
            if (IsTimeoutEnabled(timeout))
            {
                timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(timeout);
                effectiveCancellationToken = timeoutCts.Token;
            }

            return await operation(effectiveCancellationToken);
        }
        catch (OperationCanceledException ex) when (timeoutCts?.IsCancellationRequested == true && !cancellationToken.IsCancellationRequested)
        {
            // Timeout occurred during operation execution
            var elapsed = DateTime.UtcNow - startTime;

            _logger.LogError(
                ex,
                "Distributed transaction {TransactionId} for {RequestType} timed out during execution after {ElapsedSeconds} seconds",
                transactionId,
                requestType,
                elapsed.TotalSeconds);

            throw new TransactionTimeoutException(
                transactionId,
                timeout,
                elapsed,
                requestType,
                ex);
        }
        catch (TransactionException ex)
        {
            var elapsed = DateTime.UtcNow - startTime;

            _logger.LogError(
                ex,
                "Distributed transaction {TransactionId} for {RequestType} failed after {ElapsedSeconds} seconds",
                transactionId,
                requestType,
                elapsed.TotalSeconds);

            throw new DistributedTransactionException(
                $"Distributed transaction failed for request type '{requestType}'",
                transactionId,
                ex);
        }
        finally
        {
            timeoutCts?.Dispose();
        }
    }

    /// <summary>
    /// Completes (commits) the distributed transaction scope.
    /// </summary>
    /// <param name="scope">The transaction scope to complete.</param>
    /// <param name="transactionId">The transaction ID for logging.</param>
    /// <param name="requestType">The type of request being executed.</param>
    /// <param name="startTime">The transaction start time.</param>
    public void CompleteDistributedTransaction(
        TransactionScope scope,
        string transactionId,
        string requestType,
        DateTime startTime)
    {
        if (scope == null)
            throw new ArgumentNullException(nameof(scope));

        var elapsed = DateTime.UtcNow - startTime;

        try
        {
            _logger.LogInformation(
                "Completing distributed transaction {TransactionId} for {RequestType} after {ElapsedSeconds} seconds",
                transactionId,
                requestType,
                elapsed.TotalSeconds);

            scope.Complete();

            _logger.LogDebug(
                "Distributed transaction {TransactionId} completed successfully",
                transactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to complete distributed transaction {TransactionId} for {RequestType}",
                transactionId,
                requestType);

            throw new DistributedTransactionException(
                $"Failed to complete distributed transaction for request type '{requestType}'",
                transactionId,
                ex);
        }
    }

    /// <summary>
    /// Disposes the distributed transaction scope, which will rollback if not completed.
    /// </summary>
    /// <param name="scope">The transaction scope to dispose.</param>
    /// <param name="transactionId">The transaction ID for logging.</param>
    /// <param name="requestType">The type of request being executed.</param>
    /// <param name="startTime">The transaction start time.</param>
    /// <param name="exception">The exception that caused the rollback, if any.</param>
    public void DisposeDistributedTransaction(
        TransactionScope scope,
        string transactionId,
        string requestType,
        DateTime startTime,
        Exception? exception = null)
    {
        if (scope == null)
            return;

        var elapsed = DateTime.UtcNow - startTime;

        if (exception != null)
        {
            _logger.LogWarning(
                exception,
                "Disposing distributed transaction {TransactionId} for {RequestType} after {ElapsedSeconds} seconds due to exception (will rollback)",
                transactionId,
                requestType,
                elapsed.TotalSeconds);
        }
        else
        {
            _logger.LogDebug(
                "Disposing distributed transaction {TransactionId} for {RequestType}",
                transactionId,
                requestType);
        }

        try
        {
            scope.Dispose();

            if (exception != null)
            {
                _logger.LogDebug(
                    "Distributed transaction {TransactionId} rolled back successfully",
                    transactionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error disposing distributed transaction {TransactionId} for {RequestType}",
                transactionId,
                requestType);

            // Don't throw - we're already in an error path
        }
    }

    /// <summary>
    /// Converts System.Data.IsolationLevel to System.Transactions.IsolationLevel.
    /// </summary>
    /// <param name="isolationLevel">The System.Data.IsolationLevel to convert.</param>
    /// <returns>The equivalent System.Transactions.IsolationLevel.</returns>
    /// <exception cref="ArgumentException">Thrown when the isolation level is not supported.</exception>
    private static System.Transactions.IsolationLevel ConvertIsolationLevel(IsolationLevel isolationLevel)
    {
        return isolationLevel switch
        {
            IsolationLevel.Unspecified => System.Transactions.IsolationLevel.Unspecified,
            IsolationLevel.Chaos => System.Transactions.IsolationLevel.Chaos,
            IsolationLevel.ReadUncommitted => System.Transactions.IsolationLevel.ReadUncommitted,
            IsolationLevel.ReadCommitted => System.Transactions.IsolationLevel.ReadCommitted,
            IsolationLevel.RepeatableRead => System.Transactions.IsolationLevel.RepeatableRead,
            IsolationLevel.Serializable => System.Transactions.IsolationLevel.Serializable,
            IsolationLevel.Snapshot => System.Transactions.IsolationLevel.Snapshot,
            _ => throw new ArgumentException($"Unsupported isolation level: {isolationLevel}", nameof(isolationLevel))
        };
    }

    /// <summary>
    /// Determines whether timeout enforcement is enabled for the given timeout value.
    /// </summary>
    /// <param name="timeout">The timeout value to check.</param>
    /// <returns>True if timeout enforcement is enabled; otherwise, false.</returns>
    private static bool IsTimeoutEnabled(TimeSpan timeout)
    {
        // Timeout is disabled if it's zero or infinite
        return timeout > TimeSpan.Zero && timeout != System.Threading.Timeout.InfiniteTimeSpan;
    }

    /// <summary>
    /// Creates a transaction scope for testing purposes.
    /// </summary>
    /// <param name="configuration">The transaction configuration.</param>
    /// <returns>The created transaction scope.</returns>
    public TransactionScope CreateTransactionScope(ITransactionConfiguration configuration)
    {
        var transactionIsolationLevel = ConvertIsolationLevel(configuration.IsolationLevel);

        var scopeOptions = new SystemTransactionOptions
        {
            IsolationLevel = transactionIsolationLevel,
            Timeout = IsTimeoutEnabled(configuration.Timeout) 
                ? configuration.Timeout 
                : TransactionManager.DefaultTimeout
        };

        return new TransactionScope(
            TransactionScopeOption.Required,
            scopeOptions,
            TransactionScopeAsyncFlowOption.Enabled);
    }

    /// <summary>
    /// Commits a transaction scope.
    /// </summary>
    /// <param name="scope">The transaction scope to commit.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task CommitAsync(TransactionScope scope, CancellationToken cancellationToken = default)
    {
        scope.Complete();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Rolls back a transaction scope.
    /// </summary>
    /// <param name="scope">The transaction scope to rollback.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task RollbackAsync(TransactionScope scope, CancellationToken cancellationToken = default)
    {
        // Rollback is implicit when scope is disposed without calling Complete()
        return Task.CompletedTask;
    }
}
