using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// Pipeline behavior for AI-driven batch optimization of requests.
    /// </summary>
    internal class AIBatchOptimizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<AIBatchOptimizationBehavior<TRequest, TResponse>> _logger;
        private readonly ConcurrentDictionary<Type, BatchMetrics> _batchMetrics;

        public AIBatchOptimizationBehavior(ILogger<AIBatchOptimizationBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _batchMetrics = new ConcurrentDictionary<Type, BatchMetrics>();
        }

        public async ValueTask<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestType = typeof(TRequest);
            var metrics = _batchMetrics.GetOrAdd(requestType, _ => new BatchMetrics());

            // Track batch request
            metrics.IncrementRequestCount();

            try
            {
                // Check if batching should be applied
                if (ShouldApplyBatching(metrics))
                {
                    _logger.LogDebug("Batch optimization recommended for request type: {RequestType}", requestType.Name);
                    // In a production environment, this would:
                    // 1. Accumulate similar requests
                    // 2. Batch process them together
                    // 3. Optimize database queries
                    // 4. Reduce external API calls
                    // 5. Improve overall throughput
                }

                var response = await next();
                
                metrics.IncrementSuccessCount();
                
                return response;
            }
            catch (Exception)
            {
                metrics.IncrementFailureCount();
                throw;
            }
        }

        private bool ShouldApplyBatching(BatchMetrics metrics)
        {
            // Simple heuristic: batch if we have high request volume
            var requestRate = metrics.GetRequestRate();
            return requestRate > 10; // More than 10 requests per second
        }

        private class BatchMetrics
        {
            private long _totalRequests = 0;
            private long _successfulRequests = 0;
            private long _failedRequests = 0;
            private DateTime _firstRequest = DateTime.UtcNow;

            public void IncrementRequestCount() => Interlocked.Increment(ref _totalRequests);
            public void IncrementSuccessCount() => Interlocked.Increment(ref _successfulRequests);
            public void IncrementFailureCount() => Interlocked.Increment(ref _failedRequests);

            public double GetRequestRate()
            {
                var elapsed = (DateTime.UtcNow - _firstRequest).TotalSeconds;
                return elapsed > 0 ? _totalRequests / elapsed : 0;
            }
        }
    }
}
