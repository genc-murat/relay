using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.RateLimiting
{
    /// <summary>
    /// Interface for rate limiting strategies.
    /// </summary>
    public interface IRateLimiter
    {
        /// <summary>
        /// Checks if a request is allowed based on rate limiting rules.
        /// </summary>
        /// <param name="key">The key to identify the rate limiting context (e.g., IP address, user ID, handler type).</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>True if the request is allowed, false otherwise.</returns>
        ValueTask<bool> IsAllowedAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the retry after time for a rate limited request.
        /// </summary>
        /// <param name="key">The key to identify the rate limiting context.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The time after which the request can be retried.</returns>
        ValueTask<TimeSpan> GetRetryAfterAsync(string key, CancellationToken cancellationToken = default);
    }
}