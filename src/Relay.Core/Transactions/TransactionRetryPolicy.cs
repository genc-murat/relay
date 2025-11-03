using System;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Defines the retry strategy for transaction operations.
    /// </summary>
    public enum RetryStrategy
    {
        /// <summary>
        /// Linear retry strategy with fixed delay between retries.
        /// </summary>
        Linear,

        /// <summary>
        /// Exponential backoff retry strategy with increasing delay between retries.
        /// </summary>
        ExponentialBackoff
    }

    /// <summary>
    /// Configuration for automatic retry of transient transaction failures.
    /// </summary>
    /// <remarks>
    /// This policy defines how the transaction system should handle transient failures such as
    /// deadlocks, connection timeouts, or temporary database unavailability. The retry logic
    /// will only be applied to failures that are identified as transient by the ShouldRetry predicate.
    /// 
    /// Example usage:
    /// <code>
    /// var retryPolicy = new TransactionRetryPolicy
    /// {
    ///     MaxRetries = 3,
    ///     InitialDelay = TimeSpan.FromMilliseconds(100),
    ///     Strategy = RetryStrategy.ExponentialBackoff,
    ///     ShouldRetry = exception => exception is DbUpdateException || exception is TimeoutException
    /// };
    /// </code>
    /// </remarks>
    public class TransactionRetryPolicy
    {
        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// </summary>
        /// <remarks>
        /// This is the maximum number of times the transaction will be retried after the initial attempt fails.
        /// For example, MaxRetries = 3 means the transaction will be attempted up to 4 times total
        /// (1 initial attempt + 3 retries). Default is 3.
        /// 
        /// Set to 0 to disable retries.
        /// </remarks>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the initial delay before the first retry attempt.
        /// </summary>
        /// <remarks>
        /// For linear retry strategy, this is the fixed delay between all retry attempts.
        /// For exponential backoff strategy, this is the base delay that will be multiplied
        /// by powers of 2 for subsequent retries.
        /// 
        /// Default is 100 milliseconds.
        /// 
        /// Example delays with exponential backoff:
        /// - Retry 1: 100ms
        /// - Retry 2: 200ms
        /// - Retry 3: 400ms
        /// </remarks>
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets or sets the retry strategy to use.
        /// </summary>
        /// <remarks>
        /// - Linear: Fixed delay between retries (InitialDelay)
        /// - ExponentialBackoff: Exponentially increasing delay (InitialDelay * 2^retryCount)
        /// 
        /// Default is ExponentialBackoff, which is recommended for most scenarios as it reduces
        /// load on the database during transient failure conditions.
        /// </remarks>
        public RetryStrategy Strategy { get; set; } = RetryStrategy.ExponentialBackoff;

        /// <summary>
        /// Gets or sets the transient error detector used to determine whether an exception should trigger a retry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The detector is called for each exception to determine if it represents a transient failure
        /// that should be retried. Only exceptions identified as transient will trigger a retry.
        /// </para>
        /// <para>
        /// The default implementation (<see cref="DefaultTransientErrorDetector"/>) detects common transient 
        /// database errors such as:
        /// <list type="bullet">
        /// <item><description>Deadlock exceptions</description></item>
        /// <item><description>Connection timeout exceptions</description></item>
        /// <item><description>Transient network errors</description></item>
        /// <item><description>Database unavailable errors</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// You can provide a custom detector to handle application-specific or database-specific transient errors:
        /// <code>
        /// TransientErrorDetector = new CustomTransientErrorDetector(exception =>
        /// {
        ///     // SQL Server specific error codes
        ///     if (exception is SqlException sqlEx)
        ///         return sqlEx.Number == 1205; // Deadlock
        ///     
        ///     return exception is TimeoutException;
        /// });
        /// </code>
        /// </para>
        /// <para>
        /// Alternatively, you can use the <see cref="ShouldRetry"/> predicate for simpler scenarios.
        /// If both are set, <see cref="TransientErrorDetector"/> takes precedence.
        /// </para>
        /// </remarks>
        public ITransientErrorDetector? TransientErrorDetector { get; set; }

        /// <summary>
        /// Gets or sets the predicate that determines whether an exception should trigger a retry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a simpler alternative to <see cref="TransientErrorDetector"/> for basic scenarios.
        /// If both are set, <see cref="TransientErrorDetector"/> takes precedence.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// ShouldRetry = exception =>
        /// {
        ///     return exception is DbUpdateException dbEx &amp;&amp; dbEx.IsTransient()
        ///         || exception is TimeoutException
        ///         || exception is CustomTransientException;
        /// };
        /// </code>
        /// </para>
        /// </remarks>
        public Func<Exception, bool>? ShouldRetry { get; set; }
    }
}
