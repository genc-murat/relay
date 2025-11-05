using Microsoft.Extensions.Logging;
using Relay.Core.AI.Optimization.Contexts;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Pipeline.Behaviors.Strategies;

/// <summary>
/// Strategy for applying parallel processing optimizations.
/// </summary>
public class ParallelProcessingOptimizationStrategy<TRequest, TResponse> : BaseOptimizationStrategy<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public ParallelProcessingOptimizationStrategy(
        ILogger logger,
        IMetricsProvider? metricsProvider = null)
        : base(logger, metricsProvider)
    {
    }

    public override OptimizationStrategy StrategyType => OptimizationStrategy.ParallelProcessing;

    public override ValueTask<bool> CanApplyAsync(
        TRequest request,
        OptimizationRecommendation recommendation,
        SystemLoadMetrics systemLoad,
        CancellationToken cancellationToken)
    {
        // Extract parallel processing parameters from AI recommendation
        var maxDegreeOfParallelism = GetParameter(recommendation, "MaxDegreeOfParallelism", -1);

        // Adjust parallelism based on current system load
        var optimalParallelism = CalculateOptimalParallelism(maxDegreeOfParallelism, systemLoad);

        // Don't apply if system is under high load or parallelism would be minimal
        return new ValueTask<bool>(optimalParallelism > 1 && systemLoad.CpuUtilization < 0.90 &&
                                   MeetsConfidenceThreshold(recommendation, 0.5));
    }

    public override ValueTask<RequestHandlerDelegate<TResponse>> ApplyAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        OptimizationRecommendation recommendation,
        SystemLoadMetrics systemLoad,
        CancellationToken cancellationToken)
    {
        // Extract parallel processing parameters from AI recommendation
        var maxDegreeOfParallelism = GetParameter(recommendation, "MaxDegreeOfParallelism", -1);
        var enableWorkStealing = GetParameter(recommendation, "EnableWorkStealing", true);
        var taskScheduler = GetParameter(recommendation, "TaskScheduler", "Default");
        var minItemsForParallel = GetParameter(recommendation, "MinItemsForParallel", 10);

        // Adjust parallelism based on current system load
        var optimalParallelism = CalculateOptimalParallelism(maxDegreeOfParallelism, systemLoad);

        Logger.LogDebug("Applying parallel processing optimization for {RequestType}: MaxParallelism={Parallelism}, WorkStealing={WorkStealing}, MinItems={MinItems}",
            typeof(TRequest).Name, optimalParallelism, enableWorkStealing, minItemsForParallel);

        // Wrap handler with parallel processing configuration
        return new ValueTask<RequestHandlerDelegate<TResponse>>(async () =>
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
            using var scope = ParallelProcessingScope.Create(parallelContext, Logger);

            try
            {
                var startTime = DateTime.UtcNow;

                // Execute handler (handler can access parallelism context if needed)
                var response = await next();

                var duration = DateTime.UtcNow - startTime;
                var stats = scope.GetStatistics();

                // Record parallel processing metrics
                RecordParallelProcessingMetrics(typeof(TRequest), duration, stats, parallelContext);

                Logger.LogDebug("Parallel processing for {RequestType}: Duration={Duration}ms, TasksExecuted={Tasks}, Efficiency={Efficiency:P}",
                    typeof(TRequest).Name, duration.TotalMilliseconds, stats.TasksExecuted, stats.Efficiency);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Parallel processing execution failed for {RequestType}", typeof(TRequest).Name);
                throw;
            }
        });
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
        var properties = new Dictionary<string, object>
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
        };

        RecordMetrics(requestType, duration, true, properties);
    }
}