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
/// Strategy for applying SIMD (Single Instruction, Multiple Data) optimizations.
/// </summary>
public class SIMDOptimizationStrategy<TRequest, TResponse> : BaseOptimizationStrategy<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public SIMDOptimizationStrategy(
        ILogger logger,
        IMetricsProvider? metricsProvider = null)
        : base(logger, metricsProvider)
    {
    }

    public override OptimizationStrategy StrategyType => OptimizationStrategy.SIMDAcceleration;

    public override ValueTask<bool> CanApplyAsync(
        TRequest request,
        OptimizationRecommendation recommendation,
        SystemLoadMetrics systemLoad,
        CancellationToken cancellationToken)
    {
        // Check SIMD support
        if (!System.Numerics.Vector.IsHardwareAccelerated)
        {
            Logger.LogWarning("SIMD optimization requested but hardware acceleration not available for {RequestType}", typeof(TRequest).Name);
            return new ValueTask<bool>(false);
        }

        // SIMD is beneficial for CPU-intensive operations with large data sets
        return new ValueTask<bool>(MeetsConfidenceThreshold(recommendation, 0.6) &&
                                   systemLoad.CpuUtilization < 0.9); // Don't apply under high CPU load
    }

    public override ValueTask<RequestHandlerDelegate<TResponse>> ApplyAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        OptimizationRecommendation recommendation,
        SystemLoadMetrics systemLoad,
        CancellationToken cancellationToken)
    {
        // Extract SIMD optimization parameters from AI recommendation
        var enableVectorization = GetParameter(recommendation, "EnableVectorization", true);
        var vectorSize = GetParameter<int>(recommendation, "VectorSize", System.Numerics.Vector<float>.Count);
        var enableUnrolling = GetParameter(recommendation, "EnableUnrolling", true);
        var unrollFactor = GetParameter<int>(recommendation, "UnrollFactor", 4);
        var minDataSize = GetParameter<int>(recommendation, "MinDataSize", 64);

        Logger.LogDebug("Applying SIMD optimization for {RequestType}: Vectorization={Vectorization}, VectorSize={VectorSize}, Unrolling={Unrolling}",
            typeof(TRequest).Name, enableVectorization, vectorSize, enableUnrolling);

        // Wrap handler with SIMD optimization logic
        return new ValueTask<RequestHandlerDelegate<TResponse>>(async () =>
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

            using var scope = SIMDOptimizationScope.Create(simdContext, Logger);

            try
            {
                var startTime = DateTime.UtcNow;

                // Execute handler with SIMD context available
                var response = await next();

                var duration = DateTime.UtcNow - startTime;
                var stats = scope.GetStatistics();

                // Record SIMD optimization metrics
                RecordSIMDOptimizationMetrics(typeof(TRequest), duration, stats, simdContext);

                Logger.LogDebug("SIMD optimization for {RequestType}: Duration={Duration}ms, VectorOps={VectorOps}, ScalarOps={ScalarOps}, Speedup={Speedup:F2}x",
                    typeof(TRequest).Name, duration.TotalMilliseconds, stats.VectorOperations, stats.ScalarOperations, stats.EstimatedSpeedup);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "SIMD optimization execution failed for {RequestType}", typeof(TRequest).Name);
                throw;
            }
        });
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
        var properties = new Dictionary<string, object>
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
        };

        RecordMetrics(requestType, duration, true, properties);
    }
}