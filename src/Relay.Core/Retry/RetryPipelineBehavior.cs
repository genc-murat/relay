 using Microsoft.Extensions.Logging;
 using Microsoft.Extensions.Options;
 using Relay.Core.Configuration.Options.Core;
 using Relay.Core.Configuration.Options.Retry;
 using Relay.Core.Retry.Strategies;
using Relay.Core.Contracts.Pipeline;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Retry
{
    /// <summary>
    /// A pipeline behavior that implements retry logic for requests.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    public class RetryPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly ILogger<RetryPipelineBehavior<TRequest, TResponse>> _logger;
        private readonly IOptions<RelayOptions> _options;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _handlerKey;

        public RetryPipelineBehavior(
            ILogger<RetryPipelineBehavior<TRequest, TResponse>> logger,
            IOptions<RelayOptions> options,
            IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _handlerKey = typeof(TRequest).FullName ?? typeof(TRequest).Name;
        }

        public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // Get retry configuration
            var retryOptions = GetRetryOptions();
            var retryAttribute = typeof(TRequest).GetCustomAttribute<RetryAttribute>();

            // Check if retry is enabled for this request
            if (!IsRetryEnabled(retryOptions, retryAttribute))
            {
                return await next();
            }

            // Get retry parameters
            var (maxAttempts, retryDelay, retryStrategy) = GetRetryParameters(retryOptions, retryAttribute);

            var exceptions = new List<Exception>();

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    // For retry attempts (not the first attempt), apply delay
                    if (attempt > 0)
                    {
                        var delay = await retryStrategy.GetRetryDelayAsync(attempt, exceptions[attempt - 1], cancellationToken);
                        await Task.Delay(delay, cancellationToken);

                        _logger.LogInformation("Retrying request {RequestType}, attempt {Attempt}/{MaxAttempts}",
                            typeof(TRequest).Name, attempt + 1, maxAttempts);
                    }

                    return await next();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    _logger.LogWarning(ex, "Request {RequestType} failed on attempt {Attempt}/{MaxAttempts}",
                        typeof(TRequest).Name, attempt + 1, maxAttempts);

                    // If this is the last attempt, break
                    if (attempt == maxAttempts - 1)
                    {
                        break;
                    }

                    // Check if we should retry
                    if (!await retryStrategy.ShouldRetryAsync(attempt + 1, ex, cancellationToken))
                    {
                        break;
                    }
                }
            }

            // All retry attempts exhausted
            _logger.LogError("All retry attempts exhausted for request {RequestType}", typeof(TRequest).Name);

            if (retryOptions.ThrowOnRetryExhausted)
            {
                throw new RetryExhaustedException(exceptions);
            }

            // If not throwing, rethrow the last exception
            throw exceptions[exceptions.Count - 1];
        }

        private RetryOptions GetRetryOptions()
        {
            // Check for handler-specific overrides
            if (_options.Value.RetryOverrides.TryGetValue(_handlerKey, out var handlerOptions))
            {
                return handlerOptions;
            }

            // Return default options
            return _options.Value.DefaultRetryOptions;
        }

        private static bool IsRetryEnabled(RetryOptions retryOptions, RetryAttribute? retryAttribute)
        {
            // If retry is explicitly disabled globally, return false
            if (!retryOptions.EnableAutomaticRetry && retryAttribute == null)
            {
                return false;
            }

            // If retry is enabled globally or explicitly enabled with RetryAttribute, return true
            return retryOptions.EnableAutomaticRetry || retryAttribute != null;
        }

        private (int maxAttempts, TimeSpan retryDelay, IRetryStrategy retryStrategy) GetRetryParameters(
            RetryOptions retryOptions, RetryAttribute? retryAttribute)
        {
            if (retryAttribute != null)
            {
                // Use parameters from attribute
                var maxAttempts = retryAttribute.MaxRetryAttempts;
                var retryDelay = TimeSpan.FromMilliseconds(retryAttribute.RetryDelayMilliseconds);

                // Create retry strategy
                IRetryStrategy retryStrategy;
                if (retryAttribute.RetryStrategyType != null)
                {
                    retryStrategy = (_serviceProvider.GetService(retryAttribute.RetryStrategyType) as IRetryStrategy) ??
                                   new LinearRetryStrategy(retryDelay);
                }
                else
                {
                    retryStrategy = new LinearRetryStrategy(retryDelay);
                }

                return (maxAttempts, retryDelay, retryStrategy);
            }

            // Use default parameters
            var defaultMaxAttempts = retryOptions.DefaultMaxRetryAttempts;
            var defaultRetryDelay = TimeSpan.FromMilliseconds(retryOptions.DefaultRetryDelayMilliseconds);

            // Create retry strategy based on configuration
            IRetryStrategy defaultRetryStrategy = retryOptions.DefaultRetryStrategy.ToLowerInvariant() switch
            {
                "exponential" => new ExponentialBackoffRetryStrategy(
                    defaultRetryDelay,
                    TimeSpan.FromMilliseconds(defaultRetryDelay.TotalMilliseconds * 10)),
                "circuitbreaker" => new CircuitBreakerRetryStrategy(
                    new LinearRetryStrategy(defaultRetryDelay)),
                _ => new LinearRetryStrategy(defaultRetryDelay)
            };

            return (defaultMaxAttempts, defaultRetryDelay, defaultRetryStrategy);
        }
    }
}