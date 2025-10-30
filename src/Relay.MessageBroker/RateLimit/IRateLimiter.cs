namespace Relay.MessageBroker.RateLimit;

/// <summary>
/// Interface for rate limiting operations.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Checks if a request is allowed based on the rate limit.
    /// </summary>
    /// <param name="key">The key to identify the rate limit bucket (e.g., tenant ID, user ID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A rate limit result indicating whether the request is allowed.</returns>
    ValueTask<RateLimitResult> CheckAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current rate limiter metrics.
    /// </summary>
    /// <returns>The rate limiter metrics.</returns>
    RateLimiterMetrics GetMetrics();
}
