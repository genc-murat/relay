namespace Relay.MessageBroker.RateLimit;

/// <summary>
/// Exception thrown when a rate limit is exceeded.
/// </summary>
public class RateLimitExceededException : Exception
{
    /// <summary>
    /// Gets the duration after which the client should retry.
    /// </summary>
    public TimeSpan RetryAfter { get; }

    /// <summary>
    /// Gets the time when the rate limit window resets.
    /// </summary>
    public DateTimeOffset? ResetAt { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="retryAfter">The duration after which the client should retry.</param>
    /// <param name="resetAt">The time when the rate limit window resets.</param>
    public RateLimitExceededException(string message, TimeSpan retryAfter, DateTimeOffset? resetAt = null)
        : base(message)
    {
        RetryAfter = retryAfter;
        ResetAt = resetAt;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="retryAfter">The duration after which the client should retry.</param>
    /// <param name="resetAt">The time when the rate limit window resets.</param>
    /// <param name="innerException">The inner exception.</param>
    public RateLimitExceededException(
        string message,
        TimeSpan retryAfter,
        DateTimeOffset? resetAt,
        Exception innerException)
        : base(message, innerException)
    {
        RetryAfter = retryAfter;
        ResetAt = resetAt;
    }
}
