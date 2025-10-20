using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Retry.Strategies
{
    /// <summary>
    /// Interface for retry strategies.
    /// </summary>
    public interface IRetryStrategy
    {
        /// <summary>
        /// Determines whether a retry should be attempted.
        /// </summary>
        /// <param name="attempt">The current attempt number (1-based).</param>
        /// <param name="exception">The exception that caused the failure.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>True if a retry should be attempted, false otherwise.</returns>
        ValueTask<bool> ShouldRetryAsync(int attempt, Exception exception, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the delay before the next retry attempt.
        /// </summary>
        /// <param name="attempt">The current attempt number (1-based).</param>
        /// <param name="exception">The exception that caused the failure.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The delay before the next retry attempt.</returns>
        ValueTask<TimeSpan> GetRetryDelayAsync(int attempt, Exception exception, CancellationToken cancellationToken = default);
    }
}