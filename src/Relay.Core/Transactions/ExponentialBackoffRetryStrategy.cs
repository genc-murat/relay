using System;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Retry strategy that uses exponentially increasing delays between retry attempts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This strategy increases the delay exponentially with each retry attempt, which helps to:
    /// <list type="bullet">
    /// <item><description>Reduce load on the system during extended outages</description></item>
    /// <item><description>Give the system more time to recover between attempts</description></item>
    /// <item><description>Avoid overwhelming a recovering system with immediate retries</description></item>
    /// <item><description>Handle rate limiting scenarios gracefully</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The delay is calculated as: baseDelay * 2^(retryAttempt - 1)
    /// </para>
    /// <para>
    /// Example delay pattern with baseDelay = 100ms:
    /// <list type="bullet">
    /// <item><description>Retry 1: 100ms (100 * 2^0)</description></item>
    /// <item><description>Retry 2: 200ms (100 * 2^1)</description></item>
    /// <item><description>Retry 3: 400ms (100 * 2^2)</description></item>
    /// <item><description>Retry 4: 800ms (100 * 2^3)</description></item>
    /// <item><description>Retry 5: 1600ms (100 * 2^4)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This is the recommended strategy for most scenarios as it balances quick recovery
    /// for transient issues with reduced load during extended outages.
    /// </para>
    /// </remarks>
    public class ExponentialBackoffRetryStrategy : IRetryStrategy
    {
        private readonly TimeSpan _maxDelay;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExponentialBackoffRetryStrategy"/> class.
        /// </summary>
        /// <param name="maxDelay">
        /// The maximum delay to cap the exponential growth. If null, no maximum is applied.
        /// This prevents delays from growing too large for high retry counts.
        /// Default is 30 seconds.
        /// </param>
        public ExponentialBackoffRetryStrategy(TimeSpan? maxDelay = null)
        {
            _maxDelay = maxDelay ?? TimeSpan.FromSeconds(30);
            
            if (_maxDelay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(maxDelay), 
                    "Maximum delay cannot be negative.");
        }

        /// <summary>
        /// Calculates the delay before the next retry attempt using exponential backoff.
        /// </summary>
        /// <param name="retryAttempt">
        /// The retry attempt number (1-based). For example, 1 for the first retry, 2 for the second, etc.
        /// </param>
        /// <param name="baseDelay">
        /// The base delay that will be multiplied exponentially based on the retry attempt.
        /// </param>
        /// <returns>
        /// The calculated delay, which is baseDelay * 2^(retryAttempt - 1), capped at the maximum delay.
        /// </returns>
        /// <remarks>
        /// The delay grows exponentially with each retry attempt. A maximum delay cap is applied
        /// to prevent excessively long delays for high retry counts.
        /// </remarks>
        public TimeSpan CalculateDelay(int retryAttempt, TimeSpan baseDelay)
        {
            if (retryAttempt < 1)
                throw new ArgumentOutOfRangeException(nameof(retryAttempt), 
                    "Retry attempt must be greater than or equal to 1.");

            if (baseDelay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(baseDelay), 
                    "Base delay cannot be negative.");

            // Calculate exponential delay: baseDelay * 2^(retryAttempt - 1)
            // Use retryAttempt - 1 so that first retry uses baseDelay as-is
            var multiplier = Math.Pow(2, retryAttempt - 1);
            var calculatedDelay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * multiplier);

            // Cap at maximum delay to prevent excessive waits
            return calculatedDelay > _maxDelay ? _maxDelay : calculatedDelay;
        }
    }
}
