using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.RateLimiting.Interfaces;

namespace Relay.Core.RateLimiting.Implementations;

/// <summary>
/// High-performance sliding window counter rate limiter
/// Uses hybrid approach: combines fixed window counters with sliding window calculation
/// More memory efficient than sliding window log, more accurate than fixed window
/// </summary>
public class SlidingWindowRateLimiter : IRateLimiter
{
    private readonly ILogger<SlidingWindowRateLimiter> _logger;
    private readonly ConcurrentDictionary<string, SlidingWindowState> _windows;
    private readonly int _requestsPerWindow;
    private readonly TimeSpan _windowDuration;
    private readonly Timer _cleanupTimer;

    public SlidingWindowRateLimiter(
        ILogger<SlidingWindowRateLimiter> logger,
        int requestsPerWindow = 100,
        int windowSeconds = 60)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _requestsPerWindow = requestsPerWindow;
        _windowDuration = TimeSpan.FromSeconds(windowSeconds);
        _windows = new ConcurrentDictionary<string, SlidingWindowState>();

        // Cleanup expired windows every minute
        _cleanupTimer = new Timer(CleanupExpiredWindows, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    /// <inheritdoc />
    public ValueTask<bool> IsAllowedAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));

        var now = DateTimeOffset.UtcNow;
        var window = _windows.GetOrAdd(key, _ => new SlidingWindowState());

        lock (window.Lock)
        {
            // Calculate current and previous window boundaries
            var currentWindowStart = GetWindowStart(now);
            var previousWindowStart = currentWindowStart - _windowDuration;

            // Update window if we've moved to a new period
            if (window.CurrentWindowStart < currentWindowStart)
            {
                // Roll over: current becomes previous
                window.PreviousWindowCount = window.CurrentWindowStart == previousWindowStart
                    ? window.CurrentWindowCount
                    : 0;
                window.PreviousWindowStart = window.CurrentWindowStart;
                window.CurrentWindowCount = 0;
                window.CurrentWindowStart = currentWindowStart;
            }

            // Calculate weighted count using sliding window algorithm
            var slidingCount = CalculateSlidingWindowCount(
                now,
                currentWindowStart,
                window.CurrentWindowCount,
                window.PreviousWindowCount);

            if (slidingCount < _requestsPerWindow)
            {
                window.CurrentWindowCount++;
                window.LastAccessTime = now;
                return ValueTask.FromResult(true);
            }

            _logger.LogDebug(
                "Rate limit exceeded for key {Key}. Count: {Count}/{Limit}",
                key,
                Math.Ceiling(slidingCount),
                _requestsPerWindow);

            window.LastAccessTime = now;
            return ValueTask.FromResult(false);
        }
    }

    /// <inheritdoc />
    public ValueTask<TimeSpan> GetRetryAfterAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));

        if (!_windows.TryGetValue(key, out var window))
            return ValueTask.FromResult(TimeSpan.Zero);

        var now = DateTimeOffset.UtcNow;

        lock (window.Lock)
        {
            var currentWindowStart = GetWindowStart(now);
            var currentWindowEnd = currentWindowStart + _windowDuration;

            // Calculate when the window will have capacity
            var slidingCount = CalculateSlidingWindowCount(
                now,
                currentWindowStart,
                window.CurrentWindowCount,
                window.PreviousWindowCount);

            if (slidingCount < _requestsPerWindow)
                return ValueTask.FromResult(TimeSpan.Zero);

            // Estimate retry time based on window progression
            // As we slide into the next window, previous requests drop off
            var timeIntoWindow = now - currentWindowStart;
            var remainingInWindow = _windowDuration - timeIntoWindow;

            // Conservative estimate: wait until we're far enough into next window
            var retryAfter = remainingInWindow + TimeSpan.FromSeconds(1);
            return ValueTask.FromResult(retryAfter);
        }
    }

    /// <summary>
    /// Gets detailed statistics for a key
    /// </summary>
    public SlidingWindowStats GetStats(string key)
    {
        if (!_windows.TryGetValue(key, out var window))
        {
            return new SlidingWindowStats
            {
                CurrentCount = 0,
                Limit = _requestsPerWindow,
                Remaining = _requestsPerWindow,
                WindowDuration = _windowDuration
            };
        }

        var now = DateTimeOffset.UtcNow;

        lock (window.Lock)
        {
            var currentWindowStart = GetWindowStart(now);
            var slidingCount = CalculateSlidingWindowCount(
                now,
                currentWindowStart,
                window.CurrentWindowCount,
                window.PreviousWindowCount);

            return new SlidingWindowStats
            {
                CurrentCount = (int)Math.Ceiling(slidingCount),
                Limit = _requestsPerWindow,
                Remaining = Math.Max(0, _requestsPerWindow - (int)Math.Ceiling(slidingCount)),
                WindowDuration = _windowDuration,
                CurrentWindowRequests = window.CurrentWindowCount,
                PreviousWindowRequests = window.PreviousWindowCount
            };
        }
    }

    /// <summary>
    /// Resets rate limit for a specific key
    /// </summary>
    public void Reset(string key)
    {
        _windows.TryRemove(key, out _);
        _logger.LogInformation("Rate limit reset for key {Key}", key);
    }

    /// <summary>
    /// Calculates the effective request count using sliding window algorithm
    /// Formula: currentCount + previousCount * (1 - timeIntoCurrentWindow / windowDuration)
    /// </summary>
    private double CalculateSlidingWindowCount(
        DateTimeOffset now,
        DateTimeOffset currentWindowStart,
        int currentCount,
        int previousCount)
    {
        var timeIntoWindow = now - currentWindowStart;
        var percentageIntoWindow = timeIntoWindow.TotalSeconds / _windowDuration.TotalSeconds;

        // Weight previous window based on how far we are into current window
        var previousWeight = 1.0 - percentageIntoWindow;
        return currentCount + (previousCount * previousWeight);
    }

    /// <summary>
    /// Gets the start time of the current window
    /// </summary>
    private DateTimeOffset GetWindowStart(DateTimeOffset time)
    {
        var windowTicks = _windowDuration.Ticks;
        var alignedTicks = (time.Ticks / windowTicks) * windowTicks;
        return new DateTimeOffset(alignedTicks, TimeSpan.Zero);
    }

    /// <summary>
    /// Periodically cleans up expired windows to prevent memory leaks
    /// </summary>
    private void CleanupExpiredWindows(object? state)
    {
        var now = DateTimeOffset.UtcNow;
        var expirationThreshold = now - (_windowDuration * 2); // Keep 2 windows of history
        var removed = 0;

        foreach (var kvp in _windows)
        {
            if (kvp.Value.LastAccessTime < expirationThreshold)
            {
                if (_windows.TryRemove(kvp.Key, out _))
                    removed++;
            }
        }

        if (removed > 0)
        {
            _logger.LogDebug("Cleaned up {Count} expired rate limit windows", removed);
        }
    }

    /// <summary>
    /// Internal state for sliding window tracking
    /// </summary>
    private class SlidingWindowState
    {
        public object Lock { get; } = new object();
        public DateTimeOffset CurrentWindowStart { get; set; }
        public DateTimeOffset PreviousWindowStart { get; set; }
        public int CurrentWindowCount { get; set; }
        public int PreviousWindowCount { get; set; }
        public DateTimeOffset LastAccessTime { get; set; }
    }
}
