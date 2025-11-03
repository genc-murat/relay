using System;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Defines a contract for calculating retry delays.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface determine how long to wait between retry attempts.
    /// Different strategies can be used depending on the nature of the transient failures:
    /// <list type="bullet">
    /// <item><description><see cref="LinearRetryStrategy"/>: Fixed delay between retries</description></item>
    /// <item><description><see cref="ExponentialBackoffRetryStrategy"/>: Exponentially increasing delay</description></item>
    /// </list>
    /// </remarks>
    public interface IRetryStrategy
    {
        /// <summary>
        /// Calculates the delay before the next retry attempt.
        /// </summary>
        /// <param name="retryAttempt">The retry attempt number (1-based).</param>
        /// <param name="baseDelay">The base delay to use for calculations.</param>
        /// <returns>The calculated delay before the next retry attempt.</returns>
        TimeSpan CalculateDelay(int retryAttempt, TimeSpan baseDelay);
    }
}
