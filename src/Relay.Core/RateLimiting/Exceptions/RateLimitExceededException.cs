using System;

namespace Relay.Core.RateLimiting.Exceptions
{
    /// <summary>
    /// Exception thrown when a request is rate limited.
    /// </summary>
    public class RateLimitExceededException : Exception
    {
        /// <summary>
        /// Gets the time after which the request can be retried.
        /// </summary>
        public TimeSpan RetryAfter { get; }

        /// <summary>
        /// Gets the rate limiting key.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class.
        /// </summary>
        /// <param name="key">The rate limiting key.</param>
        /// <param name="retryAfter">The time after which the request can be retried.</param>
        public RateLimitExceededException(string key, TimeSpan retryAfter)
            : base($"Rate limit exceeded for key '{key}'. Retry after {retryAfter.TotalSeconds} seconds.")
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            RetryAfter = retryAfter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class.
        /// </summary>
        /// <param name="key">The rate limiting key.</param>
        /// <param name="retryAfter">The time after which the request can be retried.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public RateLimitExceededException(string key, TimeSpan retryAfter, Exception innerException)
            : base($"Rate limit exceeded for key '{key}'. Retry after {retryAfter.TotalSeconds} seconds.", innerException)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            RetryAfter = retryAfter;
        }
    }
}