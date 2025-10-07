using System;

namespace Relay.Core.RateLimiting.Attributes
{
    /// <summary>
    /// Attribute to mark handlers or requests that should be rate limited.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class RateLimitAttribute : Attribute
    {
        /// <summary>
        /// Gets the maximum number of requests allowed per time window.
        /// </summary>
        public int RequestsPerWindow { get; }

        /// <summary>
        /// Gets the time window in seconds.
        /// </summary>
        public int WindowSeconds { get; }

        /// <summary>
        /// Gets the rate limiting key (e.g., "IP", "User", "Global").
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RateLimitAttribute"/> class.
        /// </summary>
        /// <param name="requestsPerWindow">The maximum number of requests allowed per time window.</param>
        /// <param name="windowSeconds">The time window in seconds.</param>
        /// <param name="key">The rate limiting key (e.g., "IP", "User", "Global").</param>
        public RateLimitAttribute(int requestsPerWindow, int windowSeconds, string key = "Global")
        {
            if (requestsPerWindow <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestsPerWindow), "Requests per window must be a positive number.");
            }

            if (windowSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(windowSeconds), "Window seconds must be a positive number.");
            }

            RequestsPerWindow = requestsPerWindow;
            WindowSeconds = windowSeconds;
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }
    }
}