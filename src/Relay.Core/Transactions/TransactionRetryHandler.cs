using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.Transactions;

/// <summary>
/// Handles automatic retry of transient transaction failures.
/// </summary>
/// <remarks>
/// <para>
/// This handler implements the retry logic for transaction operations that fail with transient errors.
/// It uses the configured retry policy to determine:
/// <list type="bullet">
/// <item><description>Whether an error is transient and should be retried</description></item>
/// <item><description>How many times to retry</description></item>
/// <item><description>How long to wait between retry attempts</description></item>
/// </list>
/// </para>
/// <para>
/// The retry handler logs each retry attempt with context information for observability
/// and throws <see cref="TransactionRetryExhaustedException"/> when all retry attempts are exhausted.
/// </para>
/// </remarks>
public class TransactionRetryHandler : ITransactionRetryHandler
{
    private readonly ILogger<TransactionRetryHandler> _logger;
    private readonly ITransientErrorDetector _defaultErrorDetector;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionRetryHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger for recording retry attempts and failures.</param>
    public TransactionRetryHandler(ILogger<TransactionRetryHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultErrorDetector = new DefaultTransientErrorDetector();
    }

    /// <summary>
    /// Executes an operation with automatic retry on transient failures.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="retryPolicy">The retry policy to apply. If null, no retry is performed.</param>
    /// <param name="transactionId">The transaction ID for logging context.</param>
    /// <param name="requestType">The request type for logging context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="TransactionRetryExhaustedException">
    /// Thrown when all retry attempts are exhausted and the operation continues to fail.
    /// </exception>
    /// <remarks>
    /// If no retry policy is provided, the operation is executed once without retry.
    /// If the operation succeeds on any attempt (including the initial attempt), the result is returned immediately.
    /// </remarks>
    public async Task<TResult> ExecuteWithRetryAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        TransactionRetryPolicy? retryPolicy,
        string? transactionId,
        string? requestType,
        CancellationToken cancellationToken)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        // If no retry policy, execute once without retry
        if (retryPolicy == null || retryPolicy.MaxRetries == 0)
        {
            return await operation(cancellationToken).ConfigureAwait(false);
        }

        // Get the error detector and retry strategy
        var errorDetector = GetErrorDetector(retryPolicy);
        var retryStrategy = GetRetryStrategy(retryPolicy);

        Exception? lastException = null;
        var attemptNumber = 0;

        // Initial attempt + retries
        var maxAttempts = retryPolicy.MaxRetries + 1;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            attemptNumber = attempt;

            try
            {
                // Execute the operation
                var result = await operation(cancellationToken).ConfigureAwait(false);

                // Success - log if this was a retry
                if (attempt > 1)
                {
                    _logger.LogInformation(
                        "Transaction '{TransactionId}' for request type '{RequestType}' succeeded on retry attempt {RetryAttempt} of {MaxRetries}.",
                        transactionId,
                        requestType,
                        attempt - 1,
                        retryPolicy.MaxRetries);
                }

                return result;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                lastException = ex;

                // Check if this is a transient error that should be retried
                if (!errorDetector.IsTransient(ex))
                {
                    _logger.LogWarning(
                        ex,
                        "Transaction '{TransactionId}' for request type '{RequestType}' failed with non-transient error. Not retrying.",
                        transactionId,
                        requestType);

                    throw;
                }

                // Calculate delay for this retry attempt
                var retryAttemptNumber = attempt; // This is the retry number (1-based)
                var delay = retryStrategy.CalculateDelay(retryAttemptNumber, retryPolicy.InitialDelay);

                _logger.LogWarning(
                    ex,
                    "Transaction '{TransactionId}' for request type '{RequestType}' failed with transient error. " +
                    "Retry attempt {RetryAttempt} of {MaxRetries} will occur after {DelayMs}ms delay. Error: {ErrorMessage}",
                    transactionId,
                    requestType,
                    retryAttemptNumber,
                    retryPolicy.MaxRetries,
                    delay.TotalMilliseconds,
                    ex.Message);

                // Wait before retrying
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                // Last attempt failed
                lastException = ex;
            }
        }

        // All retries exhausted
        _logger.LogError(
            lastException,
            "Transaction '{TransactionId}' for request type '{RequestType}' failed after {RetryAttempts} retry attempts. " +
            "Final error: {ErrorMessage}",
            transactionId,
            requestType,
            retryPolicy.MaxRetries,
            lastException?.Message);

        throw new TransactionRetryExhaustedException(
            retryPolicy.MaxRetries,
            transactionId,
            requestType,
            lastException!);
    }

    /// <summary>
    /// Gets the error detector from the retry policy.
    /// </summary>
    /// <param name="retryPolicy">The retry policy.</param>
    /// <returns>The error detector to use.</returns>
    private ITransientErrorDetector GetErrorDetector(TransactionRetryPolicy retryPolicy)
    {
        // Prefer explicit detector
        if (retryPolicy.TransientErrorDetector != null)
            return retryPolicy.TransientErrorDetector;

        // Fall back to predicate-based detector
        if (retryPolicy.ShouldRetry != null)
            return new CustomTransientErrorDetector(retryPolicy.ShouldRetry);

        // Use default detector
        return _defaultErrorDetector;
    }

    /// <summary>
    /// Gets the retry strategy from the retry policy.
    /// </summary>
    /// <param name="retryPolicy">The retry policy.</param>
    /// <returns>The retry strategy to use.</returns>
    private static IRetryStrategy GetRetryStrategy(TransactionRetryPolicy retryPolicy)
    {
        return retryPolicy.Strategy switch
        {
            RetryStrategy.Linear => new LinearRetryStrategy(),
            RetryStrategy.ExponentialBackoff => new ExponentialBackoffRetryStrategy(),
            _ => throw new InvalidOperationException($"Unknown retry strategy: {retryPolicy.Strategy}")
        };
    }
}
