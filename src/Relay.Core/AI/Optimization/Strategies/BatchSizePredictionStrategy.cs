using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization.Strategies
{
    /// <summary>
    /// Strategy for predicting optimal batch sizes based on system load and request characteristics.
    /// </summary>
    internal class BatchSizePredictionStrategy : IOptimizationStrategy
    {
        private readonly ILogger _logger;
        private readonly AIOptimizationOptions _options;

        public string Name => "BatchSizePrediction";
        public int Priority => 90;

        public BatchSizePredictionStrategy(ILogger logger, AIOptimizationOptions options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public bool CanHandle(string operation) => operation == "PredictBatchSize";

        public async ValueTask<StrategyExecutionResult> ExecuteAsync(OptimizationContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                if (context.RequestType == null || context.SystemLoad == null)
                {
                    return new StrategyExecutionResult
                    {
                        Success = false,
                        StrategyName = Name,
                        ErrorMessage = "Request type and system load metrics are required",
                        ExecutionTime = DateTime.UtcNow - startTime
                    };
                }

                var optimalBatchSize = CalculateOptimalBatchSize(context);

                _logger.LogDebug("Predicted optimal batch size for {RequestType}: {BatchSize}",
                    context.RequestType.Name, optimalBatchSize);

                return new StrategyExecutionResult
                {
                    Success = true,
                    StrategyName = Name,
                    Confidence = CalculateConfidence(context),
                    Data = optimalBatchSize,
                    ExecutionTime = DateTime.UtcNow - startTime,
                    Metadata = new()
                    {
                        ["request_type"] = context.RequestType.Name,
                        ["cpu_utilization"] = context.SystemLoad.CpuUtilization,
                        ["memory_utilization"] = context.SystemLoad.MemoryUtilization,
                        ["active_connections"] = context.SystemLoad.ActiveConnections
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch size prediction strategy");

                return new StrategyExecutionResult
                {
                    Success = false,
                    StrategyName = Name,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        private int CalculateOptimalBatchSize(OptimizationContext context)
        {
            var load = context.SystemLoad!;
            var baseBatchSize = _options.DefaultBatchSize;

            // CPU utilization factor (higher CPU = smaller batches)
            var cpuFactor = 1.0 - (load.CpuUtilization * 0.7); // Max 70% reduction

            // Memory utilization factor (higher memory = smaller batches)
            var memoryFactor = 1.0 - (load.MemoryUtilization * 0.6); // Max 60% reduction

            // Connection factor (more connections = larger batches for efficiency)
            var connectionFactor = Math.Min(load.ActiveConnections / 100.0, 2.0); // Max 2x increase

            // Queue factor (longer queues = larger batches to clear backlog)
            var queueFactor = Math.Min(load.QueuedRequestCount / 50.0 + 1.0, 3.0); // Max 3x increase

            // Error rate factor (higher errors = smaller batches for stability)
            var errorFactor = 1.0 - (load.ErrorRate * 0.5); // Max 50% reduction

            // Calculate final batch size
            var calculatedSize = baseBatchSize * cpuFactor * memoryFactor * connectionFactor * queueFactor * errorFactor;

            // Clamp to valid range
            var finalSize = (int)Math.Clamp(calculatedSize, 1, _options.MaxBatchSize);

            // Consider request type characteristics if available
            if (context.ExecutionMetrics != null)
            {
                finalSize = AdjustForRequestCharacteristics(finalSize, context.ExecutionMetrics);
            }

            return finalSize;
        }

        private int AdjustForRequestCharacteristics(int currentBatchSize, RequestExecutionMetrics metrics)
        {
            // Fast requests can handle larger batches
            if (metrics.AverageExecutionTime < TimeSpan.FromMilliseconds(50))
            {
                return Math.Min(currentBatchSize * 2, _options.MaxBatchSize);
            }

            // Slow requests need smaller batches
            if (metrics.AverageExecutionTime > TimeSpan.FromSeconds(1))
            {
                return Math.Max(currentBatchSize / 2, 1);
            }

            // High memory usage requests need smaller batches
            if (metrics.MemoryAllocated > 50 * 1024 * 1024) // 50MB
            {
                return Math.Max(currentBatchSize / 2, 1);
            }

            return currentBatchSize;
        }

        private double CalculateConfidence(OptimizationContext context)
        {
            var load = context.SystemLoad!;

            // Confidence based on data freshness and quality
            var timeSinceMeasurement = DateTime.UtcNow - load.Timestamp;
            var freshnessFactor = Math.Max(0, 1.0 - (timeSinceMeasurement.TotalMinutes / 5.0)); // Degrades over 5 minutes

            // Confidence based on load stability (extreme values = lower confidence)
            var stabilityFactor = 1.0;
            if (load.CpuUtilization > 0.95 || load.MemoryUtilization > 0.95)
            {
                stabilityFactor = 0.7; // Lower confidence under extreme load
            }

            return (freshnessFactor + stabilityFactor) / 2.0;
        }
    }
}