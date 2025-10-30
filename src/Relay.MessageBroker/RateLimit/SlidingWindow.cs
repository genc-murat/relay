namespace Relay.MessageBroker.RateLimit;

/// <summary>
/// Represents a sliding window for rate limiting.
/// </summary>
internal sealed class SlidingWindow
{
    private readonly object _lock = new();
    private readonly Queue<DateTimeOffset> _timestamps;
    private readonly int _limit;
    private readonly TimeSpan _windowSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlidingWindow"/> class.
    /// </summary>
    /// <param name="limit">The maximum number of requests allowed in the window.</param>
    /// <param name="windowSize">The size of the sliding window.</param>
    public SlidingWindow(int limit, TimeSpan windowSize)
    {
        _limit = limit;
        _windowSize = windowSize;
        _timestamps = new Queue<DateTimeOffset>();
    }

    /// <summary>
    /// Attempts to record a request in the sliding window.
    /// </summary>
    /// <returns>True if the request is allowed; otherwise, false.</returns>
    public bool TryRecordRequest()
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            RemoveExpiredTimestamps(now);

            if (_timestamps.Count < _limit)
            {
                _timestamps.Enqueue(now);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Gets the time after which the client should retry.
    /// </summary>
    /// <returns>The duration after which the client should retry.</returns>
    public TimeSpan GetRetryAfter()
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            RemoveExpiredTimestamps(now);

            if (_timestamps.Count < _limit)
            {
                return TimeSpan.Zero;
            }

            // The oldest timestamp will expire first
            var oldestTimestamp = _timestamps.Peek();
            var expirationTime = oldestTimestamp.Add(_windowSize);
            var retryAfter = expirationTime - now;

            return retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Gets the number of remaining requests in the current window.
    /// </summary>
    /// <returns>The number of remaining requests.</returns>
    public int GetRemainingRequests()
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            RemoveExpiredTimestamps(now);

            return Math.Max(0, _limit - _timestamps.Count);
        }
    }

    /// <summary>
    /// Gets the time when the window will reset (when the oldest request expires).
    /// </summary>
    /// <returns>The time when the window will reset.</returns>
    public DateTimeOffset GetResetTime()
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            RemoveExpiredTimestamps(now);

            if (_timestamps.Count == 0)
            {
                return now;
            }

            var oldestTimestamp = _timestamps.Peek();
            return oldestTimestamp.Add(_windowSize);
        }
    }

    /// <summary>
    /// Gets the current number of requests in the window.
    /// </summary>
    /// <returns>The current number of requests.</returns>
    public int GetCurrentCount()
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            RemoveExpiredTimestamps(now);
            return _timestamps.Count;
        }
    }

    /// <summary>
    /// Removes expired timestamps from the queue.
    /// </summary>
    private void RemoveExpiredTimestamps(DateTimeOffset now)
    {
        var cutoffTime = now.Subtract(_windowSize);

        while (_timestamps.Count > 0 && _timestamps.Peek() < cutoffTime)
        {
            _timestamps.Dequeue();
        }
    }
}
