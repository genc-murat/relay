using System;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Retry strategy that uses a fixed delay between retry attempts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This strategy applies the same delay for all retry attempts. It's suitable for scenarios
    /// where transient failures are expected to resolve quickly and consistently, such as:
    /// <list type="bullet">
    /// <item><description>Brief network hiccups</description></item>
    /// <item><description>Momentary resource contention</description></item>
    /// <item><description>Quick database failovers</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Example delay pattern with baseDelay = 100ms:
    /// <list type="bullet">
    /// <item><description>Retry 1: 100ms</description></item>
    /// <item><description>Retry 2: 100ms</description></item>
    /// <item><description>Retry 3: 100ms</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Use <see cref="ExponentialBackoffRetryStrategy"/> instead if you want to reduce load
    /// on the system during extended outages or when dealing with rate limiting.
    /// </para>
    /// </remarks>
    public class LinearRetryStrategy : IRetryStrategy
    {
        /// <summary>
        /// Calculates the delay before the next retry attempt using a fixed delay.
        /// </summary>
        /// <param name="retryAttempt">
        /// The retry attempt number (1-based). This parameter is not used in linear strategy
        /// as the delay is constant.
        /// </param>
        /// <param name="baseDelay">
        /// The fixed delay to use for all retry attempts.
        /// </param>
        /// <returns>The base delay unchanged.</returns>
        /// <remarks>
        /// In linear retry strategy, the delay is constant regardless of the retry attempt number.
        /// The baseDelay is returned as-is for all retry attempts.
        /// </remarks>
        public TimeSpan CalculateDelay(int retryAttempt, TimeSpan baseDelay)
        {
            if (retryAttempt < 1)
                throw new ArgumentOutOfRangeException(nameof(retryAttempt), 
                    "Retry attempt must be greater than or equal to 1.");

            if (baseDelay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(baseDelay), 
                    "Base delay cannot be negative.");

            // Linear strategy: constant delay for all attempts
            return baseDelay;
        }
    }
}
