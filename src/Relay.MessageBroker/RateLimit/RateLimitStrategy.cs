namespace Relay.MessageBroker.RateLimit;

/// <summary>
/// Defines the rate limiting strategy to use.
/// </summary>
public enum RateLimitStrategy
{
    /// <summary>
    /// Fixed window rate limiting - counts requests in fixed time windows.
    /// </summary>
    FixedWindow,

    /// <summary>
    /// Sliding window rate limiting - counts requests in a sliding time window.
    /// </summary>
    SlidingWindow,

    /// <summary>
    /// Token bucket rate limiting - allows bursts while maintaining average rate.
    /// </summary>
    TokenBucket
}
