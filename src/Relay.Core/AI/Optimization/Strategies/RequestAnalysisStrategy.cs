using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization.Strategies
{
    /// <summary>
    /// Strategy for analyzing request patterns and providing optimization recommendations.
    /// </summary>
    internal class RequestAnalysisStrategy : IOptimizationStrategy
    {
        private readonly ILogger _logger;

        public string Name => "RequestAnalysis";
        public int Priority => 100;

        public RequestAnalysisStrategy(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool CanHandle(string operation) => operation == "AnalyzeRequest";

        public async ValueTask<StrategyExecutionResult> ExecuteAsync(OptimizationContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                if (context.RequestType == null || context.ExecutionMetrics == null)
                {
                    return new StrategyExecutionResult
                    {
                        Success = false,
                        StrategyName = Name,
                        ErrorMessage = "Request type and execution metrics are required",
                        ExecutionTime = DateTime.UtcNow - startTime
                    };
                }

                // Analyze execution metrics
                var recommendation = AnalyzeExecutionMetrics(context.ExecutionMetrics);

                // Consider system load if available
                if (context.SystemLoad != null)
                {
                    recommendation = AdjustForSystemLoad(recommendation, context.SystemLoad);
                }

                // Calculate confidence based on data quality
                var confidence = CalculateConfidence(context.ExecutionMetrics);

                _logger.LogDebug("Request analysis completed for {RequestType}: {Strategy} (Confidence: {Confidence:P2})",
                    context.RequestType.Name, recommendation.Strategy, confidence);

                return new StrategyExecutionResult
                {
                    Success = true,
                    StrategyName = Name,
                    Confidence = confidence,
                    Data = recommendation,
                    ExecutionTime = DateTime.UtcNow - startTime,
                    Metadata = new()
                    {
                        ["request_type"] = context.RequestType.Name,
                        ["sample_size"] = context.ExecutionMetrics.TotalExecutions,
                        ["analysis_time"] = DateTime.UtcNow - startTime
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in request analysis strategy");

                return new StrategyExecutionResult
                {
                    Success = false,
                    StrategyName = Name,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        private OptimizationRecommendation AnalyzeExecutionMetrics(RequestExecutionMetrics metrics)
        {
            var strategies = new List<OptimizationStrategy>();

            // High CPU usage with slow execution -> SIMD optimization
            if (metrics.CpuUsage >= 0.8 && metrics.AverageExecutionTime > TimeSpan.FromMilliseconds(100))
            {
                strategies.Add(OptimizationStrategy.SIMDAcceleration);
            }

            // High memory allocation -> Memory pooling
            if (metrics.MemoryAllocated > 10 * 1024 * 1024) // 10MB
            {
                strategies.Add(OptimizationStrategy.MemoryPooling);
            }

            // High concurrent executions -> Batching
            if (metrics.ConcurrentExecutions > 10)
            {
                strategies.Add(OptimizationStrategy.BatchProcessing);
            }

            // Database calls -> Resource pooling optimization
            if (metrics.DatabaseCalls > 5)
            {
                strategies.Add(OptimizationStrategy.ResourcePooling);
            }

            // External API calls -> Circuit breaker
            if (metrics.ExternalApiCalls > 2)
            {
                strategies.Add(OptimizationStrategy.CircuitBreaker);
            }

            // Default strategy if none detected
            if (strategies.Count == 0)
            {
                strategies.Add(OptimizationStrategy.Caching);
            }

                var primaryStrategy = strategies[0];
                var expectedImprovementPercent = CalculateExpectedImprovement(metrics, primaryStrategy);

                return new OptimizationRecommendation
                {
                    Strategy = primaryStrategy,
                    ConfidenceScore = CalculateConfidence(metrics),
                    EstimatedImprovement = TimeSpan.FromMilliseconds(expectedImprovementPercent * metrics.AverageExecutionTime.TotalMilliseconds),
                    Reasoning = $"Analysis of {metrics.TotalExecutions} executions showed {primaryStrategy} as optimal strategy",
                    Parameters = new Dictionary<string, object>
                    {
                        ["alternative_strategies"] = strategies.Skip(1).ToArray(),
                        ["resource_requirements"] = EstimateResourceRequirements(primaryStrategy),
                        ["expected_improvement_percentage"] = expectedImprovementPercent
                    },
                    Priority = CalculatePriority(primaryStrategy),
                    EstimatedGainPercentage = expectedImprovementPercent,
                    Risk = CalculateRiskLevel(primaryStrategy)
                };
        }

        private OptimizationRecommendation AdjustForSystemLoad(OptimizationRecommendation recommendation, SystemLoadMetrics load)
        {
            // Under high load, prefer less resource-intensive strategies
            if (load.CpuUtilization > 0.8 || load.MemoryUtilization > 0.8)
            {
                if (recommendation.Strategy == OptimizationStrategy.SIMDAcceleration)
                {
                    return new OptimizationRecommendation
                    {
                        Strategy = OptimizationStrategy.Caching,
                        ConfidenceScore = recommendation.ConfidenceScore,
                        EstimatedImprovement = recommendation.EstimatedImprovement,
                        Reasoning = recommendation.Reasoning + " (adjusted for high system load)",
                        Parameters = recommendation.Parameters,
                        Priority = OptimizationPriority.Low,
                        EstimatedGainPercentage = recommendation.EstimatedGainPercentage,
                        Risk = RiskLevel.Low
                    };
                }
            }

            return recommendation;
        }

        private double CalculateConfidence(RequestExecutionMetrics metrics)
        {
            // Base confidence on sample size and data quality
            var sampleConfidence = Math.Min(metrics.TotalExecutions / 1000.0, 1.0); // Max at 1000 samples
            var successRateConfidence = metrics.SuccessfulExecutions / (double)metrics.TotalExecutions;

            return (sampleConfidence + successRateConfidence) / 2.0;
        }

        private double CalculateExpectedImprovement(RequestExecutionMetrics metrics, OptimizationStrategy strategy)
        {
            return strategy switch
            {
                OptimizationStrategy.Caching => 0.3, // 30% improvement
                OptimizationStrategy.BatchProcessing => 0.4, // 40% improvement
                OptimizationStrategy.SIMDAcceleration => 0.5, // 50% improvement
                OptimizationStrategy.MemoryPooling => 0.25, // 25% improvement
                OptimizationStrategy.ResourcePooling => 0.35, // 35% improvement
                OptimizationStrategy.CircuitBreaker => 0.2, // 20% improvement
                _ => 0.1
            };
        }

        private ResourceRequirements EstimateResourceRequirements(OptimizationStrategy strategy)
        {
            return strategy switch
            {
                OptimizationStrategy.Caching => new ResourceRequirements { MemoryMB = 50, CpuPercent = 5 },
                OptimizationStrategy.BatchProcessing => new ResourceRequirements { MemoryMB = 20, CpuPercent = 10 },
                OptimizationStrategy.SIMDAcceleration => new ResourceRequirements { MemoryMB = 10, CpuPercent = 15 },
                OptimizationStrategy.MemoryPooling => new ResourceRequirements { MemoryMB = 100, CpuPercent = 5 },
                OptimizationStrategy.ResourcePooling => new ResourceRequirements { MemoryMB = 30, CpuPercent = 5 },
                OptimizationStrategy.CircuitBreaker => new ResourceRequirements { MemoryMB = 15, CpuPercent = 2 },
                _ => new ResourceRequirements { MemoryMB = 10, CpuPercent = 5 }
            };
        }

        private RiskLevel CalculateRiskLevel(OptimizationStrategy strategy)
        {
            return strategy switch
            {
                OptimizationStrategy.Caching => RiskLevel.Low,
                OptimizationStrategy.BatchProcessing => RiskLevel.Medium,
                OptimizationStrategy.SIMDAcceleration => RiskLevel.High,
                OptimizationStrategy.MemoryPooling => RiskLevel.Medium,
                OptimizationStrategy.ResourcePooling => RiskLevel.Low,
                OptimizationStrategy.CircuitBreaker => RiskLevel.Low,
                _ => RiskLevel.Medium
            };
        }

        private OptimizationPriority CalculatePriority(OptimizationStrategy strategy)
        {
            return strategy switch
            {
                OptimizationStrategy.Caching => OptimizationPriority.Low,
                OptimizationStrategy.BatchProcessing => OptimizationPriority.Medium,
                OptimizationStrategy.SIMDAcceleration => OptimizationPriority.High,
                OptimizationStrategy.MemoryPooling => OptimizationPriority.Medium,
                OptimizationStrategy.ResourcePooling => OptimizationPriority.Low,
                OptimizationStrategy.CircuitBreaker => OptimizationPriority.High,
                _ => OptimizationPriority.Medium
            };
        }
    }
}