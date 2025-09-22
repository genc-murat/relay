namespace Relay.Core.Configuration
{
    /// <summary>
    /// Configuration options for rate limiting.
    /// </summary>
    public class RateLimitingOptions
    {
        /// <summary>
        /// Gets or sets whether to enable automatic rate limiting for all requests.
        /// </summary>
        public bool EnableAutomaticRateLimiting { get; set; } = false;

        /// <summary>
        /// Gets or sets the default maximum number of requests allowed per time window.
        /// </summary>
        public int DefaultRequestsPerWindow { get; set; } = 100;

        /// <summary>
        /// Gets or sets the default time window in seconds.
        /// </summary>
        public int DefaultWindowSeconds { get; set; } = 60;

        /// <summary>
        /// Gets or sets the default rate limiting key (e.g., "IP", "User", "Global").
        /// </summary>
        public string DefaultKey { get; set; } = "Global";

        /// <summary>
        /// Gets or sets whether to throw an exception when rate limit is exceeded.
        /// </summary>
        public bool ThrowOnRateLimitExceeded { get; set; } = true;

        /// <summary>
        /// Gets or sets the default order for rate limiting pipeline behaviors.
        /// </summary>
        public int DefaultOrder { get; set; } = -2000; // Run very early in the pipeline
    }
}