using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Relay.Core.Resilience
{
    /// <summary>
    /// Bulkhead isolation pattern implementation for request handlers.
    /// Limits concurrent executions to prevent one handler from consuming all resources.
    /// </summary>
    public class BulkheadPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<BulkheadPipelineBehavior<TRequest, TResponse>> _logger;
        private readonly BulkheadOptions _options;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();

        public BulkheadPipelineBehavior(
            ILogger<BulkheadPipelineBehavior<TRequest, TResponse>> logger,
            IOptions<BulkheadOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestType = typeof(TRequest).Name;
            var maxConcurrency = _options.GetMaxConcurrency(requestType);
            
            if (maxConcurrency <= 0)
            {
                // No bulkhead configured, proceed normally
                return await next();
            }

            var semaphore = _semaphores.GetOrAdd(requestType, _ => new SemaphoreSlim(maxConcurrency, maxConcurrency));

            var acquired = await semaphore.WaitAsync(_options.MaxWaitTime, cancellationToken);
            if (!acquired)
            {
                _logger.LogWarning("Bulkhead rejection for {RequestType}: max concurrency ({MaxConcurrency}) exceeded", 
                    requestType, maxConcurrency);
                throw new BulkheadRejectedException(requestType, maxConcurrency);
            }

            try
            {
                _logger.LogDebug("Bulkhead acquired for {RequestType} (Available: {Available})", 
                    requestType, semaphore.CurrentCount);
                
                return await next();
            }
            finally
            {
                semaphore.Release();
                _logger.LogDebug("Bulkhead released for {RequestType} (Available: {Available})", 
                    requestType, semaphore.CurrentCount);
            }
        }
    }

    /// <summary>
    /// Configuration options for bulkhead isolation.
    /// </summary>
    public class BulkheadOptions
    {
        private readonly ConcurrentDictionary<string, int> _requestTypeLimits = new();

        /// <summary>
        /// Default maximum concurrency for all request types.
        /// </summary>
        public int DefaultMaxConcurrency { get; set; } = Environment.ProcessorCount * 2;

        /// <summary>
        /// Maximum time to wait for bulkhead access.
        /// </summary>
        public TimeSpan MaxWaitTime { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Sets maximum concurrency for a specific request type.
        /// </summary>
        public BulkheadOptions SetMaxConcurrency<TRequest>(int maxConcurrency)
        {
            _requestTypeLimits[typeof(TRequest).Name] = maxConcurrency;
            return this;
        }

        /// <summary>
        /// Sets maximum concurrency for a specific request type by name.
        /// </summary>
        public BulkheadOptions SetMaxConcurrency(string requestTypeName, int maxConcurrency)
        {
            _requestTypeLimits[requestTypeName] = maxConcurrency;
            return this;
        }

        /// <summary>
        /// Gets the maximum concurrency for a request type.
        /// </summary>
        public int GetMaxConcurrency(string requestTypeName)
        {
            return _requestTypeLimits.TryGetValue(requestTypeName, out var limit) ? limit : DefaultMaxConcurrency;
        }

        /// <summary>
        /// Disables bulkhead for a specific request type.
        /// </summary>
        public BulkheadOptions DisableBulkhead<TRequest>()
        {
            _requestTypeLimits[typeof(TRequest).Name] = 0;
            return this;
        }
    }

    /// <summary>
    /// Exception thrown when bulkhead rejects a request due to concurrency limits.
    /// </summary>
    public class BulkheadRejectedException : Exception
    {
        public string RequestType { get; }
        public int MaxConcurrency { get; }

        public BulkheadRejectedException(string requestType, int maxConcurrency)
            : base($"Bulkhead rejected request type '{requestType}': maximum concurrency of {maxConcurrency} exceeded")
        {
            RequestType = requestType;
            MaxConcurrency = maxConcurrency;
        }
    }
}