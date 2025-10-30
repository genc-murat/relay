namespace Relay.MessageBroker.RateLimit;

/// <summary>
/// Metrics for rate limiter operations.
/// </summary>
public sealed class RateLimiterMetrics
{
    /// <summary>
    /// Gets the total number of requests checked.
    /// </summary>
    public long TotalRequests { get; init; }

    /// <summary>
    /// Gets the number of requests that were allowed.
    /// </summary>
    public long AllowedRequests { get; init; }

    /// <summary>
    /// Gets the number of requests that were rejected.
    /// </summary>
    public long RejectedRequests { get; init; }

    /// <summary>
    /// Gets the current rate (requests per second).
    /// </summary>
    public double CurrentRate { get; init; }

    /// <summary>
    /// Gets the number of active rate limit keys being tracked.
    /// </summary>
    public int ActiveKeys { get; init; }

    /// <summary>
    /// Gets the rejection rate (percentage of requests rejected).
    /// </summary>
    public double RejectionRate => TotalRequests > 0 ? (double)RejectedRequests / TotalRequests * 100 : 0;
}
