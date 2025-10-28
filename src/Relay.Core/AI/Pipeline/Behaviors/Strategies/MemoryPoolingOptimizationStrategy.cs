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
/// Strategy for applying memory pooling optimizations.
/// </summary>
public class MemoryPoolingOptimizationStrategy<TRequest, TResponse> : BaseOptimizationStrategy<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public MemoryPoolingOptimizationStrategy(
        ILogger logger,
        IMetricsProvider? metricsProvider = null)
        : base(logger, metricsProvider)
    {
    }

    public override OptimizationStrategy StrategyType => OptimizationStrategy.MemoryPooling;

    public override ValueTask<bool> CanApplyAsync(
        TRequest request,
        OptimizationRecommendation recommendation,
        SystemLoadMetrics systemLoad,
        CancellationToken cancellationToken)
    {
        // Memory pooling is generally beneficial unless system is critically low on memory
        return new ValueTask<bool>(systemLoad.MemoryUtilization < 0.95 &&
                                   MeetsConfidenceThreshold(recommendation, 0.3)); // Lower threshold for memory optimizations
    }

    public override ValueTask<RequestHandlerDelegate<TResponse>> ApplyAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        OptimizationRecommendation recommendation,
        SystemLoadMetrics systemLoad,
        CancellationToken cancellationToken)
    {
        // Extract memory pooling parameters from AI recommendation
        var enableObjectPooling = GetParameter(recommendation, "EnableObjectPooling", true);
        var enableBufferPooling = GetParameter(recommendation, "EnableBufferPooling", true);
        var estimatedBufferSize = GetParameter(recommendation, "EstimatedBufferSize", 4096);
        var poolSize = GetParameter(recommendation, "PoolSize", 100);

        Logger.LogDebug("Applying memory pooling optimization for {RequestType}: ObjectPool={ObjectPool}, BufferPool={BufferPool}, BufferSize={BufferSize}",
            typeof(TRequest).Name, enableObjectPooling, enableBufferPooling, estimatedBufferSize);

        // Wrap next with memory pooling logic
        return new ValueTask<RequestHandlerDelegate<TResponse>>(async () =>
        {
            var startMemory = GC.GetTotalAllocatedBytes(precise: false);
            var poolingContext = new MemoryPoolingContext
            {
                EnableObjectPooling = enableObjectPooling,
                EnableBufferPooling = enableBufferPooling,
                EstimatedBufferSize = estimatedBufferSize
            };

            using var scope = MemoryPoolScope.Create(poolingContext, Logger);

            try
            {
                // Execute handler with pooling context
                var response = await next();

                // Measure memory savings
                var endMemory = GC.GetTotalAllocatedBytes(precise: false);
                var allocatedBytes = endMemory - startMemory;

                // Record pooling effectiveness
                RecordMemoryPoolingMetrics(typeof(TRequest), allocatedBytes, scope.GetStatistics());

                Logger.LogDebug("Memory pooling for {RequestType}: Allocated={Allocated}KB, PoolHits={PoolHits}, PoolMisses={PoolMisses}",
                    typeof(TRequest).Name, allocatedBytes / 1024, scope.GetStatistics().PoolHits, scope.GetStatistics().PoolMisses);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Memory pooling execution failed for {RequestType}, continuing without pooling benefits", typeof(TRequest).Name);
                throw;
            }
        });
    }

    private void RecordMemoryPoolingMetrics(Type requestType, long allocatedBytes, MemoryPoolStatistics stats)
    {
        var properties = new Dictionary<string, object>
        {
            ["AllocatedBytes"] = allocatedBytes,
            ["PoolHits"] = stats.PoolHits,
            ["PoolMisses"] = stats.PoolMisses,
            ["BuffersRented"] = stats.BuffersRented,
            ["BuffersReturned"] = stats.BuffersReturned,
            ["MemorySavings"] = stats.EstimatedSavings,
            ["PoolEfficiency"] = stats.Efficiency
        };

        RecordMetrics(requestType, TimeSpan.Zero, true, properties);
    }
}