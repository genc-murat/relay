namespace Relay.MessageBroker.RateLimit;

/// <summary>
/// Represents the result of a rate limit check.
/// </summary>
public sealed class RateLimitResult
{
    /// <summary>
    /// Gets a value indicating whether the request is allowed.
    /// </summary>
    public bool Allowed { get; init; }

    /// <summary>
    /// Gets the duration after which the client should retry if the request was rejected.
    /// </summary>
    public TimeSpan? RetryAfter { get; init; }

    /// <summary>
    /// Gets the remaining requests allowed in the current window.
    /// </summary>
    public int? RemainingRequests { get; init; }

    /// <summary>
    /// Gets the time when the rate limit window resets.
    /// </summary>
    public DateTimeOffset? ResetAt { get; init; }

    /// <summary>
    /// Creates a result indicating the request is allowed.
    /// </summary>
    /// <param name="remainingRequests">The remaining requests allowed.</param>
    /// <param name="resetAt">The time when the rate limit window resets.</param>
    /// <returns>A rate limit result indicating the request is allowed.</returns>
    public static RateLimitResult Allow(int? remainingRequests = null, DateTimeOffset? resetAt = null)
    {
        return new RateLimitResult
        {
            Allowed = true,
            RemainingRequests = remainingRequests,
            ResetAt = resetAt
        };
    }

    /// <summary>
    /// Creates a result indicating the request is rejected.
    /// </summary>
    /// <param name="retryAfter">The duration after which the client should retry.</param>
    /// <param name="resetAt">The time when the rate limit window resets.</param>
    /// <returns>A rate limit result indicating the request is rejected.</returns>
    public static RateLimitResult Reject(TimeSpan retryAfter, DateTimeOffset? resetAt = null)
    {
        return new RateLimitResult
        {
            Allowed = false,
            RetryAfter = retryAfter,
            RemainingRequests = 0,
            ResetAt = resetAt
        };
    }
}
