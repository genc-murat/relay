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
using Relay.Core.AI.Pipeline.Options;
using Relay.Core.AI.Pipeline.Interfaces;
using Relay.Core.AI.Pipeline.Metrics;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;

namespace Relay.Core.AI.Pipeline.Behaviors
{
    /// <summary>
    /// Pipeline behavior that integrates AI optimization engine into the request processing pipeline.
    /// Monitors request execution, applies AI-recommended optimizations, and learns from results.
    /// </summary>
    public sealed partial class AIOptimizationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IAIOptimizationEngine _aiEngine;
        private readonly ILogger<AIOptimizationPipelineBehavior<TRequest, TResponse>> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly ISystemLoadMetricsProvider _systemMetrics;
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
            ISystemLoadMetricsProvider systemMetrics,
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

            // Check cancellation before starting - this ensures fast fail for already-cancelled tokens
            cancellationToken.ThrowIfCancellationRequested();

            var requestType = typeof(TRequest);
            var stopwatch = Stopwatch.StartNew();
            var startMemory = GC.GetTotalMemory(false);
            var appliedOptimizations = new List<OptimizationStrategy>();

            // Variables to hold optimization state
            SystemLoadMetrics systemLoad;
            RequestHandlerDelegate<TResponse> optimizedNext;
            TResponse response;

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
                try
                {
                    systemLoad = await _systemMetrics.GetCurrentLoadAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to collect system metrics, using default values");
                    systemLoad = new SystemLoadMetrics
                    {
                        CpuUtilization = 0.5,
                        MemoryUtilization = 0.5,
                        ActiveConnections = 10,
                        QueuedRequestCount = 0,
                        AvailableMemory = 1024L * 1024 * 1024, // 1GB
                        ActiveRequestCount = 1,
                        ThroughputPerSecond = 10.0,
                        AverageResponseTime = TimeSpan.FromMilliseconds(100),
                        ErrorRate = 0.0,
                        Timestamp = DateTime.UtcNow,
                        DatabasePoolUtilization = 0.0,
                        ThreadPoolUtilization = 0.0
                    };
                }

                // Get AI recommendations
                var executionMetrics = await GetHistoricalMetrics(requestType, cancellationToken);

                // Analyze request with AI engine (TRequest already constrained to IRequest<TResponse>)
                var recommendation = await _aiEngine.AnalyzeRequestAsync(request, executionMetrics, cancellationToken);

                _logger.LogDebug("AI recommendation for {RequestType}: {Strategy} (Confidence: {Confidence:P})",
                    requestType.Name, recommendation.Strategy, recommendation.ConfidenceScore);

                // Apply optimizations based on AI recommendations
                optimizedNext = await ApplyOptimizations(request, next, recommendation, systemLoad, appliedOptimizations, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Cancellation occurred during AI optimization phase - fall back to standard execution
                _logger.LogDebug("AI optimization cancelled for {RequestType}, falling back to standard execution", requestType.Name);

                // Execute the handler anyway
                response = await next();

                // Record metrics even after cancellation
                stopwatch.Stop();
                var endMemory = GC.GetTotalMemory(false);

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
                    CpuUsage = 0.5,
                    MemoryUsage = endMemory,
                    DatabaseCalls = 0,
                    ExternalApiCalls = 0
                };

                // Learn from the execution even after cancellation (use CancellationToken.None)
                if (_options.LearningEnabled)
                {
                    try
                    {
                        await _aiEngine.LearnFromExecutionAsync(requestType, appliedOptimizations.ToArray(), actualMetrics, CancellationToken.None);
                    }
                    catch (Exception learningEx)
                    {
                        _logger.LogWarning(learningEx, "Failed to learn from execution after cancellation for {RequestType}", requestType.Name);
                    }
                }

                return response;
            }

            // Execute the request with optimizations (separate try-catch for handler execution)
            try
            {
                response = await optimizedNext();
                
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
                if (_options.LearningEnabled)
                {
                    await _aiEngine.LearnFromExecutionAsync(requestType, appliedOptimizations.ToArray(), actualMetrics, cancellationToken);
                }

                _logger.LogDebug("AI-optimized execution of {RequestType} completed in {Duration}ms with {OptimizationCount} optimizations",
                    requestType.Name, stopwatch.ElapsedMilliseconds, appliedOptimizations.Count);

                return response;
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
                    CpuUsage = 0.5, // Default CPU usage when metrics collection fails
                    MemoryUsage = endMemory
                };

                // Learn from failed execution (use CancellationToken.None to avoid cancellation during learning)
                if (_options.LearningEnabled)
                {
                    try
                    {
                        await _aiEngine.LearnFromExecutionAsync(requestType, appliedOptimizations.ToArray(), failedMetrics, CancellationToken.None);
                    }
                    catch (Exception learningEx)
                    {
                        _logger.LogWarning(learningEx, "Failed to learn from execution for {RequestType}", requestType.Name);
                    }
                }

                _logger.LogWarning(ex, "AI-optimized execution of {RequestType} failed after {Duration}ms with {OptimizationCount} optimizations",
                    requestType.Name, stopwatch.ElapsedMilliseconds, appliedOptimizations.Count);

                throw;
            }
        }

    }

}