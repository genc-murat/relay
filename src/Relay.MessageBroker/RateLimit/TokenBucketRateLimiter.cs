using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Relay.MessageBroker.RateLimit;

/// <summary>
/// Token bucket rate limiter implementation.
/// </summary>
public sealed class TokenBucketRateLimiter : IRateLimiter, IDisposable
{
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets;
    private readonly RateLimitOptions _options;
    private readonly ILogger<TokenBucketRateLimiter> _logger;
    private readonly Timer _cleanupTimer;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastAccessTimes;

    private long _totalRequests;
    private long _allowedRequests;
    private long _rejectedRequests;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenBucketRateLimiter"/> class.
    /// </summary>
    /// <param name="options">The rate limit options.</param>
    /// <param name="logger">The logger.</param>
    public TokenBucketRateLimiter(
        IOptions<RateLimitOptions> options,
        ILogger<TokenBucketRateLimiter> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _options.Validate();

        _buckets = new ConcurrentDictionary<string, TokenBucket>();
        _lastAccessTimes = new ConcurrentDictionary<string, DateTimeOffset>();

        // Start cleanup timer
        _cleanupTimer = new Timer(
            CleanupExpiredBuckets,
            null,
            _options.CleanupInterval,
            _options.CleanupInterval);

        _logger.LogInformation(
            "TokenBucketRateLimiter initialized. Requests per second: {RequestsPerSecond}, Bucket capacity: {Capacity}",
            _options.RequestsPerSecond,
            GetBucketCapacity());
    }

    /// <inheritdoc/>
    public ValueTask<RateLimitResult> CheckAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ObjectDisposedException.ThrowIf(_disposed, this);

        Interlocked.Increment(ref _totalRequests);

        // Get or create bucket for this key
        var bucket = GetOrCreateBucket(key);

        // Update last access time
        _lastAccessTimes[key] = DateTimeOffset.UtcNow;

        // Try to consume a token
        if (bucket.TryConsume())
        {
            Interlocked.Increment(ref _allowedRequests);

            var availableTokens = (int)Math.Floor(bucket.GetAvailableTokens());
            var resetAt = bucket.GetResetTime();

            _logger.LogTrace(
                "Rate limit check passed for key: {Key}. Remaining tokens: {RemainingTokens}",
                key,
                availableTokens);

            return ValueTask.FromResult(RateLimitResult.Allow(availableTokens, resetAt));
        }

        Interlocked.Increment(ref _rejectedRequests);

        var retryAfter = bucket.GetRetryAfter();
        var resetTime = bucket.GetResetTime();

        _logger.LogWarning(
            "Rate limit exceeded for key: {Key}. Retry after: {RetryAfter}",
            key,
            retryAfter);

        return ValueTask.FromResult(RateLimitResult.Reject(retryAfter, resetTime));
    }

    /// <inheritdoc/>
    public RateLimiterMetrics GetMetrics()
    {
        var totalRequests = Interlocked.Read(ref _totalRequests);
        var allowedRequests = Interlocked.Read(ref _allowedRequests);
        var rejectedRequests = Interlocked.Read(ref _rejectedRequests);

        // Calculate current rate (approximate)
        var currentRate = totalRequests > 0 ? (double)allowedRequests / totalRequests * _options.RequestsPerSecond : 0;

        return new RateLimiterMetrics
        {
            TotalRequests = totalRequests,
            AllowedRequests = allowedRequests,
            RejectedRequests = rejectedRequests,
            CurrentRate = currentRate,
            ActiveKeys = _buckets.Count
        };
    }

    /// <summary>
    /// Gets or creates a token bucket for the specified key.
    /// </summary>
    private TokenBucket GetOrCreateBucket(string key)
    {
        return _buckets.GetOrAdd(key, _ =>
        {
            var limit = GetLimitForKey(key);
            var capacity = GetBucketCapacity();

            _logger.LogDebug(
                "Created token bucket for key: {Key}. Limit: {Limit}, Capacity: {Capacity}",
                key,
                limit,
                capacity);

            return new TokenBucket(limit, capacity);
        });
    }

    /// <summary>
    /// Gets the rate limit for the specified key.
    /// </summary>
    private int GetLimitForKey(string key)
    {
        // If per-tenant limits are enabled and we have a specific limit for this key, use it
        if (_options.EnablePerTenantLimits && _options.TenantLimits != null)
        {
            if (_options.TenantLimits.TryGetValue(key, out var tenantLimit))
            {
                return tenantLimit;
            }

            // Use default tenant limit for unknown tenants
            return _options.DefaultTenantLimit;
        }

        // Use global limit
        return _options.RequestsPerSecond;
    }

    /// <summary>
    /// Gets the bucket capacity.
    /// </summary>
    private int GetBucketCapacity()
    {
        // If bucket capacity is specified, use it; otherwise, use 2x the requests per second
        return _options.BucketCapacity ?? (_options.RequestsPerSecond * 2);
    }

    /// <summary>
    /// Cleans up expired buckets that haven't been accessed recently.
    /// </summary>
    private void CleanupExpiredBuckets(object? state)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var expirationThreshold = now.Subtract(_options.CleanupInterval * 2);
            var keysToRemove = new List<string>();

            foreach (var kvp in _lastAccessTimes)
            {
                if (kvp.Value < expirationThreshold)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _buckets.TryRemove(key, out _);
                _lastAccessTimes.TryRemove(key, out _);
            }

            if (keysToRemove.Count > 0)
            {
                _logger.LogDebug(
                    "Cleaned up {Count} expired rate limit buckets",
                    keysToRemove.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during rate limit bucket cleanup");
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _logger.LogInformation("Disposing TokenBucketRateLimiter");

        _cleanupTimer?.Dispose();
        _buckets.Clear();
        _lastAccessTimes.Clear();

        _logger.LogInformation("TokenBucketRateLimiter disposed");
    }
}
