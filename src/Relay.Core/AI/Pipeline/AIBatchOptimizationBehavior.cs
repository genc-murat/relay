using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.AI.Optimization.Batching;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.AI
{
    /// <summary>
    /// Pipeline behavior for AI-driven batch optimization of requests.
    /// </summary>
    internal class AIBatchOptimizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>, IDisposable
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<AIBatchOptimizationBehavior<TRequest, TResponse>> _logger;
        private readonly ConcurrentDictionary<Type, BatchMetrics> _batchMetrics;
        private readonly ConcurrentDictionary<Type, IBatchCoordinator> _batchCoordinators;
        private readonly AIBatchOptimizationOptions _options;
        private bool _disposed;

        public AIBatchOptimizationBehavior(
            ILogger<AIBatchOptimizationBehavior<TRequest, TResponse>> logger,
            AIBatchOptimizationOptions? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new AIBatchOptimizationOptions();
            _batchMetrics = new ConcurrentDictionary<Type, BatchMetrics>();
            _batchCoordinators = new ConcurrentDictionary<Type, IBatchCoordinator>();
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
                if (ShouldApplyBatching(request, metrics))
                {
                    _logger.LogDebug("Applying batch optimization for request type: {RequestType}", requestType.Name);

                    // Get or create batch coordinator
                    var coordinator = GetOrCreateBatchCoordinator(requestType, request);

                    if (coordinator != null)
                    {
                        // Enqueue the request and wait for batch processing
                        var batchItem = new BatchItem<TRequest, TResponse>
                        {
                            Request = request,
                            Handler = next,
                            CancellationToken = cancellationToken,
                            EnqueueTime = DateTime.UtcNow,
                            BatchId = Guid.NewGuid()
                        };

                        var result = await ((BatchCoordinator<TRequest, TResponse>)coordinator)
                            .EnqueueAndWaitAsync(batchItem, cancellationToken);

                        metrics.IncrementSuccessCount();
                        metrics.RecordBatchExecution(result.BatchSize, result.WaitTime, result.Efficiency);

                        _logger.LogDebug(
                            "Batch execution completed: BatchSize={BatchSize}, WaitTime={WaitTime}ms, Efficiency={Efficiency:F2}",
                            result.BatchSize,
                            result.WaitTime.TotalMilliseconds,
                            result.Efficiency);

                        return result.Response;
                    }
                }

                // Fall back to direct execution
                var response = await next();
                metrics.IncrementSuccessCount();
                return response;
            }
            catch (Exception ex)
            {
                metrics.IncrementFailureCount();
                _logger.LogError(ex, "Error processing request in batch optimization behavior");
                throw;
            }
        }

        private bool ShouldApplyBatching(TRequest request, BatchMetrics metrics)
        {
            if (!_options.EnableBatching)
                return false;

            // Check for SmartBatchingAttribute on request type
            var attribute = typeof(TRequest).GetCustomAttribute<SmartBatchingAttribute>();
            if (attribute != null)
            {
                // Attribute explicitly enables batching
                return true;
            }

            // Use heuristic based on request rate
            var requestRate = metrics.GetRequestRate();
            return requestRate > _options.MinimumRequestRateForBatching;
        }

        private IBatchCoordinator? GetOrCreateBatchCoordinator(Type requestType, TRequest request)
        {
            return _batchCoordinators.GetOrAdd(requestType, _ =>
            {
                var attribute = requestType.GetCustomAttribute<SmartBatchingAttribute>();

                int batchSize = attribute?.MaxBatchSize ?? _options.DefaultBatchSize;
                TimeSpan batchWindow = attribute != null
                    ? TimeSpan.FromMilliseconds(attribute.MaxWaitTimeMilliseconds)
                    : _options.DefaultBatchWindow;
                TimeSpan maxWaitTime = attribute != null
                    ? TimeSpan.FromMilliseconds(attribute.MaxWaitTimeMilliseconds)
                    : _options.DefaultMaxWaitTime;
                BatchingStrategy strategy = attribute?.Strategy ?? _options.DefaultStrategy;

                var coordinator = new BatchCoordinator<TRequest, TResponse>(
                    batchSize,
                    batchWindow,
                    maxWaitTime,
                    strategy,
                    _logger);

                coordinator.Metadata = new BatchCoordinatorMetadata
                {
                    BatchSize = batchSize,
                    BatchWindow = batchWindow,
                    MaxWaitTime = maxWaitTime,
                    Strategy = strategy,
                    CreatedAt = DateTime.UtcNow,
                    RequestCount = 0,
                    LastUsed = DateTime.UtcNow,
                    AverageWaitTime = 0.0,
                    AverageBatchSize = 0.0
                };

                _logger.LogInformation(
                    "Created batch coordinator for {RequestType}: BatchSize={BatchSize}, Window={Window}ms, Strategy={Strategy}",
                    requestType.Name,
                    batchSize,
                    batchWindow.TotalMilliseconds,
                    strategy);

                return coordinator;
            });
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            foreach (var coordinator in _batchCoordinators.Values)
            {
                if (coordinator is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _batchCoordinators.Clear();
            _disposed = true;
        }

        private class BatchMetrics
        {
            private long _totalRequests = 0;
            private long _successfulRequests = 0;
            private long _failedRequests = 0;
            private long _totalBatchedRequests = 0;
            private double _totalWaitTime = 0;
            private double _totalEfficiency = 0;
            private long _batchExecutions = 0;
            private DateTime _firstRequest = DateTime.UtcNow;
            private readonly object _lock = new object();

            public void IncrementRequestCount() => Interlocked.Increment(ref _totalRequests);
            public void IncrementSuccessCount() => Interlocked.Increment(ref _successfulRequests);
            public void IncrementFailureCount() => Interlocked.Increment(ref _failedRequests);

            public void RecordBatchExecution(int batchSize, TimeSpan waitTime, double efficiency)
            {
                lock (_lock)
                {
                    Interlocked.Add(ref _totalBatchedRequests, batchSize);
                    _totalWaitTime += waitTime.TotalMilliseconds;
                    _totalEfficiency += efficiency;
                    Interlocked.Increment(ref _batchExecutions);
                }
            }

            public double GetRequestRate()
            {
                var elapsed = (DateTime.UtcNow - _firstRequest).TotalSeconds;
                return elapsed > 0 ? _totalRequests / elapsed : 0;
            }

            public double GetAverageWaitTime()
            {
                lock (_lock)
                {
                    return _batchExecutions > 0 ? _totalWaitTime / _batchExecutions : 0;
                }
            }

            public double GetAverageEfficiency()
            {
                lock (_lock)
                {
                    return _batchExecutions > 0 ? _totalEfficiency / _batchExecutions : 0;
                }
            }

            public double GetBatchingRate()
            {
                var total = _totalRequests;
                return total > 0 ? (double)_totalBatchedRequests / total : 0;
            }
        }
    }

    /// <summary>
    /// Configuration options for AIBatchOptimizationBehavior.
    /// </summary>
    public class AIBatchOptimizationOptions
    {
        /// <summary>
        /// Gets or sets whether batching is enabled.
        /// </summary>
        public bool EnableBatching { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum request rate (requests/second) to trigger batching.
        /// </summary>
        public double MinimumRequestRateForBatching { get; set; } = 10.0;

        /// <summary>
        /// Gets or sets the default batch size.
        /// </summary>
        public int DefaultBatchSize { get; set; } = 50;

        /// <summary>
        /// Gets or sets the default batch window.
        /// </summary>
        public TimeSpan DefaultBatchWindow { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets or sets the default maximum wait time.
        /// </summary>
        public TimeSpan DefaultMaxWaitTime { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Gets or sets the default batching strategy.
        /// </summary>
        public BatchingStrategy DefaultStrategy { get; set; } = BatchingStrategy.Dynamic;
    }
}
