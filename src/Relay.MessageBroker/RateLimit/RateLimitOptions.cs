namespace Relay.MessageBroker.RateLimit;

/// <summary>
/// Configuration options for rate limiting.
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether rate limiting is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of requests per second.
    /// Default is 1000.
    /// </summary>
    public int RequestsPerSecond { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the rate limiting strategy to use.
    /// Default is TokenBucket.
    /// </summary>
    public RateLimitStrategy Strategy { get; set; } = RateLimitStrategy.TokenBucket;

    /// <summary>
    /// Gets or sets a value indicating whether per-tenant rate limiting is enabled.
    /// </summary>
    public bool EnablePerTenantLimits { get; set; }

    /// <summary>
    /// Gets or sets the per-tenant rate limits.
    /// Key is the tenant ID, value is the requests per second for that tenant.
    /// </summary>
    public Dictionary<string, int>? TenantLimits { get; set; }

    /// <summary>
    /// Gets or sets the default rate limit for unknown tenants when per-tenant limiting is enabled.
    /// Default is 100 requests per second.
    /// </summary>
    public int DefaultTenantLimit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the bucket capacity for token bucket strategy.
    /// This allows for burst handling. Default is 2x the requests per second.
    /// </summary>
    public int? BucketCapacity { get; set; }

    /// <summary>
    /// Gets or sets the window size for sliding window strategy.
    /// Default is 1 second.
    /// </summary>
    public TimeSpan WindowSize { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the cleanup interval for expired entries.
    /// Default is 1 minute.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Validates the rate limit options.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when options are invalid.</exception>
    public void Validate()
    {
        if (Enabled)
        {
            if (RequestsPerSecond <= 0)
            {
                throw new InvalidOperationException("RequestsPerSecond must be greater than 0.");
            }

            if (DefaultTenantLimit <= 0)
            {
                throw new InvalidOperationException("DefaultTenantLimit must be greater than 0.");
            }

            if (WindowSize <= TimeSpan.Zero)
            {
                throw new InvalidOperationException("WindowSize must be greater than zero.");
            }

            if (CleanupInterval <= TimeSpan.Zero)
            {
                throw new InvalidOperationException("CleanupInterval must be greater than zero.");
            }

            if (BucketCapacity.HasValue && BucketCapacity.Value <= 0)
            {
                throw new InvalidOperationException("BucketCapacity must be greater than 0 when specified.");
            }

            if (EnablePerTenantLimits && TenantLimits != null)
            {
                foreach (var limit in TenantLimits.Values)
                {
                    if (limit <= 0)
                    {
                        throw new InvalidOperationException("All tenant limits must be greater than 0.");
                    }
                }
            }
        }
    }
}
