using Microsoft.Extensions.Logging;
using Relay.Core.AI.Optimization.Batching;
using Relay.Core.AI.Pipeline.Options;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Pipeline.Behaviors;

/// <summary>
/// Pipeline behavior for AI-driven batch optimization of requests.
/// </summary>
internal class AIBatchOptimizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>, IDisposable, IAIBatchOptimizationMonitor
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

                    // Update coordinator metadata with current metrics
                    if (coordinator.Metadata != null)
                    {
                        coordinator.Metadata.AverageWaitTime = metrics.GetAverageWaitTime();
                        coordinator.Metadata.AverageBatchSize = (double)metrics.GetTotalBatchedRequests() / Math.Max(metrics.GetBatchExecutionsCount(), 1);
                        coordinator.Metadata.BatchingRate = metrics.GetBatchingRate();
                        coordinator.Metadata.AverageEfficiency = metrics.GetAverageEfficiency();
                        coordinator.Metadata.RequestCount++;
                        coordinator.Metadata.LastUsed = DateTime.UtcNow;
                    }

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
                AverageBatchSize = 0.0,
                BatchingRate = 0.0,
                AverageEfficiency = 0.0
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

    // Implementation of IAIBatchOptimizationMonitor interface
    public double GetAverageWaitTime(Type requestType)
    {
        if (_batchMetrics.TryGetValue(requestType, out var metrics))
        {
            return metrics.GetAverageWaitTime();
        }
        return 0.0;
    }

    public double GetAverageEfficiency(Type requestType)
    {
        if (_batchMetrics.TryGetValue(requestType, out var metrics))
        {
            return metrics.GetAverageEfficiency();
        }
        return 0.0;
    }

    public double GetBatchingRate(Type requestType)
    {
        if (_batchMetrics.TryGetValue(requestType, out var metrics))
        {
            return metrics.GetBatchingRate();
        }
        return 0.0;
    }

    public double GetRequestRate(Type requestType)
    {
        if (_batchMetrics.TryGetValue(requestType, out var metrics))
        {
            return metrics.GetRequestRate();
        }
        return 0.0;
    }

    public System.Collections.Generic.IEnumerable<Type> GetTrackedRequestTypes()
    {
        return _batchMetrics.Keys;
    }

    public BatchMetricsSnapshot GetBatchMetrics(Type requestType)
    {
        if (_batchMetrics.TryGetValue(requestType, out var metrics))
        {
            return metrics.GetBatchMetricsSnapshot();
        }
        
        return new BatchMetricsSnapshot
        {
            AverageWaitTime = 0.0,
            AverageEfficiency = 0.0,
            BatchingRate = 0.0,
            RequestRate = 0.0,
            TotalRequests = 0,
            SuccessfulRequests = 0,
            FailedRequests = 0,
            TotalBatchedRequests = 0,
            BatchExecutions = 0
        };
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

        public long GetTotalBatchedRequests()
        {
            return _totalBatchedRequests;
        }

        public long GetBatchExecutionsCount()
        {
            return _batchExecutions;
        }

        public BatchMetricsSnapshot GetBatchMetricsSnapshot()
        {
            lock (_lock)
            {
                var elapsed = (DateTime.UtcNow - _firstRequest).TotalSeconds;
                var requestRate = elapsed > 0 ? _totalRequests / elapsed : 0;
                
                return new BatchMetricsSnapshot
                {
                    AverageWaitTime = _batchExecutions > 0 ? _totalWaitTime / _batchExecutions : 0,
                    AverageEfficiency = _batchExecutions > 0 ? _totalEfficiency / _batchExecutions : 0,
                    BatchingRate = _totalRequests > 0 ? (double)_totalBatchedRequests / _totalRequests : 0,
                    RequestRate = requestRate,
                    TotalRequests = _totalRequests,
                    SuccessfulRequests = _successfulRequests,
                    FailedRequests = _failedRequests,
                    TotalBatchedRequests = _totalBatchedRequests,
                    BatchExecutions = _batchExecutions
                };
            }
        }
    }
}
