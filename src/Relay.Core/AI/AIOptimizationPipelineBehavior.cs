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
using Microsoft.Extensions.Options;
using Relay.Core.Telemetry;

namespace Relay.Core.AI
{
    /// <summary>
    /// Metadata for batch coordinators.
    /// </summary>
    internal sealed class BatchCoordinatorMetadata
    {
        public int BatchSize { get; init; }
        public TimeSpan BatchWindow { get; init; }
        public TimeSpan MaxWaitTime { get; init; }
        public BatchingStrategy Strategy { get; init; }
        public DateTime CreatedAt { get; init; }
        public long RequestCount { get; set; }
        public DateTime LastUsed { get; set; }
        public double AverageWaitTime { get; set; }
        public double AverageBatchSize { get; set; }
    }

    /// <summary>
    /// Interface for batch coordinators.
    /// </summary>
    internal interface IBatchCoordinator
    {
        BatchCoordinatorMetadata? GetMetadata();
    }

    /// <summary>
    /// Represents a batch item waiting for processing.
    /// </summary>
    internal sealed class BatchItem<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public TRequest Request { get; init; } = default!;
        public RequestHandlerDelegate<TResponse> Handler { get; init; } = default!;
        public CancellationToken CancellationToken { get; init; }
        public DateTime EnqueueTime { get; init; }
        public Guid BatchId { get; init; }
        public TaskCompletionSource<BatchExecutionResult<TResponse>> CompletionSource { get; } = new();
    }

    /// <summary>
    /// Result of batch execution.
    /// </summary>
    internal sealed class BatchExecutionResult<TResponse>
    {
        public TResponse Response { get; init; } = default!;
        public int BatchSize { get; init; }
        public TimeSpan WaitTime { get; init; }
        public TimeSpan ExecutionTime { get; init; }
        public bool Success { get; init; }
        public BatchingStrategy Strategy { get; init; }
        public double Efficiency { get; init; }
    }

    /// <summary>
    /// Coordinates batch processing of requests.
    /// </summary>
    internal sealed class BatchCoordinator<TRequest, TResponse> : IBatchCoordinator, IDisposable
        where TRequest : IRequest<TResponse>
    {
        private readonly int _batchSize;
        private readonly TimeSpan _batchWindow;
        private readonly TimeSpan _maxWaitTime;
        private readonly BatchingStrategy _strategy;
        private readonly ILogger _logger;
        private readonly System.Collections.Concurrent.ConcurrentQueue<BatchItem<TRequest, TResponse>> _queue = new();
        private readonly SemaphoreSlim _signal = new(0);
        private readonly System.Threading.Timer _timer;
        private int _queuedCount = 0;
        private DateTime _batchStartTime = DateTime.UtcNow;
        private bool _disposed = false;

        public BatchCoordinatorMetadata? Metadata { get; set; }

        public BatchCoordinator(
            int batchSize,
            TimeSpan batchWindow,
            TimeSpan maxWaitTime,
            BatchingStrategy strategy,
            ILogger logger)
        {
            _batchSize = batchSize;
            _batchWindow = batchWindow;
            _maxWaitTime = maxWaitTime;
            _strategy = strategy;
            _logger = logger;

            // Start batch processing timer
            _timer = new System.Threading.Timer(
                _ => TriggerBatchProcessing(),
                null,
                batchWindow,
                batchWindow);
        }

        public BatchCoordinatorMetadata? GetMetadata() => Metadata;

        public async ValueTask<BatchExecutionResult<TResponse>> EnqueueAndWaitAsync(
            BatchItem<TRequest, TResponse> item,
            CancellationToken cancellationToken)
        {
            _queue.Enqueue(item);
            var count = System.Threading.Interlocked.Increment(ref _queuedCount);

            // Trigger batch if size reached
            if (count >= _batchSize)
            {
                _signal.Release();
            }

            // Wait for batch execution with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_maxWaitTime);

            try
            {
                return await item.CompletionSource.Task.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Timeout or cancellation - execute individually as fallback
                var response = await item.Handler();
                return new BatchExecutionResult<TResponse>
                {
                    Response = response,
                    BatchSize = 1,
                    WaitTime = DateTime.UtcNow - item.EnqueueTime,
                    ExecutionTime = TimeSpan.Zero,
                    Success = true,
                    Strategy = _strategy,
                    Efficiency = 0.0
                };
            }
        }

        private void TriggerBatchProcessing()
        {
            if (_queuedCount > 0)
            {
                _ = ProcessBatchAsync();
            }
        }

        private async Task ProcessBatchAsync()
        {
            var items = new List<BatchItem<TRequest, TResponse>>();
            var batchStartTime = DateTime.UtcNow;

            // Dequeue items up to batch size
            var itemsToDequeue = Math.Min(_batchSize, _queuedCount);
            for (int i = 0; i < itemsToDequeue; i++)
            {
                if (_queue.TryDequeue(out var item))
                {
                    items.Add(item);
                    System.Threading.Interlocked.Decrement(ref _queuedCount);
                }
            }

            if (items.Count == 0)
                return;

            _logger.LogDebug("Processing batch of {Count} items", items.Count);

            // Process all items in parallel
            var executionStartTime = DateTime.UtcNow;
            var tasks = items.Select(async item =>
            {
                try
                {
                    var response = await item.Handler();
                    var waitTime = executionStartTime - item.EnqueueTime;
                    var executionTime = DateTime.UtcNow - executionStartTime;

                    var result = new BatchExecutionResult<TResponse>
                    {
                        Response = response,
                        BatchSize = items.Count,
                        WaitTime = waitTime,
                        ExecutionTime = executionTime,
                        Success = true,
                        Strategy = _strategy,
                        Efficiency = CalculateEfficiency(items.Count, waitTime, executionTime)
                    };

                    item.CompletionSource.SetResult(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Batch item execution failed");
                    item.CompletionSource.SetException(ex);
                }
            });

            await Task.WhenAll(tasks);
        }

        private double CalculateEfficiency(int batchSize, TimeSpan waitTime, TimeSpan executionTime)
        {
            // Efficiency = (batch size benefit) / (wait time cost)
            // Higher batch size increases efficiency, longer wait time decreases it
            var sizeFactor = Math.Log(batchSize + 1) / Math.Log(_batchSize + 1);
            var waitPenalty = Math.Min(1.0, waitTime.TotalMilliseconds / _maxWaitTime.TotalMilliseconds);
            return Math.Clamp(sizeFactor * (1.0 - waitPenalty * 0.5), 0.0, 1.0);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _timer?.Dispose();
                _signal?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Pipeline behavior that integrates AI optimization engine into the request processing pipeline.
    /// Monitors request execution, applies AI-recommended optimizations, and learns from results.
    /// </summary>
    public sealed class AIOptimizationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IAIOptimizationEngine _aiEngine;
        private readonly ILogger<AIOptimizationPipelineBehavior<TRequest, TResponse>> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly SystemLoadMetricsProvider _systemMetrics;
        private readonly IMetricsProvider? _metricsProvider;
        private readonly IMemoryCache? _memoryCache;
        private readonly IDistributedCache? _distributedCache;

        // Batch coordinator registry - one coordinator per request type
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, object> _batchCoordinators = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> _coordinatorLocks = new();

        public AIOptimizationPipelineBehavior(
            IAIOptimizationEngine aiEngine,
            ILogger<AIOptimizationPipelineBehavior<TRequest, TResponse>> logger,
            IOptions<AIOptimizationOptions> options,
            SystemLoadMetricsProvider systemMetrics,
            IMetricsProvider? metricsProvider = null,
            IMemoryCache? memoryCache = null,
            IDistributedCache? distributedCache = null)
        {
            _aiEngine = aiEngine ?? throw new ArgumentNullException(nameof(aiEngine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _systemMetrics = systemMetrics ?? throw new ArgumentNullException(nameof(systemMetrics));
            _metricsProvider = metricsProvider;
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
        }

        public async ValueTask<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!_options.Enabled)
            {
                return await next();
            }

            var requestType = typeof(TRequest);
            var stopwatch = Stopwatch.StartNew();
            var startMemory = GC.GetTotalMemory(false);
            var appliedOptimizations = new List<OptimizationStrategy>();

            try
            {
                // Check for AI optimization attributes
                var aiAttributes = GetAIOptimizationAttributes(requestType);
                var shouldOptimize = ShouldPerformOptimization(aiAttributes);

                if (!shouldOptimize)
                {
                    return await next();
                }

                // Collect current system metrics
                var systemLoad = await _systemMetrics.GetCurrentLoadAsync(cancellationToken);

                // Get AI recommendations
                var executionMetrics = await GetHistoricalMetrics(requestType, cancellationToken);
                
                // Cast the request for AI analysis (constraint ensures this is safe)
                if (request is IRequest aiRequest)
                {
                    var recommendation = await _aiEngine.AnalyzeRequestAsync(aiRequest, executionMetrics, cancellationToken);

                    _logger.LogDebug("AI recommendation for {RequestType}: {Strategy} (Confidence: {Confidence:P})",
                        requestType.Name, recommendation.Strategy, recommendation.ConfidenceScore);

                    // Apply optimizations based on AI recommendations
                    var optimizedNext = await ApplyOptimizations(request, next, recommendation, systemLoad, appliedOptimizations, cancellationToken);

                    // Execute the request with optimizations
                    var response = await optimizedNext();
                    
                    // Continue with metrics recording...
                    stopwatch.Stop();
                    var endMemory = GC.GetTotalMemory(false);

                    // Record execution metrics for learning
                    var actualMetrics = new RequestExecutionMetrics
                    {
                        AverageExecutionTime = stopwatch.Elapsed,
                        MedianExecutionTime = stopwatch.Elapsed,
                        P95ExecutionTime = stopwatch.Elapsed,
                        P99ExecutionTime = stopwatch.Elapsed,
                        TotalExecutions = 1,
                        SuccessfulExecutions = 1,
                        FailedExecutions = 0,
                        MemoryAllocated = Math.Max(0, endMemory - startMemory),
                        ConcurrentExecutions = 1,
                        LastExecution = DateTime.UtcNow,
                        SamplePeriod = stopwatch.Elapsed,
                        CpuUsage = systemLoad.CpuUtilization,
                        MemoryUsage = endMemory,
                        DatabaseCalls = 0,
                        ExternalApiCalls = 0
                    };

                    // Learn from the execution results
                    if (_options.LearningEnabled && appliedOptimizations.Count > 0)
                    {
                        await _aiEngine.LearnFromExecutionAsync(requestType, appliedOptimizations.ToArray(), actualMetrics, cancellationToken);
                    }

                    _logger.LogDebug("AI-optimized execution of {RequestType} completed in {Duration}ms with {OptimizationCount} optimizations",
                        requestType.Name, stopwatch.ElapsedMilliseconds, appliedOptimizations.Count);

                    return response;
                }
                else
                {
                    // Fallback to normal execution if not an IRequest
                    return await next();
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var endMemory = GC.GetTotalMemory(false);

                // Record failed execution metrics
                var failedMetrics = new RequestExecutionMetrics
                {
                    AverageExecutionTime = stopwatch.Elapsed,
                    MedianExecutionTime = stopwatch.Elapsed,
                    P95ExecutionTime = stopwatch.Elapsed,
                    P99ExecutionTime = stopwatch.Elapsed,
                    TotalExecutions = 1,
                    SuccessfulExecutions = 0,
                    FailedExecutions = 1,
                    MemoryAllocated = Math.Max(0, endMemory - startMemory),
                    ConcurrentExecutions = 1,
                    LastExecution = DateTime.UtcNow,
                    SamplePeriod = stopwatch.Elapsed,
                    CpuUsage = (await _systemMetrics.GetCurrentLoadAsync(cancellationToken)).CpuUtilization,
                    MemoryUsage = endMemory
                };

                // Learn from failed execution
                if (_options.LearningEnabled && appliedOptimizations.Count > 0)
                {
                    await _aiEngine.LearnFromExecutionAsync(requestType, appliedOptimizations.ToArray(), failedMetrics, cancellationToken);
                }

                _logger.LogWarning(ex, "AI-optimized execution of {RequestType} failed after {Duration}ms with {OptimizationCount} optimizations",
                    requestType.Name, stopwatch.ElapsedMilliseconds, appliedOptimizations.Count);

                throw;
            }
        }

        private AIOptimizedAttribute[] GetAIOptimizationAttributes(Type requestType)
        {
            var attributes = new List<AIOptimizedAttribute>();

            // Check request type attributes
            attributes.AddRange(requestType.GetCustomAttributes<AIOptimizedAttribute>());

            // Discover and check handler type attributes
            var handlerType = FindHandlerType(requestType);
            if (handlerType != null)
            {
                // Check handler class attributes
                attributes.AddRange(handlerType.GetCustomAttributes<AIOptimizedAttribute>());

                // Check handler method attributes (HandleAsync)
                var handlerMethod = FindHandlerMethod(handlerType, requestType);
                if (handlerMethod != null)
                {
                    attributes.AddRange(handlerMethod.GetCustomAttributes<AIOptimizedAttribute>());
                }
            }

            return attributes.ToArray();
        }

        private Type? FindHandlerType(Type requestType)
        {
            // Determine response type
            Type? responseType = null;
            Type handlerInterfaceType;

            // Check if request implements IRequest<TResponse>
            var requestInterface = requestType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

            if (requestInterface != null)
            {
                responseType = requestInterface.GetGenericArguments()[0];
                handlerInterfaceType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
            }
            else if (typeof(IRequest).IsAssignableFrom(requestType))
            {
                // Request without response
                handlerInterfaceType = typeof(IRequestHandler<>).MakeGenericType(requestType);
            }
            else
            {
                // Check if it's a stream request
                var streamInterface = requestType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamRequest<>));

                if (streamInterface != null)
                {
                    responseType = streamInterface.GetGenericArguments()[0];
                    handlerInterfaceType = typeof(IStreamHandler<,>).MakeGenericType(requestType, responseType);
                }
                else
                {
                    return null;
                }
            }

            // Search for handler implementation in loaded assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !a.ReflectionOnly);

            foreach (var assembly in assemblies)
            {
                try
                {
                    var handlerTypes = assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && handlerInterfaceType.IsAssignableFrom(t));

                    var handler = handlerTypes.FirstOrDefault();
                    if (handler != null)
                    {
                        return handler;
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip assemblies that can't be loaded
                    continue;
                }
            }

            return null;
        }

        private MethodInfo? FindHandlerMethod(Type handlerType, Type requestType)
        {
            // Find the HandleAsync method
            var methods = handlerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == "HandleAsync" && m.GetParameters().Length >= 1);

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length > 0 &&
                    (parameters[0].ParameterType == requestType ||
                     parameters[0].ParameterType.IsAssignableFrom(requestType)))
                {
                    return method;
                }
            }

            return null;
        }

        private bool ShouldPerformOptimization(AIOptimizedAttribute[] attributes)
        {
            if (attributes.Length == 0)
                return _options.Enabled; // Default behavior when no specific attributes

            return attributes.Any(attr => attr.EnableMetricsTracking || attr.AutoApplyOptimizations);
        }

        private async ValueTask<RequestExecutionMetrics> GetHistoricalMetrics(Type requestType, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            // Try to get historical metrics from the metrics provider
            if (_metricsProvider != null)
            {
                try
                {
                    var stats = _metricsProvider.GetHandlerExecutionStats(requestType);

                    if (stats != null && stats.TotalExecutions > 0)
                    {
                        // Convert telemetry stats to AI execution metrics
                        return new RequestExecutionMetrics
                        {
                            AverageExecutionTime = stats.AverageExecutionTime,
                            MedianExecutionTime = stats.P50ExecutionTime,
                            P95ExecutionTime = stats.P95ExecutionTime,
                            P99ExecutionTime = stats.P99ExecutionTime,
                            TotalExecutions = stats.TotalExecutions,
                            SuccessfulExecutions = stats.SuccessfulExecutions,
                            FailedExecutions = stats.FailedExecutions,
                            MemoryAllocated = EstimateMemoryUsage(stats),
                            ConcurrentExecutions = EstimateConcurrentExecutions(stats),
                            LastExecution = stats.LastExecution.DateTime,
                            SamplePeriod = CalculateSamplePeriod(stats),
                            CpuUsage = EstimateCpuUsage(stats),
                            MemoryUsage = EstimateMemoryUsage(stats),
                            DatabaseCalls = ExtractDatabaseCalls(stats),
                            ExternalApiCalls = ExtractExternalApiCalls(stats)
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to retrieve historical metrics for {RequestType}", requestType.Name);
                }
            }

            // Return default metrics if no historical data is available
            return new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                MedianExecutionTime = TimeSpan.FromMilliseconds(80),
                P95ExecutionTime = TimeSpan.FromMilliseconds(200),
                P99ExecutionTime = TimeSpan.FromMilliseconds(500),
                TotalExecutions = 0, // Indicate no historical data
                SuccessfulExecutions = 0,
                FailedExecutions = 0,
                MemoryAllocated = 1024 * 512, // 512KB default
                ConcurrentExecutions = 1,
                LastExecution = DateTime.UtcNow.AddMinutes(-1),
                SamplePeriod = TimeSpan.FromHours(1),
                CpuUsage = 0.3,
                MemoryUsage = 1024 * 1024 * 100, // 100MB default
                DatabaseCalls = 0,
                ExternalApiCalls = 0
            };
        }

        private long EstimateMemoryUsage(HandlerExecutionStats stats)
        {
            // First, check if we have actual memory allocation data
            if (stats.AverageMemoryAllocated > 0)
            {
                return stats.AverageMemoryAllocated;
            }

            if (stats.TotalMemoryAllocated > 0 && stats.TotalExecutions > 0)
            {
                return stats.TotalMemoryAllocated / stats.TotalExecutions;
            }

            // Check if memory data is available in properties
            if (stats.Properties.TryGetValue("AverageMemoryBytes", out var memObj) && memObj is long avgMem)
            {
                return avgMem;
            }

            if (stats.Properties.TryGetValue("MemoryPerExecution", out var memPerExecObj) && memPerExecObj is long memPerExec)
            {
                return memPerExec;
            }

            // Fall back to estimation based on execution patterns
            return EstimateMemoryFromExecutionPatterns(stats);
        }

        private long EstimateMemoryFromExecutionPatterns(HandlerExecutionStats stats)
        {
            // Base memory estimate using execution time as a proxy
            var avgMs = stats.AverageExecutionTime.TotalMilliseconds;
            var baseEstimate = (long)(avgMs * 1024 * 5); // 5KB per millisecond as baseline

            // Adjust based on execution time variance (higher variance = more memory allocations)
            var executionTimeVariance = CalculateExecutionTimeVariance(stats);
            var varianceFactor = 1.0 + (executionTimeVariance * 0.5); // Up to 50% increase for high variance

            // Adjust based on failure rate (failed executions often allocate more memory for exceptions)
            var failureRate = stats.TotalExecutions > 0
                ? (double)stats.FailedExecutions / stats.TotalExecutions
                : 0.0;
            var failureFactor = 1.0 + (failureRate * 0.3); // Up to 30% increase for high failure rate

            // Adjust based on execution frequency (high frequency may benefit from object pooling)
            var executionFrequency = CalculateExecutionFrequency(stats);
            var frequencyFactor = executionFrequency > 10
                ? 0.8 // 20% reduction for high-frequency handlers (likely using pooling)
                : 1.0;

            // Calculate final estimate with all adjustments
            var estimate = (long)(baseEstimate * varianceFactor * failureFactor * frequencyFactor);

            // Apply reasonable bounds (min 1KB, max 100MB per execution)
            return Math.Clamp(estimate, 1024, 100 * 1024 * 1024);
        }

        private double CalculateExecutionTimeVariance(HandlerExecutionStats stats)
        {
            // Calculate coefficient of variation (CV) as a measure of variance
            // CV = standard deviation / mean
            // We approximate std dev using percentile spread

            var mean = stats.AverageExecutionTime.TotalMilliseconds;
            if (mean <= 0)
                return 0.0;

            // Approximate standard deviation using percentile range
            // For normal distribution: P95 - P50 ≈ 1.645 * std dev
            var p95ToP50Spread = stats.P95ExecutionTime.TotalMilliseconds - stats.P50ExecutionTime.TotalMilliseconds;
            var approximateStdDev = p95ToP50Spread / 1.645;

            var coefficientOfVariation = approximateStdDev / mean;

            // Normalize to 0-1 range (CV > 1 is very high variance)
            return Math.Clamp(coefficientOfVariation, 0.0, 1.0);
        }

        private double CalculateExecutionFrequency(HandlerExecutionStats stats)
        {
            // Calculate executions per second
            var timeSinceLastExecution = DateTime.UtcNow - stats.LastExecution.DateTime;
            var totalSeconds = Math.Max(1.0, timeSinceLastExecution.TotalSeconds);

            return stats.TotalExecutions / totalSeconds;
        }

        private int EstimateConcurrentExecutions(HandlerExecutionStats stats)
        {
            // Estimate concurrent executions based on total executions and timeframe
            // This is a simplified calculation
            var executionsPerSecond = stats.TotalExecutions / Math.Max(1, (DateTime.UtcNow - stats.LastExecution.DateTime).TotalSeconds);
            var avgExecutionSeconds = stats.AverageExecutionTime.TotalSeconds;
            return Math.Max(1, (int)(executionsPerSecond * avgExecutionSeconds));
        }

        private TimeSpan CalculateSamplePeriod(HandlerExecutionStats stats)
        {
            // Calculate the sample period based on last execution time
            var timeSinceLastExecution = DateTime.UtcNow - stats.LastExecution.DateTime;
            return timeSinceLastExecution > TimeSpan.Zero ? timeSinceLastExecution : TimeSpan.FromHours(1);
        }

        private double EstimateCpuUsage(HandlerExecutionStats stats)
        {
            // Estimate CPU usage based on execution time patterns
            // Higher P99/P50 ratio suggests CPU contention
            var p99ToP50Ratio = stats.P99ExecutionTime.TotalMilliseconds / Math.Max(1, stats.P50ExecutionTime.TotalMilliseconds);

            if (p99ToP50Ratio > 5.0)
                return 0.8; // High CPU usage
            else if (p99ToP50Ratio > 3.0)
                return 0.5; // Medium CPU usage
            else
                return 0.3; // Low CPU usage
        }

        private int ExtractDatabaseCalls(HandlerExecutionStats stats)
        {
            // Try to extract database call information from properties
            if (stats.Properties.TryGetValue("DatabaseCalls", out var dbCallsObj))
            {
                if (dbCallsObj is int dbCalls)
                    return dbCalls;
                if (dbCallsObj is long dbCallsLong)
                    return (int)dbCallsLong;
                if (dbCallsObj is double dbCallsDouble)
                    return (int)dbCallsDouble;
            }

            if (stats.Properties.TryGetValue("AvgDatabaseCalls", out var avgDbCallsObj))
            {
                if (avgDbCallsObj is double avgDbCalls)
                    return (int)Math.Round(avgDbCalls);
                if (avgDbCallsObj is int avgDbCallsInt)
                    return avgDbCallsInt;
            }

            // Estimate based on execution time if no data available
            // Longer execution times might indicate database operations
            if (stats.AverageExecutionTime.TotalMilliseconds > 100)
                return (int)(stats.AverageExecutionTime.TotalMilliseconds / 50); // Rough estimate: 1 DB call per 50ms

            return 0;
        }

        private int ExtractExternalApiCalls(HandlerExecutionStats stats)
        {
            // Try to extract external API call information from properties
            if (stats.Properties.TryGetValue("ExternalApiCalls", out var apiCallsObj))
            {
                if (apiCallsObj is int apiCalls)
                    return apiCalls;
                if (apiCallsObj is long apiCallsLong)
                    return (int)apiCallsLong;
                if (apiCallsObj is double apiCallsDouble)
                    return (int)apiCallsDouble;
            }

            if (stats.Properties.TryGetValue("AvgExternalApiCalls", out var avgApiCallsObj))
            {
                if (avgApiCallsObj is double avgApiCalls)
                    return (int)Math.Round(avgApiCalls);
                if (avgApiCallsObj is int avgApiCallsInt)
                    return avgApiCallsInt;
            }

            if (stats.Properties.TryGetValue("HttpCalls", out var httpCallsObj))
            {
                if (httpCallsObj is int httpCalls)
                    return httpCalls;
                if (httpCallsObj is long httpCallsLong)
                    return (int)httpCallsLong;
            }

            // Estimate based on execution time patterns
            // High P99/P50 ratio might indicate external API calls with variable latency
            var p99ToP50Ratio = stats.P99ExecutionTime.TotalMilliseconds / Math.Max(1, stats.P50ExecutionTime.TotalMilliseconds);
            if (p99ToP50Ratio > 4.0 && stats.AverageExecutionTime.TotalMilliseconds > 200)
                return 1; // Likely has external API calls

            return 0;
        }

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

        private void RecordCustomOptimizationMetrics(
            Type requestType,
            TimeSpan duration,
            CustomOptimizationStatistics stats,
            CustomOptimizationContext context)
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
                            ["OptimizationType"] = context.OptimizationType,
                            ["OptimizationLevel"] = context.OptimizationLevel,
                            ["ActionsApplied"] = stats.OptimizationActionsApplied,
                            ["ActionsSucceeded"] = stats.ActionsSucceeded,
                            ["ActionsFailed"] = stats.ActionsFailed,
                            ["OverallEffectiveness"] = stats.OverallEffectiveness,
                            ["EnableProfiling"] = context.EnableProfiling,
                            ["EnableTracing"] = context.EnableTracing,
                            ["CustomParameterCount"] = context.CustomParameters.Count
                        }
                    };

                    // Add custom parameters to metrics
                    foreach (var param in context.CustomParameters)
                    {
                        metrics.Properties[$"Param_{param.Key}"] = param.Value?.ToString() ?? "null";
                    }

                    _metricsProvider.RecordHandlerExecution(metrics);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to record custom optimization metrics");
                }
            }
        }
    }

    /// <summary>
    /// Provides system load metrics for AI optimization decisions.
    /// </summary>
    public sealed class SystemLoadMetricsProvider
    {
        private readonly ILogger<SystemLoadMetricsProvider> _logger;

        public SystemLoadMetricsProvider(ILogger<SystemLoadMetricsProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<SystemLoadMetrics> GetCurrentLoadAsync(CancellationToken cancellationToken = default)
        {
            // In a real implementation, this would collect actual system metrics
            await Task.CompletedTask;

            return new SystemLoadMetrics
            {
                CpuUtilization = GetCpuUtilization(),
                MemoryUtilization = GetMemoryUtilization(),
                AvailableMemory = GC.GetTotalMemory(false),
                ActiveRequestCount = GetActiveRequestCount(),
                QueuedRequestCount = 0,
                ThroughputPerSecond = 100.0,
                AverageResponseTime = TimeSpan.FromMilliseconds(150),
                ErrorRate = 0.02,
                Timestamp = DateTime.UtcNow,
                ActiveConnections = 25,
                DatabasePoolUtilization = 0.4,
                ThreadPoolUtilization = 0.3
            };
        }

        private double GetCpuUtilization()
        {
            try
            {
                // Use Process.TotalProcessorTime to calculate CPU usage
                // This provides accurate CPU utilization for the current process

                var currentProcess = System.Diagnostics.Process.GetCurrentProcess();

                // Get current CPU time and wall clock time
                var startTime = DateTime.UtcNow;
                var startCpuTime = currentProcess.TotalProcessorTime;

                // Small delay to measure CPU usage over a period
                System.Threading.Thread.Sleep(100);

                var endTime = DateTime.UtcNow;
                var endCpuTime = currentProcess.TotalProcessorTime;

                // Calculate CPU usage percentage
                var cpuUsedMs = (endCpuTime - startCpuTime).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

                // Normalize to 0.0 - 1.0 range
                var normalizedCpuUsage = Math.Max(0.0, Math.Min(1.0, cpuUsageTotal));

                _logger.LogTrace(
                    "CPU utilization calculated: {CpuUsage:P2} (CPU time: {CpuTime}ms, Wall time: {WallTime}ms, Cores: {ProcessorCount})",
                    normalizedCpuUsage,
                    cpuUsedMs,
                    totalMsPassed,
                    Environment.ProcessorCount);

                return normalizedCpuUsage;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get accurate CPU utilization, using fallback estimation");

                // Fallback: Use thread pool metrics as a proxy for CPU load
                System.Threading.ThreadPool.GetAvailableThreads(out var workerThreads, out var completionPortThreads);
                System.Threading.ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);

                var threadPoolUtilization = 1.0 - ((double)workerThreads / maxWorkerThreads);

                // Estimate CPU usage based on thread pool utilization
                // Higher thread pool usage typically correlates with higher CPU usage
                var estimatedCpuUsage = Math.Min(1.0, threadPoolUtilization * 0.8 + 0.1);

                _logger.LogTrace(
                    "CPU utilization estimated from thread pool: {CpuUsage:P2} (Available threads: {Available}/{Max})",
                    estimatedCpuUsage,
                    workerThreads,
                    maxWorkerThreads);

                return estimatedCpuUsage;
            }
        }

        private double GetMemoryUtilization()
        {
            var totalMemory = GC.GetTotalMemory(false);
            var maxMemory = 1024L * 1024 * 1024; // 1GB baseline
            return Math.Min(1.0, (double)totalMemory / maxMemory);
        }

        private int GetActiveRequestCount()
        {
            // In real implementation, would track active requests
            return Random.Shared.Next(1, 50);
        }
    }

    /// <summary>
    /// Circuit breaker states.
    /// </summary>
    public enum CircuitBreakerState
    {
        Closed,
        Open,
        HalfOpen
    }

    /// <summary>
    /// Circuit breaker metrics.
    /// </summary>
    public sealed class CircuitBreakerMetrics
    {
        public long TotalCalls { get; init; }
        public long SuccessfulCalls { get; init; }
        public long FailedCalls { get; init; }
        public long SlowCalls { get; init; }
        public double FailureRate => TotalCalls > 0 ? (double)FailedCalls / TotalCalls : 0.0;
        public double SuccessRate => TotalCalls > 0 ? (double)SuccessfulCalls / TotalCalls : 0.0;
        public double SlowCallRate => TotalCalls > 0 ? (double)SlowCalls / TotalCalls : 0.0;
    }

    /// <summary>
    /// Exception thrown when circuit breaker is open.
    /// </summary>
    public sealed class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message) { }
        public CircuitBreakerOpenException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// AI-powered circuit breaker implementation.
    /// </summary>
    internal sealed class AICircuitBreaker<TResponse>
    {
        private readonly int _failureThreshold;
        private readonly int _successThreshold;
        private readonly TimeSpan _timeout;
        private readonly TimeSpan _breakDuration;
        private readonly int _halfOpenMaxCalls;
        private readonly ILogger _logger;

        private CircuitBreakerState _state = CircuitBreakerState.Closed;
        private readonly object _stateLock = new();
        private DateTime _lastStateChange = DateTime.UtcNow;
        private int _consecutiveFailures = 0;
        private int _consecutiveSuccesses = 0;
        private int _halfOpenCalls = 0;

        private long _totalCalls = 0;
        private long _successfulCalls = 0;
        private long _failedCalls = 0;
        private long _slowCalls = 0;

        public CircuitBreakerState State
        {
            get
            {
                lock (_stateLock)
                {
                    return _state;
                }
            }
        }

        public AICircuitBreaker(
            int failureThreshold,
            int successThreshold,
            TimeSpan timeout,
            TimeSpan breakDuration,
            int halfOpenMaxCalls,
            ILogger logger)
        {
            _failureThreshold = failureThreshold;
            _successThreshold = successThreshold;
            _timeout = timeout;
            _breakDuration = breakDuration;
            _halfOpenMaxCalls = halfOpenMaxCalls;
            _logger = logger;
        }

        public async ValueTask<TResponse> ExecuteAsync(
            Func<CancellationToken, ValueTask<TResponse>> operation,
            CancellationToken cancellationToken)
        {
            // Check if circuit should transition from Open to HalfOpen
            CheckForAutomaticTransition();

            // Check current state
            lock (_stateLock)
            {
                if (_state == CircuitBreakerState.Open)
                {
                    throw new CircuitBreakerOpenException($"Circuit breaker is open. Last state change: {_lastStateChange}");
                }

                if (_state == CircuitBreakerState.HalfOpen)
                {
                    if (_halfOpenCalls >= _halfOpenMaxCalls)
                    {
                        throw new CircuitBreakerOpenException("Circuit breaker is half-open and max calls reached");
                    }
                    _halfOpenCalls++;
                }
            }

            System.Threading.Interlocked.Increment(ref _totalCalls);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Execute with timeout
                var timeoutTask = Task.Delay(_timeout, cancellationToken);
                var operationTask = operation(cancellationToken).AsTask();

                var completedTask = await Task.WhenAny(operationTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    System.Threading.Interlocked.Increment(ref _slowCalls);
                    OnFailure();
                    throw new TimeoutException($"Operation timed out after {_timeout.TotalMilliseconds}ms");
                }

                var result = await operationTask;
                stopwatch.Stop();

                // Check if call was slow
                if (stopwatch.Elapsed > _timeout * 0.8)
                {
                    System.Threading.Interlocked.Increment(ref _slowCalls);
                }

                OnSuccess();
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                OnFailure();
                throw;
            }
        }

        private void CheckForAutomaticTransition()
        {
            lock (_stateLock)
            {
                if (_state == CircuitBreakerState.Open)
                {
                    var timeSinceOpen = DateTime.UtcNow - _lastStateChange;
                    if (timeSinceOpen >= _breakDuration)
                    {
                        TransitionTo(CircuitBreakerState.HalfOpen);
                        _halfOpenCalls = 0;
                        _logger.LogInformation("Circuit breaker transitioned to HalfOpen after {Duration}s", timeSinceOpen.TotalSeconds);
                    }
                }
            }
        }

        private void OnSuccess()
        {
            System.Threading.Interlocked.Increment(ref _successfulCalls);

            lock (_stateLock)
            {
                _consecutiveFailures = 0;
                _consecutiveSuccesses++;

                if (_state == CircuitBreakerState.HalfOpen)
                {
                    if (_consecutiveSuccesses >= _successThreshold)
                    {
                        TransitionTo(CircuitBreakerState.Closed);
                        _logger.LogInformation("Circuit breaker transitioned to Closed after {Successes} consecutive successes", _consecutiveSuccesses);
                    }
                }
            }
        }

        private void OnFailure()
        {
            System.Threading.Interlocked.Increment(ref _failedCalls);

            lock (_stateLock)
            {
                _consecutiveSuccesses = 0;
                _consecutiveFailures++;

                if (_state == CircuitBreakerState.Closed || _state == CircuitBreakerState.HalfOpen)
                {
                    if (_consecutiveFailures >= _failureThreshold)
                    {
                        TransitionTo(CircuitBreakerState.Open);
                        _logger.LogWarning("Circuit breaker transitioned to Open after {Failures} consecutive failures", _consecutiveFailures);
                    }
                }
            }
        }

        private void TransitionTo(CircuitBreakerState newState)
        {
            _state = newState;
            _lastStateChange = DateTime.UtcNow;
            _consecutiveFailures = 0;
            _consecutiveSuccesses = 0;
        }

        public CircuitBreakerMetrics GetMetrics()
        {
            return new CircuitBreakerMetrics
            {
                TotalCalls = System.Threading.Interlocked.Read(ref _totalCalls),
                SuccessfulCalls = System.Threading.Interlocked.Read(ref _successfulCalls),
                FailedCalls = System.Threading.Interlocked.Read(ref _failedCalls),
                SlowCalls = System.Threading.Interlocked.Read(ref _slowCalls)
            };
        }
    }

    /// <summary>
    /// Context for custom optimization operations.
    /// </summary>
    public sealed class CustomOptimizationContext
    {
        public Type? RequestType { get; init; }
        public string OptimizationType { get; init; } = "General";
        public int OptimizationLevel { get; init; }
        public bool EnableProfiling { get; init; }
        public bool EnableTracing { get; init; }
        public Dictionary<string, object> CustomParameters { get; init; } = new();
        public OptimizationRecommendation? Recommendation { get; init; }
    }

    /// <summary>
    /// Statistics for custom optimization operations.
    /// </summary>
    public sealed class CustomOptimizationStatistics
    {
        public int OptimizationActionsApplied { get; set; }
        public int ActionsSucceeded { get; set; }
        public int ActionsFailed { get; set; }
        public List<OptimizationAction> Actions { get; set; } = new();
        public double OverallEffectiveness { get; set; }
        public double SuccessRate => OptimizationActionsApplied > 0 ? (double)ActionsSucceeded / OptimizationActionsApplied : 0.0;
    }

    /// <summary>
    /// Represents an optimization action.
    /// </summary>
    public sealed class OptimizationAction
    {
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public DateTime Timestamp { get; init; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Represents a custom optimization scope.
    /// </summary>
    public sealed class CustomOptimizationScope : IDisposable
    {
        private bool _disposed = false;
        private readonly CustomOptimizationContext _context;
        private readonly ILogger? _logger;
        private readonly CustomOptimizationStatistics _statistics;
        private readonly DateTime _startTime;
        private int _actionsApplied;
        private int _actionsSucceeded;
        private int _actionsFailed;
        private readonly System.Collections.Concurrent.ConcurrentBag<OptimizationAction> _actions;

        private CustomOptimizationScope(CustomOptimizationContext context, ILogger? logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger;
            _statistics = new CustomOptimizationStatistics();
            _startTime = DateTime.UtcNow;
            _actions = new System.Collections.Concurrent.ConcurrentBag<OptimizationAction>();

            _logger?.LogTrace("Custom optimization scope created: Type={Type}, Level={Level}",
                context.OptimizationType, context.OptimizationLevel);
        }

        public static CustomOptimizationScope Create(CustomOptimizationContext context, ILogger? logger)
        {
            return new CustomOptimizationScope(context, logger);
        }

        /// <summary>
        /// Records an optimization action.
        /// </summary>
        public void RecordAction(string name, string description, bool success = true, string? errorMessage = null)
        {
            var action = new OptimizationAction
            {
                Name = name,
                Description = description,
                Timestamp = DateTime.UtcNow,
                Success = success,
                ErrorMessage = errorMessage
            };

            _actions.Add(action);
            System.Threading.Interlocked.Increment(ref _actionsApplied);

            if (success)
                System.Threading.Interlocked.Increment(ref _actionsSucceeded);
            else
                System.Threading.Interlocked.Increment(ref _actionsFailed);

            _logger?.LogTrace("Optimization action recorded: {Name} - {Description} (Success: {Success})",
                name, description, success);
        }

        /// <summary>
        /// Records a timed optimization action.
        /// </summary>
        public async Task<T> RecordTimedActionAsync<T>(string name, string description, Func<Task<T>> action)
        {
            var actionRecord = new OptimizationAction
            {
                Name = name,
                Description = description,
                Timestamp = DateTime.UtcNow
            };

            var startTime = DateTime.UtcNow;
            System.Threading.Interlocked.Increment(ref _actionsApplied);

            try
            {
                var result = await action();
                actionRecord.Duration = DateTime.UtcNow - startTime;
                actionRecord.Success = true;

                System.Threading.Interlocked.Increment(ref _actionsSucceeded);

                _logger?.LogTrace("Timed action completed: {Name} - {Duration}ms",
                    name, actionRecord.Duration.TotalMilliseconds);

                _actions.Add(actionRecord);
                return result;
            }
            catch (Exception ex)
            {
                actionRecord.Duration = DateTime.UtcNow - startTime;
                actionRecord.Success = false;
                actionRecord.ErrorMessage = ex.Message;

                System.Threading.Interlocked.Increment(ref _actionsFailed);

                _logger?.LogWarning(ex, "Timed action failed: {Name} - {Duration}ms",
                    name, actionRecord.Duration.TotalMilliseconds);

                _actions.Add(actionRecord);
                throw;
            }
        }

        /// <summary>
        /// Gets custom optimization statistics.
        /// </summary>
        public CustomOptimizationStatistics GetStatistics()
        {
            _statistics.OptimizationActionsApplied = _actionsApplied;
            _statistics.ActionsSucceeded = _actionsSucceeded;
            _statistics.ActionsFailed = _actionsFailed;
            _statistics.Actions = _actions.ToList();

            // Calculate overall effectiveness based on success rate and action count
            if (_actionsApplied > 0)
            {
                var successRate = (double)_actionsSucceeded / _actionsApplied;
                var actionScore = Math.Min(1.0, _actionsApplied / 10.0); // More actions = better (up to 10)
                _statistics.OverallEffectiveness = (successRate * 0.7) + (actionScore * 0.3);
            }
            else
            {
                _statistics.OverallEffectiveness = 0.0;
            }

            return _statistics;
        }

        /// <summary>
        /// Gets profiling data if enabled.
        /// </summary>
        public Dictionary<string, object> GetProfilingData()
        {
            if (!_context.EnableProfiling)
                return new Dictionary<string, object>();

            var data = new Dictionary<string, object>
            {
                ["TotalDuration"] = (DateTime.UtcNow - _startTime).TotalMilliseconds,
                ["ActionsApplied"] = _actionsApplied,
                ["ActionsSucceeded"] = _actionsSucceeded,
                ["ActionsFailed"] = _actionsFailed,
                ["OptimizationType"] = _context.OptimizationType,
                ["OptimizationLevel"] = _context.OptimizationLevel
            };

            // Add action timings
            var actionTimings = _actions
                .OrderByDescending(a => a.Duration)
                .Take(10)
                .Select(a => new { a.Name, Duration = a.Duration.TotalMilliseconds, a.Success })
                .ToList();

            data["TopActions"] = actionTimings;

            return data;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                var duration = DateTime.UtcNow - _startTime;
                var stats = GetStatistics();

                _logger?.LogDebug("Custom optimization scope disposed: Duration={Duration}ms, Type={Type}, Actions={Actions}, Succeeded={Succeeded}, Failed={Failed}, Effectiveness={Effectiveness:P}",
                    duration.TotalMilliseconds, _context.OptimizationType, stats.OptimizationActionsApplied,
                    stats.ActionsSucceeded, stats.ActionsFailed, stats.OverallEffectiveness);

                // Log profiling data if enabled
                if (_context.EnableProfiling)
                {
                    var profilingData = GetProfilingData();
                    _logger?.LogInformation("Custom optimization profiling: {@ProfilingData}", profilingData);
                }

                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Context for SIMD optimization operations.
    /// </summary>
    public sealed class SIMDOptimizationContext
    {
        public bool EnableVectorization { get; init; }
        public int VectorSize { get; init; }
        public bool EnableUnrolling { get; init; }
        public int UnrollFactor { get; init; }
        public int MinDataSize { get; init; }
        public bool IsHardwareAccelerated { get; init; }
        public string[] SupportedVectorTypes { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Statistics for SIMD optimization operations.
    /// </summary>
    public sealed class SIMDOptimizationStatistics
    {
        public int VectorOperations { get; set; }
        public int ScalarOperations { get; set; }
        public long DataProcessed { get; set; }
        public long VectorizedData { get; set; }
        public double VectorizationRatio => VectorOperations + ScalarOperations > 0 ? (double)VectorOperations / (VectorOperations + ScalarOperations) : 0.0;
        public double VectorizedDataPercentage => DataProcessed > 0 ? (double)VectorizedData / DataProcessed : 0.0;
        public double EstimatedSpeedup { get; set; }
    }

    /// <summary>
    /// Represents a SIMD optimization scope.
    /// </summary>
    public sealed class SIMDOptimizationScope : IDisposable
    {
        private bool _disposed = false;
        private readonly SIMDOptimizationContext _context;
        private readonly ILogger? _logger;
        private readonly SIMDOptimizationStatistics _statistics;
        private readonly DateTime _startTime;
        private int _vectorOperations;
        private int _scalarOperations;
        private long _dataProcessed;
        private long _vectorizedData;

        private SIMDOptimizationScope(SIMDOptimizationContext context, ILogger? logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger;
            _statistics = new SIMDOptimizationStatistics();
            _startTime = DateTime.UtcNow;

            _logger?.LogTrace("SIMD scope created: HardwareAccelerated={HW}, VectorSize={Size}, Supported={Types}",
                context.IsHardwareAccelerated, context.VectorSize, string.Join(",", context.SupportedVectorTypes));
        }

        public static SIMDOptimizationScope Create(SIMDOptimizationContext context, ILogger? logger)
        {
            return new SIMDOptimizationScope(context, logger);
        }

        /// <summary>
        /// Records a vector operation.
        /// </summary>
        public void RecordVectorOperation(int elementsProcessed)
        {
            System.Threading.Interlocked.Increment(ref _vectorOperations);
            System.Threading.Interlocked.Add(ref _vectorizedData, elementsProcessed);
            System.Threading.Interlocked.Add(ref _dataProcessed, elementsProcessed);

            _logger?.LogTrace("Vector operation recorded: Elements={Elements}", elementsProcessed);
        }

        /// <summary>
        /// Records a scalar operation.
        /// </summary>
        public void RecordScalarOperation(int elementsProcessed)
        {
            System.Threading.Interlocked.Increment(ref _scalarOperations);
            System.Threading.Interlocked.Add(ref _dataProcessed, elementsProcessed);

            _logger?.LogTrace("Scalar operation recorded: Elements={Elements}", elementsProcessed);
        }

        /// <summary>
        /// Processes data using SIMD when possible.
        /// </summary>
        public void ProcessData<T>(ReadOnlySpan<T> data, Action<System.Numerics.Vector<T>> vectorAction, Action<T> scalarAction)
            where T : struct
        {
            if (!_context.EnableVectorization || data.Length < _context.MinDataSize)
            {
                // Process as scalar
                for (int i = 0; i < data.Length; i++)
                {
                    scalarAction(data[i]);
                    RecordScalarOperation(1);
                }
                return;
            }

            var vectorSize = System.Numerics.Vector<T>.Count;
            var vectorCount = data.Length / vectorSize;
            var remainder = data.Length % vectorSize;

            // Process vectors
            for (int i = 0; i < vectorCount; i++)
            {
                var vector = new System.Numerics.Vector<T>(data.Slice(i * vectorSize, vectorSize));
                vectorAction(vector);
                RecordVectorOperation(vectorSize);
            }

            // Process remainder as scalar
            for (int i = vectorCount * vectorSize; i < data.Length; i++)
            {
                scalarAction(data[i]);
                RecordScalarOperation(1);
            }
        }

        /// <summary>
        /// Gets SIMD optimization statistics.
        /// </summary>
        public SIMDOptimizationStatistics GetStatistics()
        {
            _statistics.VectorOperations = _vectorOperations;
            _statistics.ScalarOperations = _scalarOperations;
            _statistics.DataProcessed = _dataProcessed;
            _statistics.VectorizedData = _vectorizedData;

            // Estimate speedup based on vectorization ratio and vector size
            // Theoretical speedup = (vector_ops * vector_size + scalar_ops) / (vector_ops + scalar_ops)
            if (_vectorOperations + _scalarOperations > 0)
            {
                var totalOps = _vectorOperations + _scalarOperations;
                var vectorizedOps = _vectorOperations * _context.VectorSize;
                var totalElements = vectorizedOps + _scalarOperations;

                // Actual speedup compared to pure scalar
                _statistics.EstimatedSpeedup = totalOps > 0 ? (double)totalElements / totalOps : 1.0;
            }
            else
            {
                _statistics.EstimatedSpeedup = 1.0;
            }

            return _statistics;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                var duration = DateTime.UtcNow - _startTime;
                var stats = GetStatistics();

                _logger?.LogDebug("SIMD optimization scope disposed: Duration={Duration}ms, VectorOps={VectorOps}, ScalarOps={ScalarOps}, VectorizationRatio={Ratio:P}, Speedup={Speedup:F2}x",
                    duration.TotalMilliseconds, stats.VectorOperations, stats.ScalarOperations,
                    stats.VectorizationRatio, stats.EstimatedSpeedup);

                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Context for database optimization operations.
    /// </summary>
    public sealed class DatabaseOptimizationContext
    {
        public bool EnableQueryOptimization { get; init; }
        public bool EnableConnectionPooling { get; init; }
        public bool EnableReadOnlyHint { get; init; }
        public bool EnableBatchingHint { get; init; }
        public bool EnableNoTracking { get; init; }
        public int MaxRetries { get; init; }
        public int RetryDelayMs { get; init; }
        public int QueryTimeoutSeconds { get; init; }
        public Type? RequestType { get; init; }
    }

    /// <summary>
    /// Statistics for database optimization operations.
    /// </summary>
    public sealed class DatabaseOptimizationStatistics
    {
        public int QueriesExecuted { get; set; }
        public int ConnectionsOpened { get; set; }
        public int ConnectionsReused { get; set; }
        public TimeSpan TotalQueryTime { get; set; }
        public TimeSpan SlowestQueryTime { get; set; }
        public int RetryCount { get; set; }
        public TimeSpan AverageQueryTime => QueriesExecuted > 0 ? TimeSpan.FromTicks(TotalQueryTime.Ticks / QueriesExecuted) : TimeSpan.Zero;
        public double ConnectionPoolEfficiency => ConnectionsOpened + ConnectionsReused > 0 ? (double)ConnectionsReused / (ConnectionsOpened + ConnectionsReused) : 0.0;
        public double QueryEfficiency { get; set; }
    }

    /// <summary>
    /// Represents a database optimization scope.
    /// </summary>
    public sealed class DatabaseOptimizationScope : IDisposable
    {
        private bool _disposed = false;
        private readonly DatabaseOptimizationContext _context;
        private readonly ILogger? _logger;
        private readonly DatabaseOptimizationStatistics _statistics;
        private readonly DateTime _startTime;
        private int _queriesExecuted;
        private int _connectionsOpened;
        private int _connectionsReused;
        private int _retryCount;
        private long _totalQueryTicks;
        private long _slowestQueryTicks;

        private DatabaseOptimizationScope(DatabaseOptimizationContext context, ILogger? logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger;
            _statistics = new DatabaseOptimizationStatistics();
            _startTime = DateTime.UtcNow;
        }

        public static DatabaseOptimizationScope Create(DatabaseOptimizationContext context, ILogger? logger)
        {
            return new DatabaseOptimizationScope(context, logger);
        }

        /// <summary>
        /// Records a query execution.
        /// </summary>
        public void RecordQuery(TimeSpan duration, bool fromPool = false)
        {
            System.Threading.Interlocked.Increment(ref _queriesExecuted);
            System.Threading.Interlocked.Add(ref _totalQueryTicks, duration.Ticks);

            // Update slowest query
            long currentSlowest;
            do
            {
                currentSlowest = System.Threading.Interlocked.Read(ref _slowestQueryTicks);
                if (duration.Ticks <= currentSlowest)
                    break;
            }
            while (System.Threading.Interlocked.CompareExchange(ref _slowestQueryTicks, duration.Ticks, currentSlowest) != currentSlowest);

            if (fromPool)
                System.Threading.Interlocked.Increment(ref _connectionsReused);

            _logger?.LogTrace("Query executed in {Duration}ms (FromPool: {FromPool})", duration.TotalMilliseconds, fromPool);
        }

        /// <summary>
        /// Records a database connection opened.
        /// </summary>
        public void RecordConnectionOpened()
        {
            System.Threading.Interlocked.Increment(ref _connectionsOpened);
            _logger?.LogTrace("Database connection opened");
        }

        /// <summary>
        /// Records a retry attempt.
        /// </summary>
        public void RecordRetry()
        {
            System.Threading.Interlocked.Increment(ref _retryCount);
            _logger?.LogDebug("Database operation retry recorded (Count: {RetryCount})", _retryCount);
        }

        /// <summary>
        /// Gets database optimization statistics.
        /// </summary>
        public DatabaseOptimizationStatistics GetStatistics()
        {
            var duration = DateTime.UtcNow - _startTime;

            _statistics.QueriesExecuted = _queriesExecuted;
            _statistics.ConnectionsOpened = _connectionsOpened;
            _statistics.ConnectionsReused = _connectionsReused;
            _statistics.TotalQueryTime = TimeSpan.FromTicks(_totalQueryTicks);
            _statistics.SlowestQueryTime = TimeSpan.FromTicks(_slowestQueryTicks);
            _statistics.RetryCount = _retryCount;

            // Calculate query efficiency
            // Efficiency = (ideal time) / (actual time)
            // Ideal = fastest theoretical execution
            if (_queriesExecuted > 0 && duration.TotalMilliseconds > 0)
            {
                var idealTime = _statistics.AverageQueryTime.TotalMilliseconds * _queriesExecuted;
                var actualTime = duration.TotalMilliseconds;
                _statistics.QueryEfficiency = Math.Min(1.0, idealTime / actualTime);
            }

            return _statistics;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                var duration = DateTime.UtcNow - _startTime;
                var stats = GetStatistics();

                _logger?.LogDebug("Database optimization scope disposed: Duration={Duration}ms, Queries={Queries}, Connections={Connections}/{Reused}, AvgQueryTime={AvgTime}ms, Retries={Retries}, PoolEfficiency={PoolEff:P}",
                    duration.TotalMilliseconds, stats.QueriesExecuted, stats.ConnectionsOpened, stats.ConnectionsReused,
                    stats.AverageQueryTime.TotalMilliseconds, stats.RetryCount, stats.ConnectionPoolEfficiency);

                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Context for parallel processing operations.
    /// </summary>
    public sealed class ParallelProcessingContext
    {
        public int MaxDegreeOfParallelism { get; init; }
        public bool EnableWorkStealing { get; init; }
        public int MinItemsForParallel { get; init; }
        public double CpuUtilization { get; init; }
        public int AvailableProcessors { get; init; }
    }

    /// <summary>
    /// Statistics for parallel processing operations.
    /// </summary>
    public sealed class ParallelProcessingStatistics
    {
        public int TasksExecuted { get; set; }
        public int TasksCompleted { get; set; }
        public int TasksFailed { get; set; }
        public TimeSpan TotalTaskDuration { get; set; }
        public TimeSpan AverageTaskDuration => TasksExecuted > 0 ? TimeSpan.FromTicks(TotalTaskDuration.Ticks / TasksExecuted) : TimeSpan.Zero;
        public double Efficiency { get; set; }
        public double ActualParallelism { get; set; }
        public double Speedup { get; set; }
    }

    /// <summary>
    /// Represents a parallel processing scope.
    /// </summary>
    public sealed class ParallelProcessingScope : IDisposable
    {
        private bool _disposed = false;
        private readonly ParallelProcessingContext _context;
        private readonly ILogger? _logger;
        private readonly ParallelProcessingStatistics _statistics;
        private readonly DateTime _startTime;
        private int _tasksExecuted;
        private int _tasksCompleted;
        private int _tasksFailed;
        private long _totalTaskTicks;

        private ParallelProcessingScope(ParallelProcessingContext context, ILogger? logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger;
            _statistics = new ParallelProcessingStatistics();
            _startTime = DateTime.UtcNow;
        }

        public static ParallelProcessingScope Create(ParallelProcessingContext context, ILogger? logger)
        {
            return new ParallelProcessingScope(context, logger);
        }

        /// <summary>
        /// Gets parallel options configured for this scope.
        /// </summary>
        public ParallelOptions GetParallelOptions()
        {
            return new ParallelOptions
            {
                MaxDegreeOfParallelism = _context.MaxDegreeOfParallelism,
                CancellationToken = CancellationToken.None
            };
        }

        /// <summary>
        /// Records task execution metrics.
        /// </summary>
        public void RecordTaskExecution(TimeSpan duration, bool success)
        {
            System.Threading.Interlocked.Increment(ref _tasksExecuted);

            if (success)
                System.Threading.Interlocked.Increment(ref _tasksCompleted);
            else
                System.Threading.Interlocked.Increment(ref _tasksFailed);

            System.Threading.Interlocked.Add(ref _totalTaskTicks, duration.Ticks);
        }

        /// <summary>
        /// Gets parallel processing statistics.
        /// </summary>
        public ParallelProcessingStatistics GetStatistics()
        {
            var duration = DateTime.UtcNow - _startTime;

            _statistics.TasksExecuted = _tasksExecuted;
            _statistics.TasksCompleted = _tasksCompleted;
            _statistics.TasksFailed = _tasksFailed;
            _statistics.TotalTaskDuration = TimeSpan.FromTicks(_totalTaskTicks);

            // Calculate actual parallelism achieved
            if (duration.TotalSeconds > 0)
            {
                _statistics.ActualParallelism = _statistics.TotalTaskDuration.TotalSeconds / duration.TotalSeconds;
            }

            // Calculate efficiency (how well we utilized available parallelism)
            if (_context.MaxDegreeOfParallelism > 0)
            {
                _statistics.Efficiency = Math.Min(1.0, _statistics.ActualParallelism / _context.MaxDegreeOfParallelism);
            }

            // Calculate speedup (assuming sequential baseline)
            if (duration.TotalSeconds > 0 && _statistics.TotalTaskDuration.TotalSeconds > 0)
            {
                var sequentialEstimate = _statistics.AverageTaskDuration.TotalSeconds * _tasksExecuted;
                _statistics.Speedup = sequentialEstimate / duration.TotalSeconds;
            }

            return _statistics;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                var duration = DateTime.UtcNow - _startTime;
                var stats = GetStatistics();

                _logger?.LogDebug("Parallel processing scope disposed: Duration={Duration}ms, Tasks={Tasks}, Completed={Completed}, Failed={Failed}, Parallelism={Parallelism:F2}, Efficiency={Efficiency:P}, Speedup={Speedup:F2}x",
                    duration.TotalMilliseconds, stats.TasksExecuted, stats.TasksCompleted, stats.TasksFailed,
                    stats.ActualParallelism, stats.Efficiency, stats.Speedup);

                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Context for memory pooling operations.
    /// </summary>
    public sealed class MemoryPoolingContext
    {
        public bool EnableObjectPooling { get; init; }
        public bool EnableBufferPooling { get; init; }
        public int EstimatedBufferSize { get; init; } = 4096;
    }

    /// <summary>
    /// Statistics for memory pooling operations.
    /// </summary>
    public sealed class MemoryPoolStatistics
    {
        public int PoolHits { get; set; }
        public int PoolMisses { get; set; }
        public int BuffersRented { get; set; }
        public int BuffersReturned { get; set; }
        public long EstimatedSavings { get; set; }
        public double Efficiency => PoolHits + PoolMisses > 0 ? (double)PoolHits / (PoolHits + PoolMisses) : 0.0;
    }

    /// <summary>
    /// Represents a memory pool scope for optimized memory usage.
    /// </summary>
    public sealed class MemoryPoolScope : IDisposable
    {
        private bool _disposed = false;
        private readonly MemoryPoolingContext _context;
        private readonly ILogger? _logger;
        private readonly MemoryPoolStatistics _statistics;
        private readonly List<byte[]> _rentedBuffers;
        private readonly DateTime _startTime;

        private MemoryPoolScope(MemoryPoolingContext context, ILogger? logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger;
            _statistics = new MemoryPoolStatistics();
            _rentedBuffers = new List<byte[]>();
            _startTime = DateTime.UtcNow;
        }

        public static MemoryPoolScope Create()
        {
            return new MemoryPoolScope(
                new MemoryPoolingContext
                {
                    EnableObjectPooling = false,
                    EnableBufferPooling = false
                },
                null);
        }

        public static MemoryPoolScope Create(MemoryPoolingContext context, ILogger? logger)
        {
            return new MemoryPoolScope(context, logger);
        }

        /// <summary>
        /// Rents a buffer from the pool.
        /// </summary>
        public byte[] RentBuffer(int minimumSize)
        {
            if (!_context.EnableBufferPooling)
            {
                _statistics.PoolMisses++;
                return new byte[minimumSize];
            }

            try
            {
                // Use ArrayPool for buffer management
                var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(minimumSize);
                _rentedBuffers.Add(buffer);
                _statistics.BuffersRented++;
                _statistics.PoolHits++;

                // Estimate savings (vs heap allocation)
                _statistics.EstimatedSavings += buffer.Length;

                _logger?.LogTrace("Rented buffer of size {Size} from pool", buffer.Length);

                return buffer;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to rent buffer from pool, falling back to heap allocation");
                _statistics.PoolMisses++;
                return new byte[minimumSize];
            }
        }

        /// <summary>
        /// Returns a buffer to the pool.
        /// </summary>
        public void ReturnBuffer(byte[] buffer, bool clearArray = true)
        {
            if (buffer == null || !_context.EnableBufferPooling)
                return;

            try
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer, clearArray);
                _rentedBuffers.Remove(buffer);
                _statistics.BuffersReturned++;

                _logger?.LogTrace("Returned buffer of size {Size} to pool", buffer.Length);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to return buffer to pool");
            }
        }

        /// <summary>
        /// Gets pooling statistics.
        /// </summary>
        public MemoryPoolStatistics GetStatistics() => _statistics;

        public void Dispose()
        {
            if (!_disposed)
            {
                // Return all rented buffers to pool
                foreach (var buffer in _rentedBuffers.ToList())
                {
                    ReturnBuffer(buffer);
                }

                _rentedBuffers.Clear();

                var duration = DateTime.UtcNow - _startTime;
                _logger?.LogDebug("Memory pool scope disposed: Duration={Duration}ms, PoolHits={Hits}, PoolMisses={Misses}, Efficiency={Efficiency:P}",
                    duration.TotalMilliseconds, _statistics.PoolHits, _statistics.PoolMisses, _statistics.Efficiency);

                _disposed = true;
            }
        }
    }
}