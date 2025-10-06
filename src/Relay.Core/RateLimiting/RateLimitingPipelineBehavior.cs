using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Configuration;
using Relay.Core.Configuration.Options;
using Relay.Core.Contracts.Pipeline;

namespace Relay.Core.RateLimiting
{
    /// <summary>
    /// A pipeline behavior that implements rate limiting for requests.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    public class RateLimitingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IRateLimiter _rateLimiter;
        private readonly ILogger<RateLimitingPipelineBehavior<TRequest, TResponse>> _logger;
        private readonly IOptions<RelayOptions> _options;
        private readonly string _handlerKey;

        public RateLimitingPipelineBehavior(
            IRateLimiter rateLimiter,
            ILogger<RateLimitingPipelineBehavior<TRequest, TResponse>> logger,
            IOptions<RelayOptions> options)
        {
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _handlerKey = typeof(TRequest).FullName ?? typeof(TRequest).Name;
        }

        public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // Get rate limiting configuration
            var rateLimitingOptions = GetRateLimitingOptions();
            var rateLimitAttribute = typeof(TRequest).GetCustomAttribute<RateLimitAttribute>();

            // Check if rate limiting is enabled for this request
            if (!IsRateLimitingEnabled(rateLimitingOptions, rateLimitAttribute))
            {
                return await next();
            }

            // Generate rate limiting key
            var key = GenerateRateLimitKey(request, rateLimitingOptions, rateLimitAttribute);

            // Check if request is allowed
            if (!await _rateLimiter.IsAllowedAsync(key, cancellationToken))
            {
                _logger.LogWarning("Rate limit exceeded for key: {RateLimitKey}", key);

                if (rateLimitingOptions.ThrowOnRateLimitExceeded)
                {
                    var retryAfter = await _rateLimiter.GetRetryAfterAsync(key, cancellationToken);
                    throw new RateLimitExceededException(key, retryAfter);
                }

                // If not throwing, we might want to return a default response or handle it differently
                // For now, we'll just continue to the next handler
            }

            return await next();
        }

        private RateLimitingOptions GetRateLimitingOptions()
        {
            // Check for handler-specific overrides
            if (_options.Value.RateLimitingOverrides.TryGetValue(_handlerKey, out var handlerOptions))
            {
                return handlerOptions;
            }

            // Return default options
            return _options.Value.DefaultRateLimitingOptions;
        }

        private static bool IsRateLimitingEnabled(RateLimitingOptions rateLimitingOptions, RateLimitAttribute? rateLimitAttribute)
        {
            // If rate limiting is explicitly disabled globally, return false
            if (!rateLimitingOptions.EnableAutomaticRateLimiting && rateLimitAttribute == null)
            {
                return false;
            }

            // If rate limiting is enabled globally or explicitly enabled with RateLimitAttribute, return true
            return rateLimitingOptions.EnableAutomaticRateLimiting || rateLimitAttribute != null;
        }

        private static string GenerateRateLimitKey(TRequest request, RateLimitingOptions rateLimitingOptions, RateLimitAttribute? rateLimitAttribute)
        {
            // Get the key from the attribute or use the default
            var keyType = rateLimitAttribute?.Key ?? rateLimitingOptions.DefaultKey;

            // Generate key based on the key type
            return keyType switch
            {
                "Global" => "Global",
                "Type" => typeof(TRequest).FullName ?? typeof(TRequest).Name,
                _ => $"{keyType}:{typeof(TRequest).FullName ?? typeof(TRequest).Name}"
            };
        }
    }
}