using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Resilience.Bulkhead
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
}