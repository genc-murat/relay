using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Relay.MessageBroker.RateLimit;

/// <summary>
/// Extension methods for configuring rate limiting services.
/// </summary>
public static class RateLimitServiceCollectionExtensions
{
    /// <summary>
    /// Adds rate limiting to the message broker with the specified strategy.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for rate limit options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageBrokerRateLimit(
        this IServiceCollection services,
        Action<RateLimitOptions>? configure = null)
    {
        // Configure options
        if (configure != null)
        {
            services.Configure(configure);
        }

        // Register rate limiter based on strategy
        services.TryAddSingleton<IRateLimiter>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RateLimitOptions>>();

            return options.Value.Strategy switch
            {
                RateLimitStrategy.TokenBucket => new TokenBucketRateLimiter(
                    options,
                    sp.GetRequiredService<ILogger<TokenBucketRateLimiter>>()),

                RateLimitStrategy.SlidingWindow => new SlidingWindowRateLimiter(
                    options,
                    sp.GetRequiredService<ILogger<SlidingWindowRateLimiter>>()),

                RateLimitStrategy.FixedWindow => throw new NotImplementedException(
                    "FixedWindow rate limiting strategy is not yet implemented. Use TokenBucket or SlidingWindow."),

                _ => throw new InvalidOperationException(
                    $"Unknown rate limiting strategy: {options.Value.Strategy}")
            };
        });

        return services;
    }

    /// <summary>
    /// Adds rate limiting to the message broker with token bucket strategy.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="requestsPerSecond">The maximum number of requests per second.</param>
    /// <param name="bucketCapacity">The bucket capacity for burst handling. Default is 2x requests per second.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageBrokerRateLimit(
        this IServiceCollection services,
        int requestsPerSecond,
        int? bucketCapacity = null)
    {
        return services.AddMessageBrokerRateLimit(options =>
        {
            options.Enabled = true;
            options.Strategy = RateLimitStrategy.TokenBucket;
            options.RequestsPerSecond = requestsPerSecond;
            options.BucketCapacity = bucketCapacity;
        });
    }

    /// <summary>
    /// Adds per-tenant rate limiting to the message broker.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="defaultTenantLimit">The default rate limit for unknown tenants.</param>
    /// <param name="tenantLimits">Optional dictionary of tenant-specific rate limits.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageBrokerPerTenantRateLimit(
        this IServiceCollection services,
        int defaultTenantLimit,
        Dictionary<string, int>? tenantLimits = null)
    {
        return services.AddMessageBrokerRateLimit(options =>
        {
            options.Enabled = true;
            options.Strategy = RateLimitStrategy.TokenBucket;
            options.EnablePerTenantLimits = true;
            options.DefaultTenantLimit = defaultTenantLimit;
            options.TenantLimits = tenantLimits;
        });
    }

    /// <summary>
    /// Decorates the message broker with rate limiting.
    /// This should be called after registering the base message broker.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection DecorateMessageBrokerWithRateLimit(this IServiceCollection services)
    {
        services.Decorate<IMessageBroker, RateLimitMessageBrokerDecorator>();
        return services;
    }
}
