using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Relay.MessageBroker.RateLimit;

/// <summary>
/// Sliding window rate limiter implementation.
/// </summary>
public sealed class SlidingWindowRateLimiter : IRateLimiter, IDisposable
{
    private readonly ConcurrentDictionary<string, SlidingWindow> _windows;
    private readonly RateLimitOptions _options;
    private readonly ILogger<SlidingWindowRateLimiter> _logger;
    private readonly Timer _cleanupTimer;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastAccessTimes;

    private long _totalRequests;
    private long _allowedRequests;
    private long _rejectedRequests;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlidingWindowRateLimiter"/> class.
    /// </summary>
    /// <param name="options">The rate limit options.</param>
    /// <param name="logger">The logger.</param>
    public SlidingWindowRateLimiter(
        IOptions<RateLimitOptions> options,
        ILogger<SlidingWindowRateLimiter> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _options.Validate();

        _windows = new ConcurrentDictionary<string, SlidingWindow>();
        _lastAccessTimes = new ConcurrentDictionary<string, DateTimeOffset>();

        // Start cleanup timer
        _cleanupTimer = new Timer(
            CleanupExpiredWindows,
            null,
            _options.CleanupInterval,
            _options.CleanupInterval);

        _logger.LogInformation(
            "SlidingWindowRateLimiter initialized. Requests per second: {RequestsPerSecond}, Window size: {WindowSize}",
            _options.RequestsPerSecond,
            _options.WindowSize);
    }

    /// <inheritdoc/>
    public ValueTask<RateLimitResult> CheckAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ObjectDisposedException.ThrowIf(_disposed, this);

        Interlocked.Increment(ref _totalRequests);

        // Get or create window for this key
        var window = GetOrCreateWindow(key);

        // Update last access time
        _lastAccessTimes[key] = DateTimeOffset.UtcNow;

        // Try to record the request
        if (window.TryRecordRequest())
        {
            Interlocked.Increment(ref _allowedRequests);

            var remainingRequests = window.GetRemainingRequests();
            var resetAt = window.GetResetTime();

            _logger.LogTrace(
                "Rate limit check passed for key: {Key}. Remaining requests: {RemainingRequests}",
                key,
                remainingRequests);

            return ValueTask.FromResult(RateLimitResult.Allow(remainingRequests, resetAt));
        }

        Interlocked.Increment(ref _rejectedRequests);

        var retryAfter = window.GetRetryAfter();
        var resetTime = window.GetResetTime();

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
            ActiveKeys = _windows.Count
        };
    }

    /// <summary>
    /// Gets or creates a sliding window for the specified key.
    /// </summary>
    private SlidingWindow GetOrCreateWindow(string key)
    {
        return _windows.GetOrAdd(key, _ =>
        {
            var limit = GetLimitForKey(key);

            _logger.LogDebug(
                "Created sliding window for key: {Key}. Limit: {Limit}, Window size: {WindowSize}",
                key,
                limit,
                _options.WindowSize);

            return new SlidingWindow(limit, _options.WindowSize);
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
    /// Cleans up expired windows that haven't been accessed recently.
    /// </summary>
    private void CleanupExpiredWindows(object? state)
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
                _windows.TryRemove(key, out _);
                _lastAccessTimes.TryRemove(key, out _);
            }

            if (keysToRemove.Count > 0)
            {
                _logger.LogDebug(
                    "Cleaned up {Count} expired rate limit windows",
                    keysToRemove.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during rate limit window cleanup");
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

        _logger.LogInformation("Disposing SlidingWindowRateLimiter");

        _cleanupTimer?.Dispose();
        _windows.Clear();
        _lastAccessTimes.Clear();

        _logger.LogInformation("SlidingWindowRateLimiter disposed");
    }
}
