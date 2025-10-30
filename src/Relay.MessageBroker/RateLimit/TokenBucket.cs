namespace Relay.MessageBroker.RateLimit;

/// <summary>
/// Represents a token bucket for rate limiting.
/// </summary>
internal sealed class TokenBucket
{
    private readonly object _lock = new();
    private double _tokens;
    private DateTimeOffset _lastRefill;
    private readonly double _refillRate;
    private readonly double _capacity;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenBucket"/> class.
    /// </summary>
    /// <param name="refillRate">The rate at which tokens are refilled (tokens per second).</param>
    /// <param name="capacity">The maximum number of tokens the bucket can hold.</param>
    public TokenBucket(double refillRate, double capacity)
    {
        _refillRate = refillRate;
        _capacity = capacity;
        _tokens = capacity; // Start with a full bucket
        _lastRefill = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Attempts to consume a token from the bucket.
    /// </summary>
    /// <param name="tokensToConsume">The number of tokens to consume. Default is 1.</param>
    /// <returns>True if the token was consumed; otherwise, false.</returns>
    public bool TryConsume(double tokensToConsume = 1.0)
    {
        lock (_lock)
        {
            Refill();

            if (_tokens >= tokensToConsume)
            {
                _tokens -= tokensToConsume;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Gets the time after which the client should retry.
    /// </summary>
    /// <param name="tokensNeeded">The number of tokens needed.</param>
    /// <returns>The duration after which the client should retry.</returns>
    public TimeSpan GetRetryAfter(double tokensNeeded = 1.0)
    {
        lock (_lock)
        {
            Refill();

            if (_tokens >= tokensNeeded)
            {
                return TimeSpan.Zero;
            }

            var tokensShortfall = tokensNeeded - _tokens;
            var secondsToWait = tokensShortfall / _refillRate;

            return TimeSpan.FromSeconds(Math.Ceiling(secondsToWait));
        }
    }

    /// <summary>
    /// Gets the number of available tokens.
    /// </summary>
    /// <returns>The number of available tokens.</returns>
    public double GetAvailableTokens()
    {
        lock (_lock)
        {
            Refill();
            return _tokens;
        }
    }

    /// <summary>
    /// Gets the time when the bucket will be full again.
    /// </summary>
    /// <returns>The time when the bucket will be full.</returns>
    public DateTimeOffset GetResetTime()
    {
        lock (_lock)
        {
            Refill();

            if (_tokens >= _capacity)
            {
                return DateTimeOffset.UtcNow;
            }

            var tokensNeeded = _capacity - _tokens;
            var secondsToFull = tokensNeeded / _refillRate;

            return DateTimeOffset.UtcNow.AddSeconds(secondsToFull);
        }
    }

    /// <summary>
    /// Refills the bucket based on elapsed time.
    /// </summary>
    private void Refill()
    {
        var now = DateTimeOffset.UtcNow;
        var elapsed = (now - _lastRefill).TotalSeconds;

        if (elapsed > 0)
        {
            var tokensToAdd = elapsed * _refillRate;
            _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
            _lastRefill = now;
        }
    }
}
