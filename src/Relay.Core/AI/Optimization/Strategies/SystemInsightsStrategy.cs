using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization.Strategies
{
    /// <summary>
    /// Strategy for analyzing system-wide insights and providing global optimization recommendations.
    /// </summary>
    internal class SystemInsightsStrategy : IOptimizationStrategy
    {
        private readonly ILogger _logger;

        public string Name => "SystemInsights";
        public int Priority => 60;

        public SystemInsightsStrategy(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool CanHandle(string operation) => operation == "AnalyzeSystemInsights";

        public async ValueTask<StrategyExecutionResult> ExecuteAsync(OptimizationContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                if (context.SystemLoad == null)
                {
                    return new StrategyExecutionResult
                    {
                        Success = false,
                        StrategyName = Name,
                        ErrorMessage = "System load metrics are required for system insights analysis",
                        ExecutionTime = DateTime.UtcNow - startTime
                    };
                }

                var systemInsights = AnalyzeSystemInsights(context);

                _logger.LogDebug("System insights analysis completed: {InsightsCount} insights generated",
                    systemInsights.Parameters.Count);

                return new StrategyExecutionResult
                {
                    Success = true,
                    StrategyName = Name,
                    Confidence = CalculateSystemConfidence(context),
                    Data = systemInsights,
                    ExecutionTime = DateTime.UtcNow - startTime,
                    Metadata = new()
                    {
                        ["cpu_utilization"] = context.SystemLoad.CpuUtilization,
                        ["memory_utilization"] = context.SystemLoad.MemoryUtilization,
                        ["active_connections"] = context.SystemLoad.ActiveConnections,
                        ["analysis_time"] = DateTime.UtcNow - startTime
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in system insights strategy");

                return new StrategyExecutionResult
                {
                    Success = false,
                    StrategyName = Name,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        private OptimizationRecommendation AnalyzeSystemInsights(OptimizationContext context)
        {
            var systemLoad = context.SystemLoad!;
            var insights = new Dictionary<string, object>();

            // Analyze CPU utilization patterns
            var cpuInsights = AnalyzeCpuUtilization(systemLoad);
            insights.Add("cpu_insights", cpuInsights);

            // Analyze memory utilization patterns
            var memoryInsights = AnalyzeMemoryUtilization(systemLoad);
            insights.Add("memory_insights", memoryInsights);

            // Analyze connection patterns
            var connectionInsights = AnalyzeConnectionPatterns(systemLoad);
            insights.Add("connection_insights", connectionInsights);

            // Analyze queue patterns
            var queueInsights = AnalyzeQueuePatterns(systemLoad);
            insights.Add("queue_insights", queueInsights);

            // Generate system-wide recommendations
            var recommendations = GenerateSystemRecommendations(cpuInsights, memoryInsights, connectionInsights, queueInsights);

            // Determine primary strategy based on most critical insight
            var primaryStrategy = DeterminePrimarySystemStrategy(recommendations);

            return new OptimizationRecommendation
            {
                Strategy = primaryStrategy,
                ConfidenceScore = CalculateOverallSystemConfidence(systemLoad),
                EstimatedImprovement = EstimateSystemImprovement(recommendations),
                Reasoning = $"System analysis identified {recommendations.Count} optimization opportunities",
                Parameters = insights,
                Priority = DetermineSystemPriority(recommendations),
                EstimatedGainPercentage = EstimateOverallSystemGain(recommendations),
                Risk = DetermineSystemRisk(recommendations)
            };
        }

        private Dictionary<string, object> AnalyzeCpuUtilization(SystemLoadMetrics load)
        {
            var insights = new Dictionary<string, object>();

            insights["utilization"] = load.CpuUtilization;
            insights["is_high"] = load.CpuUtilization > 0.8;
            insights["is_critical"] = load.CpuUtilization > 0.95;

            // Determine if CPU-bound optimizations are needed
            if (load.CpuUtilization > 0.8)
            {
                insights["recommendations"] = new[] { "parallel_processing", "async_optimization", "cpu_affinity" };
                insights["priority"] = "high";
            }
            else if (load.CpuUtilization < 0.3)
            {
                insights["recommendations"] = new[] { "consolidation", "resource_sharing" };
                insights["priority"] = "low";
            }
            else
            {
                insights["recommendations"] = new[] { "balanced_optimization" };
                insights["priority"] = "medium";
            }

            return insights;
        }

        private Dictionary<string, object> AnalyzeMemoryUtilization(SystemLoadMetrics load)
        {
            var insights = new Dictionary<string, object>();

            insights["utilization"] = load.MemoryUtilization;
            insights["is_high"] = load.MemoryUtilization > 0.8;
            insights["is_critical"] = load.MemoryUtilization > 0.95;

            if (load.MemoryUtilization > 0.8)
            {
                insights["recommendations"] = new[] { "memory_pooling", "garbage_collection_tuning", "object_pooling" };
                insights["priority"] = "high";
            }
            else if (load.MemoryUtilization < 0.3)
            {
                insights["recommendations"] = new[] { "memory_preallocation", "caching_expansion" };
                insights["priority"] = "low";
            }
            else
            {
                insights["recommendations"] = new[] { "memory_optimization" };
                insights["priority"] = "medium";
            }

            return insights;
        }

        private Dictionary<string, object> AnalyzeConnectionPatterns(SystemLoadMetrics load)
        {
            var insights = new Dictionary<string, object>();

            insights["active_connections"] = load.ActiveConnections;
            insights["is_high"] = load.ActiveConnections > 1000;
            insights["is_low"] = load.ActiveConnections < 10;

            if (load.ActiveConnections > 1000)
            {
                insights["recommendations"] = new[] { "resource_pooling", "load_balancing", "circuit_breaker" };
                insights["priority"] = "high";
            }
            else if (load.ActiveConnections < 10)
            {
                insights["recommendations"] = new[] { "connection_reuse", "keep_alive" };
                insights["priority"] = "low";
            }
            else
            {
                insights["recommendations"] = new[] { "connection_optimization" };
                insights["priority"] = "medium";
            }

            return insights;
        }

        private Dictionary<string, object> AnalyzeQueuePatterns(SystemLoadMetrics load)
        {
            var insights = new Dictionary<string, object>();

            insights["queued_requests"] = load.QueuedRequestCount;
            insights["is_backlogged"] = load.QueuedRequestCount > 100;
            insights["is_idle"] = load.QueuedRequestCount == 0;

            if (load.QueuedRequestCount > 100)
            {
                insights["recommendations"] = new[] { "batching", "queue_optimization", "horizontal_scaling" };
                insights["priority"] = "critical";
            }
            else if (load.QueuedRequestCount == 0)
            {
                insights["recommendations"] = new[] { "resource_consolidation" };
                insights["priority"] = "low";
            }
            else
            {
                insights["recommendations"] = new[] { "queue_monitoring" };
                insights["priority"] = "medium";
            }

            return insights;
        }

        private List<string> GenerateSystemRecommendations(
            Dictionary<string, object> cpuInsights,
            Dictionary<string, object> memoryInsights,
            Dictionary<string, object> connectionInsights,
            Dictionary<string, object> queueInsights)
        {
            var recommendations = new List<string>();

            // Collect all recommendations from insights
            recommendations.AddRange(GetInsightsRecommendations(cpuInsights));
            recommendations.AddRange(GetInsightsRecommendations(memoryInsights));
            recommendations.AddRange(GetInsightsRecommendations(connectionInsights));
            recommendations.AddRange(GetInsightsRecommendations(queueInsights));

            // Remove duplicates and prioritize
            return recommendations.Distinct().ToList();
        }

        private string[] GetInsightsRecommendations(Dictionary<string, object> insights)
        {
            return insights.GetValueOrDefault("recommendations", Array.Empty<string>()) as string[] ?? new string[0];
        }

        private OptimizationStrategy DeterminePrimarySystemStrategy(List<string> recommendations)
        {
            // Map recommendations to strategies
            var strategyVotes = new Dictionary<OptimizationStrategy, int>();

            foreach (var recommendation in recommendations)
            {
                var strategy = MapRecommendationToStrategy(recommendation);
                if (strategy != OptimizationStrategy.None)
                {
                    strategyVotes[strategy] = strategyVotes.GetValueOrDefault(strategy, 0) + 1;
                }
            }

            // Return most voted strategy
            return strategyVotes.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key;
        }

        private OptimizationStrategy MapRecommendationToStrategy(string recommendation)
        {
            return recommendation switch
            {
                "parallel_processing" => OptimizationStrategy.ParallelProcessing,
                "async_optimization" => OptimizationStrategy.StreamingOptimization,
                "memory_pooling" => OptimizationStrategy.MemoryPooling,
                "caching_expansion" => OptimizationStrategy.Caching,
                "resource_pooling" => OptimizationStrategy.ResourcePooling,
                "circuit_breaker" => OptimizationStrategy.CircuitBreaker,
                "batching" => OptimizationStrategy.BatchProcessing,
                _ => OptimizationStrategy.None
            };
        }

        private double CalculateOverallSystemConfidence(SystemLoadMetrics load)
        {
            // Confidence based on data freshness and metric stability
            var timeSinceMeasurement = DateTime.UtcNow - load.Timestamp;
            var freshnessFactor = Math.Max(0, 1.0 - (timeSinceMeasurement.TotalMinutes / 10.0)); // Degrades over 10 minutes

            // Higher confidence when metrics are in normal ranges
            var stabilityFactor = 1.0;
            if (load.CpuUtilization > 0.95 || load.MemoryUtilization > 0.95 || load.QueuedRequestCount > 500)
            {
                stabilityFactor = 0.8; // Lower confidence under extreme conditions
            }

            return (freshnessFactor + stabilityFactor) / 2.0;
        }

        private TimeSpan EstimateSystemImprovement(List<string> recommendations)
        {
            // Estimate improvement based on number and type of recommendations
            var baseImprovement = recommendations.Count * 50; // 50ms per recommendation
            return TimeSpan.FromMilliseconds(baseImprovement);
        }

        private OptimizationPriority DetermineSystemPriority(List<string> recommendations)
        {
            var criticalCount = recommendations.Count(r =>
                r.Contains("scaling") || r.Contains("circuit_breaker") || r.Contains("critical"));

            if (criticalCount > 0) return OptimizationPriority.Critical;
            if (recommendations.Count > 5) return OptimizationPriority.High;
            if (recommendations.Count > 2) return OptimizationPriority.Medium;
            return OptimizationPriority.Low;
        }

        private double EstimateOverallSystemGain(List<string> recommendations)
        {
            // Conservative estimate based on recommendation count
            return Math.Min(recommendations.Count * 0.05, 0.3); // Max 30% improvement
        }

        private RiskLevel DetermineSystemRisk(List<string> recommendations)
        {
            // System-level changes are generally higher risk
            return recommendations.Count > 3 ? RiskLevel.Medium : RiskLevel.Low;
        }

        private double CalculateSystemConfidence(OptimizationContext context)
        {
            var load = context.SystemLoad!;

            // Base confidence on data completeness
            var completenessFactor = 1.0;
            if (load.CpuUtilization < 0 || load.MemoryUtilization < 0)
            {
                completenessFactor = 0.7;
            }

            // Confidence based on measurement recency
            var recencyFactor = Math.Max(0.5, 1.0 - (DateTime.UtcNow - load.Timestamp).TotalMinutes / 15.0);

            return (completenessFactor + recencyFactor) / 2.0;
        }
    }
}