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
    /// Pipeline behavior that integrates AI optimization engine into the request processing pipeline.
    /// Monitors request execution, applies AI-recommended optimizations, and learns from results.
    /// </summary>
    public sealed partial class AIOptimizationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
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
                
                // Analyze request with AI engine (TRequest already constrained to IRequest<TResponse>)
                var recommendation = await _aiEngine.AnalyzeRequestAsync(request, executionMetrics, cancellationToken);

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
                    CpuUsage = (await _systemMetrics.GetCurrentLoadAsync(cancellationToken)).CpuUtilization,
                    MemoryUsage = endMemory
                };

                // Learn from failed execution
                if (_options.LearningEnabled)
                {
                    await _aiEngine.LearnFromExecutionAsync(requestType, appliedOptimizations.ToArray(), failedMetrics, cancellationToken);
                }

                _logger.LogWarning(ex, "AI-optimized execution of {RequestType} failed after {Duration}ms with {OptimizationCount} optimizations",
                    requestType.Name, stopwatch.ElapsedMilliseconds, appliedOptimizations.Count);

                throw;
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

}