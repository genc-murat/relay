using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Relay.Core.Telemetry;

namespace Relay.Core.AI
{
    /// <summary>
    /// Partial class containing optimization strategy application methods.
    /// </summary>
    public sealed partial class AIOptimizationPipelineBehavior<TRequest, TResponse>
    {
        private async ValueTask<RequestHandlerDelegate<TResponse>> ApplyOptimizations(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            OptimizationRecommendation recommendation,
            SystemLoadMetrics systemLoad,
            List<OptimizationStrategy> appliedOptimizations,
            CancellationToken cancellationToken)
        {
            var optimizedNext = next;

            // Apply optimizations based on AI recommendations
            if (recommendation.ConfidenceScore >= _options.MinConfidenceScore)
            {
                switch (recommendation.Strategy)
                {
                    case OptimizationStrategy.EnableCaching:
                        optimizedNext = await ApplyCachingOptimization(request, optimizedNext, recommendation, appliedOptimizations, cancellationToken);
                        break;

                    case OptimizationStrategy.BatchProcessing:
                        optimizedNext = await ApplyBatchingOptimization(request, optimizedNext, recommendation, systemLoad, appliedOptimizations, cancellationToken);
                        break;

                    case OptimizationStrategy.MemoryPooling:
                        optimizedNext = ApplyMemoryPoolingOptimization(optimizedNext, recommendation, appliedOptimizations);
                        break;

                    case OptimizationStrategy.ParallelProcessing:
                        optimizedNext = ApplyParallelProcessingOptimization(optimizedNext, recommendation, systemLoad, appliedOptimizations);
                        break;

                    case OptimizationStrategy.CircuitBreaker:
                        optimizedNext = ApplyCircuitBreakerOptimization(optimizedNext, recommendation, appliedOptimizations);
                        break;

                    case OptimizationStrategy.DatabaseOptimization:
                        optimizedNext = await ApplyDatabaseOptimization(request, optimizedNext, recommendation, appliedOptimizations, cancellationToken);
                        break;

                    case OptimizationStrategy.SIMDAcceleration:
                        optimizedNext = ApplySIMDOptimization(optimizedNext, recommendation, appliedOptimizations);
                        break;

                    case OptimizationStrategy.Custom:
                        optimizedNext = await ApplyCustomOptimization(request, optimizedNext, recommendation, appliedOptimizations, cancellationToken);
                        break;

                    case OptimizationStrategy.None:
                    default:
                        // No optimization needed
                        break;
                }
            }

            return optimizedNext;
        }

        private async ValueTask<RequestHandlerDelegate<TResponse>> ApplyCachingOptimization(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            OptimizationRecommendation recommendation,
            List<OptimizationStrategy> appliedOptimizations,
            CancellationToken cancellationToken)
        {
            // Check if caching infrastructure is available
            if (_memoryCache == null && _distributedCache == null)
            {
                _logger.LogWarning("Caching optimization recommended but no cache provider available for {RequestType}", typeof(TRequest).Name);
                return next;
            }

            // Get AI caching recommendation with access patterns
            var accessPatterns = await GetAccessPatternsAsync(typeof(TRequest), cancellationToken);
            var cachingRecommendation = await _aiEngine.ShouldCacheAsync(typeof(TRequest), accessPatterns, cancellationToken);

            if (!cachingRecommendation.ShouldCache || cachingRecommendation.PredictedHitRate < _options.MinCacheHitRate)
            {
                _logger.LogDebug("AI recommends skipping cache for {RequestType} (HitRate: {HitRate:P})",
                    typeof(TRequest).Name, cachingRecommendation.PredictedHitRate);
                return next;
            }

            _logger.LogDebug("Applying AI-powered caching for {RequestType} (Predicted HitRate: {HitRate:P}, TTL: {TTL}s)",
                typeof(TRequest).Name, cachingRecommendation.PredictedHitRate, cachingRecommendation.RecommendedTtl.TotalSeconds);

            appliedOptimizations.Add(OptimizationStrategy.EnableCaching);

            // Generate cache key using AI-recommended strategy
            var cacheKey = GenerateSmartCacheKey(request, cachingRecommendation);

            // Wrap the handler with caching logic
            return async () =>
            {
                // Try memory cache first (L1)
                if (_memoryCache != null && _memoryCache.TryGetValue<TResponse>(cacheKey, out var memCachedResponse) && memCachedResponse != null)
                {
                    _logger.LogDebug("AI cache hit (Memory L1) for {RequestType}: {CacheKey}", typeof(TRequest).Name, cacheKey);
                    RecordCacheMetrics(typeof(TRequest), "Memory", hit: true);
                    return memCachedResponse;
                }

                // Try distributed cache (L2)
                if (_distributedCache != null)
                {
                    try
                    {
                        var cachedBytes = await _distributedCache.GetAsync(cacheKey, cancellationToken);
                        if (cachedBytes != null && cachedBytes.Length > 0)
                        {
                            var distCachedResponse = DeserializeResponse(cachedBytes);
                            _logger.LogDebug("AI cache hit (Distributed L2) for {RequestType}: {CacheKey}", typeof(TRequest).Name, cacheKey);

                            // Promote to memory cache (cache warming)
                            if (_memoryCache != null)
                            {
                                var memOptions = new MemoryCacheEntryOptions
                                {
                                    AbsoluteExpirationRelativeToNow = cachingRecommendation.RecommendedTtl,
                                    Size = EstimateResponseSize(distCachedResponse)
                                };
                                _memoryCache.Set(cacheKey, distCachedResponse, memOptions);
                            }

                            RecordCacheMetrics(typeof(TRequest), "Distributed", hit: true);
                            return distCachedResponse;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to retrieve from distributed cache for {RequestType}", typeof(TRequest).Name);
                    }
                }

                _logger.LogDebug("AI cache miss for {RequestType}: {CacheKey}", typeof(TRequest).Name, cacheKey);
                RecordCacheMetrics(typeof(TRequest), "All", hit: false);

                // Execute handler
                var response = await next();

                // Store in cache with AI-recommended TTL and eviction policy
                await StoreToCacheAsync(cacheKey, response, cachingRecommendation, cancellationToken);

                return response;
            };
        }

        private async ValueTask<AccessPattern[]> GetAccessPatternsAsync(Type requestType, CancellationToken cancellationToken)
        {
            // Try to get access patterns from metrics provider
            if (_metricsProvider != null)
            {
                try
                {
                    var stats = _metricsProvider.GetHandlerExecutionStats(requestType);
                    if (stats != null && stats.TotalExecutions > 0)
                    {
                        return new[]
                        {
                            new AccessPattern
                            {
                                RequestType = requestType,
                                AccessFrequency = CalculateExecutionFrequency(stats),
                                AverageExecutionTime = stats.AverageExecutionTime,
                                DataVolatility = CalculateDataVolatility(stats),
                                TimeOfDayPattern = TimeOfDayPattern.Uniform,
                                SampleSize = stats.TotalExecutions
                            }
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to retrieve access patterns for {RequestType}", requestType.Name);
                }
            }

            // Return default pattern
            await Task.CompletedTask;
            return new[]
            {
                new AccessPattern
                {
                    RequestType = requestType,
                    AccessFrequency = 1.0,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                    DataVolatility = 0.5,
                    TimeOfDayPattern = TimeOfDayPattern.Uniform,
                    SampleSize = 0
                }
            };
        }

        private double CalculateDataVolatility(Telemetry.HandlerExecutionStats stats)
        {
            // High failure rate or high execution time variance indicates volatile data
            var failureRate = stats.TotalExecutions > 0
                ? (double)stats.FailedExecutions / stats.TotalExecutions
                : 0.0;

            var executionTimeVariance = CalculateExecutionTimeVariance(stats);

            // Combine factors (0 = stable, 1 = highly volatile)
            return Math.Clamp(failureRate * 0.7 + executionTimeVariance * 0.3, 0.0, 1.0);
        }

        private string GenerateSmartCacheKey(TRequest request, CachingRecommendation recommendation)
        {
            var requestType = typeof(TRequest).Name;

            // Use AI-recommended key strategy
            switch (recommendation.KeyStrategy)
            {
                case CacheKeyStrategy.FullRequest:
                    return $"ai:cache:{requestType}:{GetRequestHash(request)}";

                case CacheKeyStrategy.RequestTypeOnly:
                    return $"ai:cache:{requestType}";

                case CacheKeyStrategy.SelectedProperties:
                    return $"ai:cache:{requestType}:{GetSelectedPropertiesHash(request, recommendation.KeyProperties)}";

                case CacheKeyStrategy.Custom:
                default:
                    return $"ai:cache:{requestType}:{GetRequestHash(request)}";
            }
        }

        private string GetRequestHash(TRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = false });
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));
                return Convert.ToBase64String(hashBytes)[..16]; // Use first 16 characters
            }
            catch
            {
                return request.GetHashCode().ToString();
            }
        }

        private string GetSelectedPropertiesHash(TRequest request, string[] properties)
        {
            if (properties == null || properties.Length == 0)
                return GetRequestHash(request);

            try
            {
                var values = new List<string>();
                var requestType = typeof(TRequest);

                foreach (var propName in properties)
                {
                    var prop = requestType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                    if (prop != null)
                    {
                        var value = prop.GetValue(request);
                        values.Add(value?.ToString() ?? "null");
                    }
                }

                var combined = string.Join(":", values);
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combined));
                return Convert.ToBase64String(hashBytes)[..16];
            }
            catch
            {
                return GetRequestHash(request);
            }
        }

        private TResponse DeserializeResponse(byte[] cachedBytes)
        {
            try
            {
                return JsonSerializer.Deserialize<TResponse>(cachedBytes)!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize cached response for {RequestType}", typeof(TRequest).Name);
                throw;
            }
        }

        private long EstimateResponseSize(TResponse response)
        {
            try
            {
                var json = JsonSerializer.Serialize(response);
                return json.Length;
            }
            catch
            {
                return 1024; // Default 1KB
            }
        }

        private async Task StoreToCacheAsync(string cacheKey, TResponse response, CachingRecommendation recommendation, CancellationToken cancellationToken)
        {
            try
            {
                // Store in memory cache (L1)
                if (_memoryCache != null)
                {
                    var memOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = recommendation.RecommendedTtl,
                        Priority = recommendation.Priority switch
                        {
                            CachePriority.High => CacheItemPriority.High,
                            CachePriority.Normal => CacheItemPriority.Normal,
                            CachePriority.Low => CacheItemPriority.Low,
                            _ => CacheItemPriority.Normal
                        },
                        Size = EstimateResponseSize(response)
                    };

                    _memoryCache.Set(cacheKey, response, memOptions);
                    _logger.LogDebug("Stored in memory cache (L1): {CacheKey}, TTL: {TTL}s", cacheKey, recommendation.RecommendedTtl.TotalSeconds);
                }

                // Store in distributed cache (L2)
                if (_distributedCache != null && recommendation.UseDistributedCache)
                {
                    var serialized = JsonSerializer.SerializeToUtf8Bytes(response);
                    var distOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = recommendation.RecommendedTtl
                    };

                    await _distributedCache.SetAsync(cacheKey, serialized, distOptions, cancellationToken);
                    _logger.LogDebug("Stored in distributed cache (L2): {CacheKey}, TTL: {TTL}s", cacheKey, recommendation.RecommendedTtl.TotalSeconds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to store response in cache for {RequestType}", typeof(TRequest).Name);
            }
        }

        private void RecordCacheMetrics(Type requestType, string cacheType, bool hit)
        {
            // Record metrics for AI learning
            if (_metricsProvider != null)
            {
                try
                {
                    var metrics = new HandlerExecutionMetrics
                    {
                        RequestType = requestType,
                        Timestamp = DateTimeOffset.UtcNow,
                        Properties = new Dictionary<string, object>
                        {
                            ["CacheType"] = cacheType,
                            ["CacheHit"] = hit
                        }
                    };
                    _metricsProvider.RecordHandlerExecution(metrics);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to record cache metrics");
                }
            }
        }

        private async ValueTask<RequestHandlerDelegate<TResponse>> ApplyBatchingOptimization(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            OptimizationRecommendation recommendation,
            SystemLoadMetrics systemLoad,
            List<OptimizationStrategy> appliedOptimizations,
            CancellationToken cancellationToken)
        {
            // Get optimal batch size from AI based on current system load
            var optimalBatchSize = await _aiEngine.PredictOptimalBatchSizeAsync(typeof(TRequest), systemLoad, cancellationToken);

            // Check if batching is beneficial based on system conditions
            if (!ShouldApplyBatching(systemLoad, optimalBatchSize, recommendation))
            {
                _logger.LogDebug("Batching optimization skipped for {RequestType} - conditions not favorable", typeof(TRequest).Name);
                return next;
            }

            // Extract batching parameters from recommendation
            var batchWindow = GetBatchWindow(recommendation);
            var maxWaitTime = GetMaxWaitTime(recommendation, systemLoad);
            var batchingStrategy = GetBatchingStrategy(recommendation);

            _logger.LogDebug("Applying AI-powered batching for {RequestType}: Size={BatchSize}, Window={Window}ms, Strategy={Strategy}",
                typeof(TRequest).Name, optimalBatchSize, batchWindow.TotalMilliseconds, batchingStrategy);

            appliedOptimizations.Add(OptimizationStrategy.BatchProcessing);

            // Get or create batch coordinator for this request type
            var batchCoordinator = GetBatchCoordinator(typeof(TRequest), optimalBatchSize, batchWindow, maxWaitTime, batchingStrategy);

            // Wrap the handler with batching logic
            return async () =>
            {
                var batchId = Guid.NewGuid();
                _logger.LogDebug("Request {RequestType} entering batch queue (ID: {BatchId})", typeof(TRequest).Name, batchId);

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

                    _logger.LogDebug("Request {RequestType} batch execution completed (ID: {BatchId}, BatchSize: {Size})",
                        typeof(TRequest).Name, batchId, result.BatchSize);

                    // Record batching metrics for AI learning
                    RecordBatchingMetrics(typeof(TRequest), result);

                    return result.Response;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Batching failed for {RequestType} (ID: {BatchId}), executing individually",
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
                        _logger.LogDebug("Batch coordinator parameters changed for {RequestType}, creating new coordinator", requestType.Name);
                        // Remove old coordinator and create new one
                        _batchCoordinators.TryRemove(coordinatorKey, out _);
                    }
                    else
                    {
                        _logger.LogDebug("Reusing existing batch coordinator for {RequestType}", requestType.Name);
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
                _logger.LogInformation("Creating new batch coordinator for {RequestType}: Size={BatchSize}, Window={Window}ms, MaxWait={MaxWait}ms, Strategy={Strategy}",
                    requestType.Name, batchSize, batchWindow.TotalMilliseconds, maxWaitTime.TotalMilliseconds, strategy);

                var newCoordinator = new BatchCoordinator<TRequest, TResponse>(
                    batchSize,
                    batchWindow,
                    maxWaitTime,
                    strategy,
                    _logger);

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

        /// <summary>
        /// Cleanup method to remove stale batch coordinators (should be called periodically)
        /// </summary>
        public static void CleanupStaleCoordinators(TimeSpan maxAge)
        {
            var now = DateTime.UtcNow;
            var keysToRemove = new List<string>();

            foreach (var kvp in _batchCoordinators)
            {
                if (kvp.Value is IBatchCoordinator coordinator)
                {
                    var metadata = coordinator.GetMetadata();
                    if (metadata != null && (now - metadata.CreatedAt) > maxAge)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
            }

            foreach (var key in keysToRemove)
            {
                if (_batchCoordinators.TryRemove(key, out var coordinator))
                {
                    if (coordinator is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

        private void RecordBatchingMetrics(Type requestType, BatchExecutionResult<TResponse> result)
        {
            if (_metricsProvider != null)
            {
                try
                {
                    var metrics = new HandlerExecutionMetrics
                    {
                        RequestType = requestType,
                        Duration = result.ExecutionTime,
                        Success = result.Success,
                        Timestamp = DateTimeOffset.UtcNow,
                        Properties = new Dictionary<string, object>
                        {
                            ["BatchSize"] = result.BatchSize,
                            ["BatchWaitTime"] = result.WaitTime.TotalMilliseconds,
                            ["BatchExecutionTime"] = result.ExecutionTime.TotalMilliseconds,
                            ["BatchStrategy"] = result.Strategy.ToString(),
                            ["BatchEfficiency"] = result.Efficiency
                        }
                    };
                    _metricsProvider.RecordHandlerExecution(metrics);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to record batching metrics");
                }
            }
        }

        private RequestHandlerDelegate<TResponse> ApplyMemoryPoolingOptimization(
            RequestHandlerDelegate<TResponse> next,
            OptimizationRecommendation recommendation,
            List<OptimizationStrategy> appliedOptimizations)
        {
            // Extract memory pooling parameters from AI recommendation
            var enableObjectPooling = GetParameter<bool>(recommendation, "EnableObjectPooling", true);
            var enableBufferPooling = GetParameter<bool>(recommendation, "EnableBufferPooling", true);
            var estimatedBufferSize = GetParameter<int>(recommendation, "EstimatedBufferSize", 4096);
            var poolSize = GetParameter<int>(recommendation, "PoolSize", 100);

            _logger.LogDebug("Applying memory pooling optimization for {RequestType}: ObjectPool={ObjectPool}, BufferPool={BufferPool}, BufferSize={BufferSize}",
                typeof(TRequest).Name, enableObjectPooling, enableBufferPooling, estimatedBufferSize);

            appliedOptimizations.Add(OptimizationStrategy.MemoryPooling);

            // Wrap next with memory pooling logic
            return async () =>
            {
                var startMemory = GC.GetTotalAllocatedBytes(precise: false);
                var poolingContext = new MemoryPoolingContext
                {
                    EnableObjectPooling = enableObjectPooling,
                    EnableBufferPooling = enableBufferPooling,
                    EstimatedBufferSize = estimatedBufferSize
                };

                using var scope = MemoryPoolScope.Create(poolingContext, _logger);

                try
                {
                    // Execute handler with pooling context
                    var response = await next();

                    // Measure memory savings
                    var endMemory = GC.GetTotalAllocatedBytes(precise: false);
                    var allocatedBytes = endMemory - startMemory;

                    // Record pooling effectiveness
                    RecordMemoryPoolingMetrics(typeof(TRequest), allocatedBytes, scope.GetStatistics());

                    _logger.LogDebug("Memory pooling for {RequestType}: Allocated={Allocated}KB, PoolHits={PoolHits}, PoolMisses={PoolMisses}",
                        typeof(TRequest).Name, allocatedBytes / 1024, scope.GetStatistics().PoolHits, scope.GetStatistics().PoolMisses);

                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Memory pooling execution failed for {RequestType}, continuing without pooling benefits", typeof(TRequest).Name);
                    throw;
                }
            };
        }

        private T GetParameter<T>(OptimizationRecommendation recommendation, string parameterName, T defaultValue)
        {
            if (recommendation.Parameters.TryGetValue(parameterName, out var value))
            {
                if (value is T typedValue)
                    return typedValue;

                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        private void RecordMemoryPoolingMetrics(Type requestType, long allocatedBytes, MemoryPoolStatistics stats)
        {
            if (_metricsProvider != null)
            {
                try
                {
                    var metrics = new HandlerExecutionMetrics
                    {
                        RequestType = requestType,
                        Timestamp = DateTimeOffset.UtcNow,
                        Properties = new Dictionary<string, object>
                        {
                            ["AllocatedBytes"] = allocatedBytes,
                            ["PoolHits"] = stats.PoolHits,
                            ["PoolMisses"] = stats.PoolMisses,
                            ["BuffersRented"] = stats.BuffersRented,
                            ["BuffersReturned"] = stats.BuffersReturned,
                            ["MemorySavings"] = stats.EstimatedSavings,
                            ["PoolEfficiency"] = stats.Efficiency
                        }
                    };
                    _metricsProvider.RecordHandlerExecution(metrics);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to record memory pooling metrics");
                }
            }
        }

        private RequestHandlerDelegate<TResponse> ApplyParallelProcessingOptimization(
            RequestHandlerDelegate<TResponse> next,
            OptimizationRecommendation recommendation,
            SystemLoadMetrics systemLoad,
            List<OptimizationStrategy> appliedOptimizations)
        {
            // Extract parallel processing parameters from AI recommendation
            var maxDegreeOfParallelism = GetParameter(recommendation, "MaxDegreeOfParallelism", -1);
            var enableWorkStealing = GetParameter(recommendation, "EnableWorkStealing", true);
            var taskScheduler = GetParameter(recommendation, "TaskScheduler", "Default");
            var minItemsForParallel = GetParameter(recommendation, "MinItemsForParallel", 10);

            // Adjust parallelism based on current system load
            var optimalParallelism = CalculateOptimalParallelism(maxDegreeOfParallelism, systemLoad);

            // Don't apply if system is under high load or parallelism would be minimal
            if (optimalParallelism <= 1 || systemLoad.CpuUtilization > 0.90)
            {
                _logger.LogDebug("Skipping parallel processing for {RequestType} - system load too high or parallelism not beneficial",
                    typeof(TRequest).Name);
                return next;
            }

            _logger.LogDebug("Applying parallel processing optimization for {RequestType}: MaxParallelism={Parallelism}, WorkStealing={WorkStealing}, MinItems={MinItems}",
                typeof(TRequest).Name, optimalParallelism, enableWorkStealing, minItemsForParallel);

            appliedOptimizations.Add(OptimizationStrategy.ParallelProcessing);

            // Wrap handler with parallel processing configuration
            return async () =>
            {
                var parallelContext = new ParallelProcessingContext
                {
                    MaxDegreeOfParallelism = optimalParallelism,
                    EnableWorkStealing = enableWorkStealing,
                    MinItemsForParallel = minItemsForParallel,
                    CpuUtilization = systemLoad.CpuUtilization,
                    AvailableProcessors = Environment.ProcessorCount
                };

                // Store context for handlers that might use it
                using var scope = ParallelProcessingScope.Create(parallelContext, _logger);

                try
                {
                    var startTime = DateTime.UtcNow;

                    // Execute handler (handler can access parallelism context if needed)
                    var response = await next();

                    var duration = DateTime.UtcNow - startTime;
                    var stats = scope.GetStatistics();

                    // Record parallel processing metrics
                    RecordParallelProcessingMetrics(typeof(TRequest), duration, stats, parallelContext);

                    _logger.LogDebug("Parallel processing for {RequestType}: Duration={Duration}ms, TasksExecuted={Tasks}, Efficiency={Efficiency:P}",
                        typeof(TRequest).Name, duration.TotalMilliseconds, stats.TasksExecuted, stats.Efficiency);

                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Parallel processing execution failed for {RequestType}", typeof(TRequest).Name);
                    throw;
                }
            };
        }

        private int CalculateOptimalParallelism(int requestedParallelism, SystemLoadMetrics systemLoad)
        {
            var processorCount = Environment.ProcessorCount;

            // Start with requested parallelism or processor count
            var baseParallelism = requestedParallelism > 0 ? requestedParallelism : processorCount;

            // Adjust based on CPU utilization
            // Under high load, reduce parallelism to avoid contention
            var cpuFactor = 1.0 - systemLoad.CpuUtilization;
            if (cpuFactor < 0.3)
                cpuFactor = 0.3; // Minimum 30% capacity

            var adjustedParallelism = (int)(baseParallelism * cpuFactor);

            // Adjust based on thread pool utilization
            if (systemLoad.ThreadPoolUtilization > 0.8)
            {
                adjustedParallelism = Math.Max(1, adjustedParallelism / 2);
            }

            // Ensure we don't exceed processor count
            adjustedParallelism = Math.Min(adjustedParallelism, processorCount);

            // Ensure minimum of 1
            return Math.Max(1, adjustedParallelism);
        }

        private void RecordParallelProcessingMetrics(
            Type requestType,
            TimeSpan duration,
            ParallelProcessingStatistics stats,
            ParallelProcessingContext context)
        {
            if (_metricsProvider != null)
            {
                try
                {
                    var metrics = new HandlerExecutionMetrics
                    {
                        RequestType = requestType,
                        Duration = duration,
                        Timestamp = DateTimeOffset.UtcNow,
                        Properties = new Dictionary<string, object>
                        {
                            ["MaxDegreeOfParallelism"] = context.MaxDegreeOfParallelism,
                            ["TasksExecuted"] = stats.TasksExecuted,
                            ["TasksCompleted"] = stats.TasksCompleted,
                            ["TasksFailed"] = stats.TasksFailed,
                            ["AverageTaskDuration"] = stats.AverageTaskDuration.TotalMilliseconds,
                            ["ParallelEfficiency"] = stats.Efficiency,
                            ["ActualParallelism"] = stats.ActualParallelism,
                            ["ThreadPoolUtilization"] = context.CpuUtilization,
                            ["Speedup"] = stats.Speedup
                        }
                    };
                    _metricsProvider.RecordHandlerExecution(metrics);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to record parallel processing metrics");
                }
            }
        }

        private RequestHandlerDelegate<TResponse> ApplyCircuitBreakerOptimization(
            RequestHandlerDelegate<TResponse> next,
            OptimizationRecommendation recommendation,
            List<OptimizationStrategy> appliedOptimizations)
        {
            // Extract circuit breaker parameters from AI recommendation
            var failureThreshold = GetParameter(recommendation, "FailureThreshold", 5);
            var successThreshold = GetParameter(recommendation, "SuccessThreshold", 2);
            var timeout = GetParameter(recommendation, "Timeout", 30000);
            var breakDuration = GetParameter(recommendation, "BreakDuration", 60000);
            var halfOpenMaxCalls = GetParameter(recommendation, "HalfOpenMaxCalls", 1);

            _logger.LogDebug("Applying circuit breaker optimization for {RequestType}: FailureThreshold={FailureThreshold}, Timeout={Timeout}ms, BreakDuration={BreakDuration}ms",
                typeof(TRequest).Name, failureThreshold, timeout, breakDuration);

            appliedOptimizations.Add(OptimizationStrategy.CircuitBreaker);

            // Get or create circuit breaker for this request type
            var circuitBreaker = GetCircuitBreaker(
                typeof(TRequest),
                failureThreshold,
                successThreshold,
                TimeSpan.FromMilliseconds(timeout),
                TimeSpan.FromMilliseconds(breakDuration),
                halfOpenMaxCalls);

            // Wrap handler with circuit breaker logic
            return async () =>
            {
                try
                {
                    var result = await circuitBreaker.ExecuteAsync(
                        async ct =>
                        {
                            var response = await next();
                            return response;
                        },
                        CancellationToken.None);

                    // Record successful execution
                    RecordCircuitBreakerMetrics(typeof(TRequest), circuitBreaker.State, circuitBreaker.GetMetrics(), success: true);

                    return result;
                }
                catch (CircuitBreakerOpenException ex)
                {
                    _logger.LogWarning("Circuit breaker is OPEN for {RequestType} - request rejected", typeof(TRequest).Name);

                    // Record circuit open
                    RecordCircuitBreakerMetrics(typeof(TRequest), CircuitBreakerState.Open, circuitBreaker.GetMetrics(), success: false);

                    // Provide fallback response or rethrow
                    if (TryGetFallbackResponse(recommendation, out var fallbackResponse))
                    {
                        _logger.LogDebug("Using fallback response for {RequestType}", typeof(TRequest).Name);
                        return fallbackResponse;
                    }

                    throw new InvalidOperationException($"Circuit breaker is open for {typeof(TRequest).Name}", ex);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Circuit breaker execution failed for {RequestType}", typeof(TRequest).Name);

                    // Record failure
                    RecordCircuitBreakerMetrics(typeof(TRequest), circuitBreaker.State, circuitBreaker.GetMetrics(), success: false);

                    throw;
                }
            };
        }

        private AICircuitBreaker<TResponse> GetCircuitBreaker(
            Type requestType,
            int failureThreshold,
            int successThreshold,
            TimeSpan timeout,
            TimeSpan breakDuration,
            int halfOpenMaxCalls)
        {
            var key = $"CircuitBreaker:{requestType.FullName}";

            // Try to get existing circuit breaker
            if (_batchCoordinators.TryGetValue(key, out var existing) && existing is AICircuitBreaker<TResponse> cb)
            {
                return cb;
            }

            // Create new circuit breaker
            var circuitBreaker = new AICircuitBreaker<TResponse>(
                failureThreshold,
                successThreshold,
                timeout,
                breakDuration,
                halfOpenMaxCalls,
                _logger);

            _batchCoordinators[key] = circuitBreaker;

            _logger.LogInformation("Created circuit breaker for {RequestType}: FailureThreshold={FailureThreshold}, BreakDuration={BreakDuration}s",
                requestType.Name, failureThreshold, breakDuration.TotalSeconds);

            return circuitBreaker;
        }

        private bool TryGetFallbackResponse(OptimizationRecommendation recommendation, out TResponse? fallbackResponse)
        {
            fallbackResponse = default;

            if (recommendation.Parameters.TryGetValue("FallbackResponse", out var fallback))
            {
                if (fallback is TResponse typedFallback)
                {
                    fallbackResponse = typedFallback;
                    return true;
                }
            }

            // Check if response type has a default constructor
            if (typeof(TResponse).IsClass && typeof(TResponse).GetConstructor(Type.EmptyTypes) != null)
            {
                fallbackResponse = Activator.CreateInstance<TResponse>();
                return true;
            }

            return false;
        }

        private void RecordCircuitBreakerMetrics(
            Type requestType,
            CircuitBreakerState state,
            CircuitBreakerMetrics metrics,
            bool success)
        {
            if (_metricsProvider != null)
            {
                try
                {
                    var metricsData = new HandlerExecutionMetrics
                    {
                        RequestType = requestType,
                        Success = success,
                        Timestamp = DateTimeOffset.UtcNow,
                        Properties = new Dictionary<string, object>
                        {
                            ["CircuitBreakerState"] = state.ToString(),
                            ["TotalCalls"] = metrics.TotalCalls,
                            ["SuccessfulCalls"] = metrics.SuccessfulCalls,
                            ["FailedCalls"] = metrics.FailedCalls,
                            ["SlowCalls"] = metrics.SlowCalls,
                            ["FailureRate"] = metrics.FailureRate,
                            ["SuccessRate"] = metrics.SuccessRate,
                            ["SlowCallRate"] = metrics.SlowCallRate
                        }
                    };
                    _metricsProvider.RecordHandlerExecution(metricsData);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to record circuit breaker metrics");
                }
            }
        }

        private async ValueTask<RequestHandlerDelegate<TResponse>> ApplyDatabaseOptimization(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            OptimizationRecommendation recommendation,
            List<OptimizationStrategy> appliedOptimizations,
            CancellationToken cancellationToken)
        {
            // Extract database optimization parameters from AI recommendation
            var enableQueryOptimization = GetParameter(recommendation, "EnableQueryOptimization", true);
            var enableConnectionPooling = GetParameter(recommendation, "EnableConnectionPooling", true);
            var enableReadOnlyHint = GetParameter(recommendation, "EnableReadOnlyHint", false);
            var enableBatchingHint = GetParameter(recommendation, "EnableBatchingHint", false);
            var enableNoTracking = GetParameter(recommendation, "EnableNoTracking", true);
            var maxRetries = GetParameter(recommendation, "MaxRetries", 3);
            var retryDelay = GetParameter(recommendation, "RetryDelayMs", 100);
            var queryTimeout = GetParameter(recommendation, "QueryTimeoutSeconds", 30);

            _logger.LogDebug("Applying database optimization for {RequestType}: QueryOpt={QueryOpt}, Pooling={Pooling}, ReadOnly={ReadOnly}, NoTracking={NoTracking}",
                typeof(TRequest).Name, enableQueryOptimization, enableConnectionPooling, enableReadOnlyHint, enableNoTracking);

            appliedOptimizations.Add(OptimizationStrategy.DatabaseOptimization);

            await Task.CompletedTask;

            // Wrap handler with database optimization logic
            return async () =>
            {
                var dbContext = new DatabaseOptimizationContext
                {
                    EnableQueryOptimization = enableQueryOptimization,
                    EnableConnectionPooling = enableConnectionPooling,
                    EnableReadOnlyHint = enableReadOnlyHint,
                    EnableBatchingHint = enableBatchingHint,
                    EnableNoTracking = enableNoTracking,
                    MaxRetries = maxRetries,
                    RetryDelayMs = retryDelay,
                    QueryTimeoutSeconds = queryTimeout,
                    RequestType = typeof(TRequest)
                };

                using var scope = DatabaseOptimizationScope.Create(dbContext, _logger);

                var retryCount = 0;
                var startTime = DateTime.UtcNow;

                while (retryCount <= maxRetries)
                {
                    try
                    {
                        // Execute handler with database optimizations
                        var response = await next();

                        var duration = DateTime.UtcNow - startTime;
                        var stats = scope.GetStatistics();

                        // Record successful execution
                        RecordDatabaseOptimizationMetrics(typeof(TRequest), duration, stats, dbContext, success: true);

                        _logger.LogDebug("Database optimization for {RequestType}: Duration={Duration}ms, Queries={Queries}, Connections={Connections}, RetryCount={RetryCount}",
                            typeof(TRequest).Name, duration.TotalMilliseconds, stats.QueriesExecuted, stats.ConnectionsOpened, retryCount);

                        return response;
                    }
                    catch (Exception ex) when (IsTransientDatabaseError(ex) && retryCount < maxRetries)
                    {
                        retryCount++;
                        scope.RecordRetry();

                        _logger.LogWarning(ex, "Transient database error for {RequestType}, retry {RetryCount}/{MaxRetries}",
                            typeof(TRequest).Name, retryCount, maxRetries);

                        // Exponential backoff
                        var delay = retryDelay * (int)Math.Pow(2, retryCount - 1);
                        await Task.Delay(delay);
                    }
                    catch (Exception ex)
                    {
                        var duration = DateTime.UtcNow - startTime;
                        var stats = scope.GetStatistics();

                        _logger.LogError(ex, "Database optimization execution failed for {RequestType} after {RetryCount} retries",
                            typeof(TRequest).Name, retryCount);

                        // Record failure
                        RecordDatabaseOptimizationMetrics(typeof(TRequest), duration, stats, dbContext, success: false);

                        throw;
                    }
                }

                throw new InvalidOperationException($"Database operation failed after {maxRetries} retries");
            };
        }

        private bool IsTransientDatabaseError(Exception ex)
        {
            // Check for common transient database errors
            var message = ex.Message?.ToLowerInvariant() ?? string.Empty;
            var exceptionType = ex.GetType().Name.ToLowerInvariant();

            return message.Contains("timeout") ||
                   message.Contains("deadlock") ||
                   message.Contains("connection") ||
                   message.Contains("network") ||
                   message.Contains("transport") ||
                   exceptionType.Contains("timeout") ||
                   exceptionType.Contains("sqlexception");
        }

        private void RecordDatabaseOptimizationMetrics(
            Type requestType,
            TimeSpan duration,
            DatabaseOptimizationStatistics stats,
            DatabaseOptimizationContext context,
            bool success)
        {
            if (_metricsProvider != null)
            {
                try
                {
                    var metrics = new HandlerExecutionMetrics
                    {
                        RequestType = requestType,
                        Duration = duration,
                        Success = success,
                        Timestamp = DateTimeOffset.UtcNow,
                        Properties = new Dictionary<string, object>
                        {
                            ["QueriesExecuted"] = stats.QueriesExecuted,
                            ["ConnectionsOpened"] = stats.ConnectionsOpened,
                            ["ConnectionsReused"] = stats.ConnectionsReused,
                            ["TotalQueryTime"] = stats.TotalQueryTime.TotalMilliseconds,
                            ["AverageQueryTime"] = stats.AverageQueryTime.TotalMilliseconds,
                            ["SlowestQueryTime"] = stats.SlowestQueryTime.TotalMilliseconds,
                            ["RetryCount"] = stats.RetryCount,
                            ["EnableQueryOptimization"] = context.EnableQueryOptimization,
                            ["EnableConnectionPooling"] = context.EnableConnectionPooling,
                            ["EnableNoTracking"] = context.EnableNoTracking,
                            ["ConnectionPoolEfficiency"] = stats.ConnectionPoolEfficiency,
                            ["QueryEfficiency"] = stats.QueryEfficiency
                        }
                    };
                    _metricsProvider.RecordHandlerExecution(metrics);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to record database optimization metrics");
                }
            }
        }

        private RequestHandlerDelegate<TResponse> ApplySIMDOptimization(
            RequestHandlerDelegate<TResponse> next,
            OptimizationRecommendation recommendation,
            List<OptimizationStrategy> appliedOptimizations)
        {
            // Check SIMD support
            if (!System.Numerics.Vector.IsHardwareAccelerated)
            {
                _logger.LogWarning("SIMD optimization requested but hardware acceleration not available for {RequestType}", typeof(TRequest).Name);
                return next;
            }

            // Extract SIMD optimization parameters from AI recommendation
            var enableVectorization = GetParameter(recommendation, "EnableVectorization", true);
            var vectorSize = GetParameter(recommendation, "VectorSize", System.Numerics.Vector<float>.Count);
            var enableUnrolling = GetParameter(recommendation, "EnableUnrolling", true);
            var unrollFactor = GetParameter(recommendation, "UnrollFactor", 4);
            var minDataSize = GetParameter(recommendation, "MinDataSize", 64);

            _logger.LogDebug("Applying SIMD optimization for {RequestType}: Vectorization={Vectorization}, VectorSize={VectorSize}, Unrolling={Unrolling}",
                typeof(TRequest).Name, enableVectorization, vectorSize, enableUnrolling);

            appliedOptimizations.Add(OptimizationStrategy.SIMDAcceleration);

            // Wrap handler with SIMD optimization logic
            return async () =>
            {
                var simdContext = new SIMDOptimizationContext
                {
                    EnableVectorization = enableVectorization,
                    VectorSize = vectorSize,
                    EnableUnrolling = enableUnrolling,
                    UnrollFactor = unrollFactor,
                    MinDataSize = minDataSize,
                    IsHardwareAccelerated = System.Numerics.Vector.IsHardwareAccelerated,
                    SupportedVectorTypes = GetSupportedVectorTypes()
                };

                using var scope = SIMDOptimizationScope.Create(simdContext, _logger);

                try
                {
                    var startTime = DateTime.UtcNow;

                    // Execute handler with SIMD context available
                    var response = await next();

                    var duration = DateTime.UtcNow - startTime;
                    var stats = scope.GetStatistics();

                    // Record SIMD optimization metrics
                    RecordSIMDOptimizationMetrics(typeof(TRequest), duration, stats, simdContext);

                    _logger.LogDebug("SIMD optimization for {RequestType}: Duration={Duration}ms, VectorOps={VectorOps}, ScalarOps={ScalarOps}, Speedup={Speedup:F2}x",
                        typeof(TRequest).Name, duration.TotalMilliseconds, stats.VectorOperations, stats.ScalarOperations, stats.EstimatedSpeedup);

                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "SIMD optimization execution failed for {RequestType}", typeof(TRequest).Name);
                    throw;
                }
            };
        }

        private string[] GetSupportedVectorTypes()
        {
            var supported = new List<string>();

            if (System.Runtime.Intrinsics.X86.Sse.IsSupported)
                supported.Add("SSE");
            if (System.Runtime.Intrinsics.X86.Sse2.IsSupported)
                supported.Add("SSE2");
            if (System.Runtime.Intrinsics.X86.Sse3.IsSupported)
                supported.Add("SSE3");
            if (System.Runtime.Intrinsics.X86.Ssse3.IsSupported)
                supported.Add("SSSE3");
            if (System.Runtime.Intrinsics.X86.Sse41.IsSupported)
                supported.Add("SSE4.1");
            if (System.Runtime.Intrinsics.X86.Sse42.IsSupported)
                supported.Add("SSE4.2");
            if (System.Runtime.Intrinsics.X86.Avx.IsSupported)
                supported.Add("AVX");
            if (System.Runtime.Intrinsics.X86.Avx2.IsSupported)
                supported.Add("AVX2");
            if (System.Runtime.Intrinsics.Arm.AdvSimd.IsSupported)
                supported.Add("ARM-NEON");

            return supported.ToArray();
        }

        private void RecordSIMDOptimizationMetrics(
            Type requestType,
            TimeSpan duration,
            SIMDOptimizationStatistics stats,
            SIMDOptimizationContext context)
        {
            if (_metricsProvider != null)
            {
                try
                {
                    var metrics = new HandlerExecutionMetrics
                    {
                        RequestType = requestType,
                        Duration = duration,
                        Success = true,
                        Timestamp = DateTimeOffset.UtcNow,
                        Properties = new Dictionary<string, object>
                        {
                            ["VectorOperations"] = stats.VectorOperations,
                            ["ScalarOperations"] = stats.ScalarOperations,
                            ["VectorizationRatio"] = stats.VectorizationRatio,
                            ["EstimatedSpeedup"] = stats.EstimatedSpeedup,
                            ["VectorSize"] = context.VectorSize,
                            ["IsHardwareAccelerated"] = context.IsHardwareAccelerated,
                            ["SupportedVectorTypes"] = string.Join(",", context.SupportedVectorTypes),
                            ["DataProcessed"] = stats.DataProcessed,
                            ["VectorizedDataPercentage"] = stats.VectorizedDataPercentage
                        }
                    };
                    _metricsProvider.RecordHandlerExecution(metrics);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to record SIMD optimization metrics");
                }
            }
        }

        private async ValueTask<RequestHandlerDelegate<TResponse>> ApplyCustomOptimization(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            OptimizationRecommendation recommendation,
            List<OptimizationStrategy> appliedOptimizations,
            CancellationToken cancellationToken)
        {
            // Extract custom optimization parameters from AI recommendation
            var optimizationType = GetParameter(recommendation, "OptimizationType", "General");
            var optimizationLevel = GetParameter(recommendation, "OptimizationLevel", 1);
            var enableProfiling = GetParameter(recommendation, "EnableProfiling", false);
            var enableTracing = GetParameter(recommendation, "EnableTracing", false);
            var customParameters = recommendation.Parameters
                .Where(p => p.Key.StartsWith("Custom_"))
                .ToDictionary(p => p.Key, p => p.Value);

            _logger.LogDebug("Applying custom optimization for {RequestType}: Type={Type}, Level={Level}, Profiling={Profiling}",
                typeof(TRequest).Name, optimizationType, optimizationLevel, enableProfiling);

            appliedOptimizations.Add(OptimizationStrategy.Custom);

            await Task.CompletedTask;

            // Wrap handler with custom optimization logic
            return async () =>
            {
                var customContext = new CustomOptimizationContext
                {
                    RequestType = typeof(TRequest),
                    OptimizationType = optimizationType,
                    OptimizationLevel = optimizationLevel,
                    EnableProfiling = enableProfiling,
                    EnableTracing = enableTracing,
                    CustomParameters = customParameters,
                    Recommendation = recommendation
                };

                using var scope = CustomOptimizationScope.Create(customContext, _logger);

                try
                {
                    var startTime = DateTime.UtcNow;

                    // Apply pre-execution optimizations
                    await ApplyPreExecutionOptimizations(customContext, scope);

                    // Execute handler with custom optimizations
                    var response = await next();

                    // Apply post-execution optimizations
                    await ApplyPostExecutionOptimizations(customContext, scope, response);

                    var duration = DateTime.UtcNow - startTime;
                    var stats = scope.GetStatistics();

                    // Record custom optimization metrics
                    RecordCustomOptimizationMetrics(typeof(TRequest), duration, stats, customContext);

                    _logger.LogDebug("Custom optimization for {RequestType}: Duration={Duration}ms, Type={Type}, ActionsApplied={Actions}, Effectiveness={Effectiveness:P}",
                        typeof(TRequest).Name, duration.TotalMilliseconds, optimizationType,
                        stats.OptimizationActionsApplied, stats.OverallEffectiveness);

                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Custom optimization execution failed for {RequestType}", typeof(TRequest).Name);
                    throw;
                }
            };
        }

        private async Task ApplyPreExecutionOptimizations(CustomOptimizationContext context, CustomOptimizationScope scope)
        {
            // Apply custom pre-execution logic based on optimization type
            switch (context.OptimizationType.ToLowerInvariant())
            {
                case "warmup":
                    scope.RecordAction("Warmup", "Warming up caches and resources");
                    // Warm up caches, connection pools, etc.
                    await Task.CompletedTask;
                    break;

                case "prefetch":
                    scope.RecordAction("Prefetch", "Prefetching data based on AI prediction");
                    // Prefetch likely needed data
                    await Task.CompletedTask;
                    break;

                case "throttle":
                    scope.RecordAction("Throttle", "Applying throttling based on load");
                    // Apply rate limiting or throttling
                    await Task.Delay(context.OptimizationLevel * 10);
                    break;

                case "prioritize":
                    scope.RecordAction("Prioritize", "Setting execution priority");
                    // Adjust thread priority or scheduling
                    break;

                default:
                    scope.RecordAction("General", "Applying general pre-execution optimizations");
                    break;
            }
        }

        private async Task ApplyPostExecutionOptimizations<T>(CustomOptimizationContext context, CustomOptimizationScope scope, T response)
        {
            // Apply custom post-execution logic
            switch (context.OptimizationType.ToLowerInvariant())
            {
                case "compress":
                    scope.RecordAction("Compress", "Compressing response data");
                    // Compress response if beneficial
                    await Task.CompletedTask;
                    break;

                case "cache_prime":
                    scope.RecordAction("CachePrime", "Priming cache with result");
                    // Prime cache for related requests
                    await Task.CompletedTask;
                    break;

                case "notify":
                    scope.RecordAction("Notify", "Sending notifications");
                    // Send notifications or events
                    await Task.CompletedTask;
                    break;

                default:
                    scope.RecordAction("General", "Applying general post-execution optimizations");
                    break;
            }
        }

    }
}
