using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Relay.MessageBroker.RateLimit;

/// <summary>
/// Decorator that adds rate limiting capabilities to an IMessageBroker implementation.
/// </summary>
public sealed class RateLimitMessageBrokerDecorator : IMessageBroker, IAsyncDisposable
{
    private readonly IMessageBroker _innerBroker;
    private readonly IRateLimiter _rateLimiter;
    private readonly RateLimitOptions _options;
    private readonly ILogger<RateLimitMessageBrokerDecorator> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitMessageBrokerDecorator"/> class.
    /// </summary>
    /// <param name="innerBroker">The inner message broker to decorate.</param>
    /// <param name="rateLimiter">The rate limiter.</param>
    /// <param name="options">The rate limit options.</param>
    /// <param name="logger">The logger.</param>
    public RateLimitMessageBrokerDecorator(
        IMessageBroker innerBroker,
        IRateLimiter rateLimiter,
        IOptions<RateLimitOptions> options,
        ILogger<RateLimitMessageBrokerDecorator> logger)
    {
        _innerBroker = innerBroker ?? throw new ArgumentNullException(nameof(innerBroker));
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _options.Validate();

        _logger.LogInformation(
            "RateLimitMessageBrokerDecorator initialized. Rate limiting enabled: {Enabled}, Strategy: {Strategy}",
            _options.Enabled,
            _options.Strategy);
    }

    /// <inheritdoc/>
    public async ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ObjectDisposedException.ThrowIf(_disposed, this);

        // If rate limiting is disabled, publish directly
        if (!_options.Enabled)
        {
            await _innerBroker.PublishAsync(message, options, cancellationToken);
            return;
        }

        try
        {
            // Extract rate limit key (tenant ID or default)
            var rateLimitKey = ExtractRateLimitKey(options?.Headers);

            // Check rate limit
            var result = await _rateLimiter.CheckAsync(rateLimitKey, cancellationToken);

            if (!result.Allowed)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for key: {Key}, message type: {MessageType}. Retry after: {RetryAfter}",
                    rateLimitKey,
                    typeof(TMessage).Name,
                    result.RetryAfter);

                // Add rate limit headers to the exception for client information
                var retryAfter = result.RetryAfter ?? TimeSpan.Zero;
                throw new RateLimitExceededException(
                    $"Rate limit exceeded for key '{rateLimitKey}'. Retry after {retryAfter.TotalSeconds:F2} seconds.",
                    retryAfter,
                    result.ResetAt);
            }

            _logger.LogTrace(
                "Rate limit check passed for key: {Key}, message type: {MessageType}. Remaining: {Remaining}",
                rateLimitKey,
                typeof(TMessage).Name,
                result.RemainingRequests);

            // Add rate limit information to headers
            if (options?.Headers != null)
            {
                options.Headers["X-RateLimit-Remaining"] = result.RemainingRequests ?? 0;
                if (result.ResetAt.HasValue)
                {
                    options.Headers["X-RateLimit-Reset"] = result.ResetAt.Value.ToUnixTimeSeconds();
                }
            }

            // Publish the message
            await _innerBroker.PublishAsync(message, options, cancellationToken);
        }
        catch (RateLimitExceededException)
        {
            // Re-throw rate limit exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error during rate-limited publish of message type {MessageType}",
                typeof(TMessage).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Subscription is not affected by rate limiting on publish side
        // Rate limiting for consumption would be handled differently (backpressure)
        return _innerBroker.SubscribeAsync(handler, options, cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        return _innerBroker.StartAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        return _innerBroker.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the rate limiter metrics.
    /// </summary>
    /// <returns>The rate limiter metrics.</returns>
    public RateLimiterMetrics GetMetrics()
    {
        return _rateLimiter.GetMetrics();
    }

    /// <summary>
    /// Extracts the rate limit key from message headers.
    /// </summary>
    /// <param name="headers">The message headers.</param>
    /// <returns>The rate limit key.</returns>
    private string ExtractRateLimitKey(Dictionary<string, object>? headers)
    {
        if (_options.EnablePerTenantLimits)
        {
            // Extract tenant ID from headers or authentication context
            return TenantIdExtractor.Extract(headers, "default");
        }

        // Use a global key for non-tenant-specific rate limiting
        return "global";
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _logger.LogInformation("Disposing RateLimitMessageBrokerDecorator");

        // Dispose rate limiter if it's disposable
        if (_rateLimiter is IDisposable disposable)
        {
            disposable.Dispose();
        }

        // Dispose inner broker if it's disposable
        if (_innerBroker is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }

        _logger.LogInformation("RateLimitMessageBrokerDecorator disposed");
    }
}
