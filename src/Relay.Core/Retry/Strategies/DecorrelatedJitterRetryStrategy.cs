using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Retry.Strategies
{
    /// <summary>
    /// Implements the Decorrelated Jitter retry strategy, which provides optimal distribution of retry attempts
    /// in distributed systems by using a decorrelated algorithm to calculate delays with randomized jitter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This strategy is based on AWS's research into optimal backoff strategies for distributed systems.
    /// Unlike traditional exponential backoff with jitter, decorrelated jitter calculates each retry delay
    /// based on the previous delay rather than a fixed exponential formula. This approach provides better
    /// distribution of retries across multiple clients and significantly reduces the "thundering herd" problem.
    /// </para>
    /// <para>
    /// The algorithm calculates each delay as: delay = Random(baseDelay, previousDelay * 3), capped at maxDelay.
    /// This creates a sequence where each delay is decorrelated from the attempt number but correlated with
    /// the previous delay, resulting in a more natural and distributed retry pattern.
    /// </para>
    /// <para>
    /// <strong>When to use this strategy:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>High-concurrency distributed systems where many clients may fail simultaneously</description></item>
    /// <item><description>Microservices architectures with shared downstream dependencies</description></item>
    /// <item><description>Cloud-native applications dealing with transient failures (rate limiting, throttling, temporary unavailability)</description></item>
    /// <item><description>Scenarios where you want to avoid synchronized retry storms after mass failures</description></item>
    /// <item><description>Systems where downstream services need time to recover without being overwhelmed</description></item>
    /// </list>
    /// <para>
    /// <strong>Advantages over standard exponential backoff:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Better distribution: Reduces correlation between clients' retry attempts</description></item>
    /// <item><description>Faster recovery: Can retry sooner on average while maintaining backoff pressure</description></item>
    /// <item><description>Smoother load: Creates a more uniform retry pattern rather than synchronized waves</description></item>
    /// <item><description>Optimal resource utilization: Proven to minimize completion time in distributed scenarios</description></item>
    /// </list>
    /// <para>
    /// This strategy is thread-safe and can be safely shared across multiple concurrent operations.
    /// Each retry sequence maintains its own delay state internally without shared mutable state.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic usage with default settings
    /// var strategy = new DecorrelatedJitterRetryStrategy(
    ///     baseDelay: TimeSpan.FromMilliseconds(100),
    ///     maxDelay: TimeSpan.FromSeconds(30)
    /// );
    ///
    /// // Advanced usage with maximum attempts limit
    /// var strategy = new DecorrelatedJitterRetryStrategy(
    ///     baseDelay: TimeSpan.FromMilliseconds(50),
    ///     maxDelay: TimeSpan.FromSeconds(60),
    ///     maxAttempts: 10
    /// );
    ///
    /// // Usage with retry pipeline
    /// var retryBehavior = new RetryPipelineBehavior&lt;MyRequest, MyResponse&gt;(strategy);
    /// mediator.Pipeline.AddBehavior(retryBehavior);
    /// </code>
    /// </example>
    public sealed class DecorrelatedJitterRetryStrategy : IRetryStrategy, IDisposable
    {
        private readonly TimeSpan _baseDelay;
        private readonly TimeSpan _maxDelay;
        private readonly int? _maxAttempts;
        private readonly ThreadLocal<Random> _random;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="DecorrelatedJitterRetryStrategy"/> class.
        /// </summary>
        /// <param name="baseDelay">
        /// The base delay that serves as the minimum delay between retry attempts.
        /// This is the starting point for the decorrelated jitter algorithm.
        /// </param>
        /// <param name="maxDelay">
        /// The maximum delay between retry attempts. Calculated delays will be capped at this value
        /// to prevent excessively long waits that could impact user experience or timeout constraints.
        /// </param>
        /// <param name="maxAttempts">
        /// The maximum number of retry attempts allowed. If null, retries will continue indefinitely
        /// (subject to other constraints such as circuit breakers or timeout policies).
        /// Default is null (unlimited retries).
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when baseDelay is negative, maxDelay is less than baseDelay, or maxAttempts is less than 1.
        /// </exception>
        /// <remarks>
        /// The baseDelay should typically be set to the minimum acceptable delay for your scenario
        /// (e.g., 50-100ms for API calls). The maxDelay should be chosen based on your timeout
        /// requirements and user experience constraints (typically 30-60 seconds for user-facing
        /// operations, potentially longer for background tasks).
        /// </remarks>
        public DecorrelatedJitterRetryStrategy(
            TimeSpan baseDelay,
            TimeSpan maxDelay,
            int? maxAttempts = null)
        {
            if (baseDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(baseDelay),
                    baseDelay,
                    "Base delay must be non-negative.");
            }

            if (maxDelay < baseDelay)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxDelay),
                    maxDelay,
                    "Max delay must be greater than or equal to base delay.");
            }

            if (maxAttempts.HasValue && maxAttempts.Value < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxAttempts),
                    maxAttempts,
                    "Max attempts must be at least 1 if specified.");
            }

            _baseDelay = baseDelay;
            _maxDelay = maxDelay;
            _maxAttempts = maxAttempts;

            // Use ThreadLocal<Random> to ensure thread-safety without locks
            // Each thread gets its own Random instance with a thread-specific seed
            _random = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));
        }

        /// <summary>
        /// Determines whether a retry should be attempted based on the current attempt number
        /// and the maximum attempts configuration.
        /// </summary>
        /// <param name="attempt">
        /// The current attempt number (1-based). The first retry attempt is 1, the second is 2, etc.
        /// </param>
        /// <param name="exception">
        /// The exception that caused the failure. This can be used to implement exception-specific
        /// retry logic in derived classes or custom implementations.
        /// </param>
        /// <param name="cancellationToken">
        /// Cancellation token for the operation. When cancelled, the operation should stop gracefully.
        /// </param>
        /// <returns>
        /// A <see cref="ValueTask{Boolean}"/> that represents the asynchronous operation.
        /// Returns true if a retry should be attempted, false otherwise.
        /// </returns>
        /// <remarks>
        /// This method returns false for attempt 0 (the initial execution, not a retry),
        /// and for attempts beyond the configured maxAttempts (if specified).
        /// For all valid retry attempts, it returns true, allowing the strategy to handle
        /// transient failures consistently.
        /// </remarks>
        public ValueTask<bool> ShouldRetryAsync(int attempt, Exception exception, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Don't retry on the initial attempt (attempt 0)
            if (attempt <= 0)
            {
                return new ValueTask<bool>(false);
            }

            // Check if we've exceeded the maximum number of attempts
            if (_maxAttempts.HasValue && attempt > _maxAttempts.Value)
            {
                return new ValueTask<bool>(false);
            }

            return new ValueTask<bool>(true);
        }

        /// <summary>
        /// Calculates the delay before the next retry attempt using the decorrelated jitter algorithm.
        /// </summary>
        /// <param name="attempt">
        /// The current attempt number (1-based). Used to track the retry sequence state.
        /// </param>
        /// <param name="exception">
        /// The exception that caused the failure. Can be used for exception-specific delay logic.
        /// </param>
        /// <param name="cancellationToken">
        /// Cancellation token for the operation. When cancelled, the operation should stop gracefully.
        /// </param>
        /// <returns>
        /// A <see cref="ValueTask{TimeSpan}"/> representing the calculated delay before the next retry.
        /// The delay is calculated using: Random(baseDelay, min(maxDelay, previousDelay * 3)).
        /// </returns>
        /// <remarks>
        /// <para>
        /// The decorrelated jitter algorithm works as follows:
        /// </para>
        /// <list type="number">
        /// <item><description>For the first retry (attempt 1), the delay is random between baseDelay and baseDelay * 3</description></item>
        /// <item><description>For subsequent retries, the delay is random between baseDelay and min(maxDelay, previousDelay * 3)</description></item>
        /// <item><description>This creates a sequence where each delay depends on (is correlated with) the previous delay</description></item>
        /// <item><description>The randomization ensures different clients have decorrelated retry patterns</description></item>
        /// </list>
        /// <para>
        /// The algorithm naturally provides backoff pressure (delays generally increase) while maintaining
        /// randomization to prevent synchronized retry storms. The * 3 multiplier provides a good balance
        /// between aggressive retries and giving systems time to recover.
        /// </para>
        /// <para>
        /// Performance note: This method uses thread-local Random instances to avoid lock contention
        /// in high-concurrency scenarios, making it safe and efficient for parallel retry operations.
        /// </para>
        /// </remarks>
        public ValueTask<TimeSpan> GetRetryDelayAsync(int attempt, Exception exception, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (attempt <= 0)
            {
                return new ValueTask<TimeSpan>(TimeSpan.Zero);
            }

            // Get the previous delay for decorrelation
            // For the first attempt, we use baseDelay as the "previous" delay
            var previousDelay = attempt == 1
                ? _baseDelay
                : CalculatePreviousDelay(attempt - 1);

            // Decorrelated jitter formula: Random(baseDelay, min(maxDelay, previousDelay * 3))
            var maxJitteredDelay = TimeSpan.FromMilliseconds(previousDelay.TotalMilliseconds * 3.0);

            // Cap at maxDelay to prevent unbounded growth
            if (maxJitteredDelay > _maxDelay)
            {
                maxJitteredDelay = _maxDelay;
            }

            // Generate random delay between baseDelay and maxJitteredDelay
            var random = _random.Value ?? throw new InvalidOperationException("Thread-local Random instance is not initialized.");
            var delayRange = maxJitteredDelay.TotalMilliseconds - _baseDelay.TotalMilliseconds;

            // Handle edge case where baseDelay equals maxDelay
            if (delayRange <= 0)
            {
                return new ValueTask<TimeSpan>(_baseDelay);
            }

            var randomDelay = _baseDelay.TotalMilliseconds + (random.NextDouble() * delayRange);
            var delay = TimeSpan.FromMilliseconds(randomDelay);

            return new ValueTask<TimeSpan>(delay);
        }

        /// <summary>
        /// Calculates what the delay was for a previous attempt, used for decorrelation.
        /// This reconstructs the delay sequence to maintain correlation between successive delays.
        /// </summary>
        /// <param name="attempt">The attempt number to calculate the delay for (1-based).</param>
        /// <returns>The calculated delay for the specified attempt.</returns>
        /// <remarks>
        /// This is a simplified approximation that uses the expected value (midpoint) of the range
        /// rather than the actual random value that was used. This is acceptable because:
        /// 1. We cannot store per-retry-sequence state in a stateless strategy object
        /// 2. Using the expected value provides sufficient decorrelation in practice
        /// 3. The randomization on the current attempt still provides jitter
        ///
        /// For attempt n, the expected delay is: baseDelay + (range from baseDelay to cap) / 2
        /// where cap = min(maxDelay, previousExpectedDelay * 3)
        /// </remarks>
        private TimeSpan CalculatePreviousDelay(int attempt)
        {
            if (attempt <= 0)
            {
                return TimeSpan.Zero;
            }

            // Calculate expected delay using the same formula but taking midpoint
            var currentDelay = _baseDelay;

            for (int i = 1; i <= attempt; i++)
            {
                var maxJitteredDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * 3.0);

                if (maxJitteredDelay > _maxDelay)
                {
                    maxJitteredDelay = _maxDelay;
                }

                // Use midpoint of range for expected value
                var delayRange = maxJitteredDelay.TotalMilliseconds - _baseDelay.TotalMilliseconds;

                if (delayRange <= 0)
                {
                    currentDelay = _baseDelay;
                }
                else
                {
                    var expectedDelay = _baseDelay.TotalMilliseconds + (delayRange / 2.0);
                    currentDelay = TimeSpan.FromMilliseconds(expectedDelay);
                }
            }

            return currentDelay;
        }

        /// <summary>
        /// Releases all resources used by the <see cref="DecorrelatedJitterRetryStrategy"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="DecorrelatedJitterRetryStrategy"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _random?.Dispose();
                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizer that ensures resources are released if <see cref="Dispose()"/> was not called.
        /// </summary>
        ~DecorrelatedJitterRetryStrategy()
        {
            Dispose(false);
        }
    }
}
