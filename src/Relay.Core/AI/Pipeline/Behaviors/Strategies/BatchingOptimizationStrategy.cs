using Microsoft.Extensions.Logging;
using Relay.Core.AI.Optimization.Batching;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Pipeline.Behaviors.Strategies;

/// <summary>
/// Strategy for applying AI-powered batching optimizations.
/// </summary>
public class BatchingOptimizationStrategy<TRequest, TResponse> : BaseOptimizationStrategy<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAIOptimizationEngine _aiEngine;
    private readonly AIOptimizationOptions _options;

    // Batch coordinator registry - one coordinator per request type
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, object> _batchCoordinators = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> _coordinatorLocks = new();

    public BatchingOptimizationStrategy(
        ILogger logger,
        IAIOptimizationEngine aiEngine,
        AIOptimizationOptions options,
        IMetricsProvider? metricsProvider = null)
        : base(logger, metricsProvider)
    {
        _aiEngine = aiEngine ?? throw new ArgumentNullException(nameof(aiEngine));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override OptimizationStrategy StrategyType => OptimizationStrategy.BatchProcessing;

    public override async ValueTask<bool> CanApplyAsync(
        TRequest request,
        OptimizationRecommendation recommendation,
        SystemLoadMetrics systemLoad,
        CancellationToken cancellationToken)
    {
        // Check confidence threshold
        if (!MeetsConfidenceThreshold(recommendation, _options.MinConfidenceScore))
            return false;

        // Get optimal batch size from AI based on current system load
        var optimalBatchSize = await _aiEngine.PredictOptimalBatchSizeAsync(typeof(TRequest), systemLoad, cancellationToken);

        // Check if batching is beneficial based on system conditions
        return ShouldApplyBatching(systemLoad, optimalBatchSize, recommendation);
    }

    public override async ValueTask<RequestHandlerDelegate<TResponse>> ApplyAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        OptimizationRecommendation recommendation,
        SystemLoadMetrics systemLoad,
        CancellationToken cancellationToken)
    {
        // Get optimal batch size from AI based on current system load
        var optimalBatchSize = await _aiEngine.PredictOptimalBatchSizeAsync(typeof(TRequest), systemLoad, cancellationToken);

        // Extract batching parameters from recommendation
        var batchWindow = GetBatchWindow(recommendation);
        var maxWaitTime = GetMaxWaitTime(recommendation, systemLoad);
        var batchingStrategy = GetBatchingStrategy(recommendation);

        Logger.LogDebug("Applying AI-powered batching for {RequestType}: Size={BatchSize}, Window={Window}ms, Strategy={Strategy}",
            typeof(TRequest).Name, optimalBatchSize, batchWindow.TotalMilliseconds, batchingStrategy);

        // Get or create batch coordinator for this request type
        var batchCoordinator = GetBatchCoordinator(typeof(TRequest), optimalBatchSize, batchWindow, maxWaitTime, batchingStrategy);

        // Wrap the handler with batching logic
        return async () =>
        {
            var batchId = Guid.NewGuid();
            Logger.LogDebug("Request {RequestType} entering batch queue (ID: {BatchId})", typeof(TRequest).Name, batchId);

            try
            {
                // Add request to batch and wait for batch execution
                var batchItem = new BatchItem<TRequest, TResponse>
                {
                    Request = request,
                    Handler = next,
                    CancellationToken = cancellationToken,
                    EnqueueTime = DateTime.UtcNow,
                    BatchId = batchId
                };

                var result = await batchCoordinator.EnqueueAndWaitAsync(batchItem, cancellationToken);

                Logger.LogDebug("Request {RequestType} batch execution completed (ID: {BatchId}, BatchSize: {Size})",
                    typeof(TRequest).Name, batchId, result.BatchSize);

                // Record batching metrics for AI learning
                RecordBatchingMetrics(typeof(TRequest), result);

                return result.Response;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Batching failed for {RequestType} (ID: {BatchId}), executing individually",
                    typeof(TRequest).Name, batchId);

                // Fallback to individual execution on batching failure
                return await next();
            }
        };
    }

    private bool ShouldApplyBatching(SystemLoadMetrics systemLoad, int optimalBatchSize, OptimizationRecommendation recommendation)
    {
        // Don't batch if batch size is too small
        if (optimalBatchSize < 2)
            return false;

        // Don't batch under very high load (batching adds coordination overhead)
        if (systemLoad.CpuUtilization > 0.95 || systemLoad.MemoryUtilization > 0.95)
            return false;

        // Check if confidence is sufficient
        if (recommendation.ConfidenceScore < _options.MinConfidenceScore)
            return false;

        // Check throughput - batching is beneficial for high-throughput scenarios
        if (systemLoad.ThroughputPerSecond < 5.0)
            return false; // Too low throughput for batching

        return true;
    }

    private TimeSpan GetBatchWindow(OptimizationRecommendation recommendation)
    {
        if (recommendation.Parameters.TryGetValue("BatchWindow", out var windowObj))
        {
            if (windowObj is TimeSpan window)
                return window;
            if (windowObj is int windowMs)
                return TimeSpan.FromMilliseconds(windowMs);
            if (windowObj is double windowMsDouble)
                return TimeSpan.FromMilliseconds(windowMsDouble);
        }

        // Default adaptive window based on average response time
        return TimeSpan.FromMilliseconds(100);
    }

    private TimeSpan GetMaxWaitTime(OptimizationRecommendation recommendation, SystemLoadMetrics systemLoad)
    {
        if (recommendation.Parameters.TryGetValue("MaxWaitTime", out var waitObj))
        {
            if (waitObj is TimeSpan wait)
                return wait;
            if (waitObj is int waitMs)
                return TimeSpan.FromMilliseconds(waitMs);
        }

        // Adaptive max wait time based on system load
        // Under high load, wait less to maintain responsiveness
        var baseWaitMs = 200.0;
        var loadFactor = 1.0 - (systemLoad.CpuUtilization * 0.5); // Reduce wait time under load

        return TimeSpan.FromMilliseconds(baseWaitMs * Math.Max(0.3, loadFactor));
    }

    private BatchingStrategy GetBatchingStrategy(OptimizationRecommendation recommendation)
    {
        if (recommendation.Parameters.TryGetValue("BatchingStrategy", out var strategyObj))
        {
            if (strategyObj is BatchingStrategy strategy)
                return strategy;
            if (strategyObj is string strategyStr && Enum.TryParse<BatchingStrategy>(strategyStr, out var parsedStrategy))
                return parsedStrategy;
        }

        return BatchingStrategy.Adaptive; // Default to adaptive
    }

    private BatchCoordinator<TRequest, TResponse> GetBatchCoordinator(
        Type requestType,
        int batchSize,
        TimeSpan batchWindow,
        TimeSpan maxWaitTime,
        BatchingStrategy strategy)
    {
        // Create a unique key for this coordinator configuration
        var coordinatorKey = GenerateCoordinatorKey(requestType, batchSize, strategy);

        // Try to get existing coordinator
        if (_batchCoordinators.TryGetValue(coordinatorKey, out var existingCoordinator))
        {
            var coordinator = existingCoordinator as BatchCoordinator<TRequest, TResponse>;
            if (coordinator != null)
            {
                // Check if coordinator needs to be updated due to parameter changes
                if (ShouldUpdateCoordinator(coordinator, batchSize, batchWindow, maxWaitTime, strategy))
                {
                    Logger.LogDebug("Batch coordinator parameters changed for {RequestType}, creating new coordinator", requestType.Name);
                    // Remove old coordinator and create new one
                    _batchCoordinators.TryRemove(coordinatorKey, out _);
                }
                else
                {
                    Logger.LogDebug("Reusing existing batch coordinator for {RequestType}", requestType.Name);
                    return coordinator;
                }
            }
        }

        // Get or create lock for this coordinator key
        var coordinatorLock = _coordinatorLocks.GetOrAdd(coordinatorKey, _ => new SemaphoreSlim(1, 1));

        // Double-checked locking pattern to ensure only one coordinator is created
        coordinatorLock.Wait();
        try
        {
            // Check again after acquiring lock
            if (_batchCoordinators.TryGetValue(coordinatorKey, out var lockedCoordinator))
            {
                var coordinator = lockedCoordinator as BatchCoordinator<TRequest, TResponse>;
                if (coordinator != null)
                {
                    return coordinator;
                }
            }

            // Create new coordinator
            Logger.LogInformation("Creating new batch coordinator for {RequestType}: Size={BatchSize}, Window={Window}ms, MaxWait={MaxWait}ms, Strategy={Strategy}",
                requestType.Name, batchSize, batchWindow.TotalMilliseconds, maxWaitTime.TotalMilliseconds, strategy);

            var newCoordinator = new BatchCoordinator<TRequest, TResponse>(
                batchSize,
                batchWindow,
                maxWaitTime,
                strategy,
                Logger);

            // Store metadata for future comparison
            newCoordinator.Metadata = new BatchCoordinatorMetadata
            {
                BatchSize = batchSize,
                BatchWindow = batchWindow,
                MaxWaitTime = maxWaitTime,
                Strategy = strategy,
                CreatedAt = DateTime.UtcNow,
                RequestCount = 0
            };

            _batchCoordinators[coordinatorKey] = newCoordinator;

            return newCoordinator;
        }
        finally
        {
            coordinatorLock.Release();
        }
    }

    private string GenerateCoordinatorKey(Type requestType, int batchSize, BatchingStrategy strategy)
    {
        // Create a key that includes request type and strategy
        // This allows different coordinators for different configurations
        return $"{requestType.FullName}:{strategy}";
    }

    private bool ShouldUpdateCoordinator(
        BatchCoordinator<TRequest, TResponse> coordinator,
        int newBatchSize,
        TimeSpan newBatchWindow,
        TimeSpan newMaxWaitTime,
        BatchingStrategy newStrategy)
    {
        if (coordinator.Metadata == null)
            return false;

        var metadata = coordinator.Metadata;

        // Check if any critical parameters have changed significantly
        var batchSizeChanged = Math.Abs(metadata.BatchSize - newBatchSize) > metadata.BatchSize * 0.3; // 30% threshold
        var batchWindowChanged = Math.Abs(metadata.BatchWindow.TotalMilliseconds - newBatchWindow.TotalMilliseconds) > metadata.BatchWindow.TotalMilliseconds * 0.5; // 50% threshold
        var maxWaitTimeChanged = Math.Abs(metadata.MaxWaitTime.TotalMilliseconds - newMaxWaitTime.TotalMilliseconds) > metadata.MaxWaitTime.TotalMilliseconds * 0.5; // 50% threshold
        var strategyChanged = metadata.Strategy != newStrategy;

        // Also consider replacing old coordinators (older than 1 hour)
        var isOld = (DateTime.UtcNow - metadata.CreatedAt) > TimeSpan.FromHours(1);

        return batchSizeChanged || batchWindowChanged || maxWaitTimeChanged || strategyChanged || isOld;
    }

    private void RecordBatchingMetrics(Type requestType, BatchExecutionResult<TResponse> result)
    {
        var properties = new Dictionary<string, object>
        {
            ["BatchSize"] = result.BatchSize,
            ["BatchWaitTime"] = result.WaitTime.TotalMilliseconds,
            ["BatchExecutionTime"] = result.ExecutionTime.TotalMilliseconds,
            ["BatchStrategy"] = result.Strategy.ToString(),
            ["BatchEfficiency"] = result.Efficiency
        };

        RecordMetrics(requestType, result.ExecutionTime, result.Success, properties);
    }
}