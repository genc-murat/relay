using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization.Strategies;

/// <summary>
/// Strategy for analyzing system-wide insights and providing global optimization recommendations.
/// </summary>
internal class SystemInsightsStrategy : IOptimizationStrategy
{
    private class SystemAnalysisResult
    {
        public string MetricName { get; set; } = string.Empty;
        public double CurrentValue { get; set; }
        public bool IsHigh { get; set; }
        public bool IsCritical { get; set; }
        public string[] Recommendations { get; set; } = Array.Empty<string>();
        public string Priority { get; set; } = "low";
        public InsightSeverity Severity { get; set; } = InsightSeverity.Info;
    }

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
        var insights = new List<TrendInsight>();

        // Analyze CPU utilization patterns
        var cpuInsights = AnalyzeCpuUtilization(systemLoad);
        insights.AddRange(GenerateTrendInsightsFromAnalysis("CPU", cpuInsights));

        // Analyze memory utilization patterns
        var memoryInsights = AnalyzeMemoryUtilization(systemLoad);
        insights.AddRange(GenerateTrendInsightsFromAnalysis("Memory", memoryInsights));

        // Analyze connection patterns
        var connectionInsights = AnalyzeConnectionPatterns(systemLoad);
        insights.AddRange(GenerateTrendInsightsFromAnalysis("Connections", connectionInsights));

        // Analyze queue patterns
        var queueInsights = AnalyzeQueuePatterns(systemLoad);
        insights.AddRange(GenerateTrendInsightsFromAnalysis("Queue", queueInsights));

        // Generate system-wide recommendations
        var recommendations = GenerateSystemRecommendations(new[] { cpuInsights, memoryInsights, connectionInsights, queueInsights });

        // Determine primary strategy based on most critical insight
        var primaryStrategy = DeterminePrimarySystemStrategy(recommendations);

        return new OptimizationRecommendation
        {
            Strategy = primaryStrategy,
            ConfidenceScore = CalculateOverallSystemConfidence(systemLoad),
            EstimatedImprovement = EstimateSystemImprovement(recommendations),
            Reasoning = $"System analysis identified {insights.Count} insights and {recommendations.Count} optimization opportunities",
            Parameters = new Dictionary<string, object> { ["insights"] = insights },
            Priority = DetermineSystemPriority(recommendations),
            EstimatedGainPercentage = EstimateOverallSystemGain(recommendations),
            Risk = DetermineSystemRisk(recommendations)
        };
    }

    private SystemAnalysisResult AnalyzeCpuUtilization(SystemLoadMetrics load)
    {
        var result = new SystemAnalysisResult
        {
            MetricName = "CPU Utilization",
            CurrentValue = load.CpuUtilization,
            IsHigh = load.CpuUtilization > 0.8,
            IsCritical = load.CpuUtilization > 0.95
        };

        if (load.CpuUtilization > 0.95)
        {
            result.Recommendations = new[] { "parallel_processing", "async_optimization", "cpu_affinity" };
            result.Priority = "high";
            result.Severity = InsightSeverity.Critical;
        }
        else if (load.CpuUtilization > 0.8)
        {
            result.Recommendations = new[] { "consolidation", "resource_sharing" };
            result.Priority = "medium";
            result.Severity = InsightSeverity.Warning;
        }
        else
        {
            result.Recommendations = new[] { "balanced_optimization" };
            result.Priority = "low";
            result.Severity = InsightSeverity.Info;
        }

        return result;
    }

    private SystemAnalysisResult AnalyzeMemoryUtilization(SystemLoadMetrics load)
    {
        var result = new SystemAnalysisResult
        {
            MetricName = "Memory Utilization",
            CurrentValue = load.MemoryUtilization,
            IsHigh = load.MemoryUtilization > 0.8,
            IsCritical = load.MemoryUtilization > 0.95
        };

        if (load.MemoryUtilization > 0.95)
        {
            result.Recommendations = new[] { "memory_pooling", "garbage_collection_tuning", "object_pooling" };
            result.Priority = "high";
            result.Severity = InsightSeverity.Critical;
        }
        else if (load.MemoryUtilization > 0.8)
        {
            result.Recommendations = new[] { "memory_preallocation", "caching_expansion" };
            result.Priority = "medium";
            result.Severity = InsightSeverity.Warning;
        }
        else
        {
            result.Recommendations = new[] { "memory_optimization" };
            result.Priority = "low";
            result.Severity = InsightSeverity.Info;
        }

        return result;
    }

    private SystemAnalysisResult AnalyzeConnectionPatterns(SystemLoadMetrics load)
    {
        var result = new SystemAnalysisResult
        {
            MetricName = "Active Connections",
            CurrentValue = load.ActiveConnections,
            IsHigh = load.ActiveConnections > 1000,
            IsCritical = false // Connections don't have a critical threshold like CPU/memory
        };

        if (load.ActiveConnections > 1000)
        {
            result.Recommendations = new[] { "resource_pooling", "load_balancing", "circuit_breaker" };
            result.Priority = "high";
            result.Severity = InsightSeverity.Warning;
        }
        else if (load.ActiveConnections < 10)
        {
            result.Recommendations = new[] { "connection_reuse", "keep_alive" };
            result.Priority = "low";
            result.Severity = InsightSeverity.Info;
        }
        else
        {
            result.Recommendations = new[] { "connection_optimization" };
            result.Priority = "medium";
            result.Severity = InsightSeverity.Info;
        }

        return result;
    }

    private SystemAnalysisResult AnalyzeQueuePatterns(SystemLoadMetrics load)
    {
        var result = new SystemAnalysisResult
        {
            MetricName = "Queued Requests",
            CurrentValue = load.QueuedRequestCount,
            IsHigh = load.QueuedRequestCount > 100,
            IsCritical = load.QueuedRequestCount > 100
        };

        if (load.QueuedRequestCount > 100)
        {
            result.Recommendations = new[] { "batching", "queue_optimization", "horizontal_scaling" };
            result.Priority = "critical";
            result.Severity = InsightSeverity.Critical;
        }
        else if (load.QueuedRequestCount == 0)
        {
            result.Recommendations = new[] { "resource_consolidation" };
            result.Priority = "low";
            result.Severity = InsightSeverity.Info;
        }
        else
        {
            result.Recommendations = new[] { "queue_monitoring" };
            result.Priority = "medium";
            result.Severity = InsightSeverity.Info;
        }

        return result;
    }

    private List<string> GenerateSystemRecommendations(SystemAnalysisResult[] analyses)
    {
        var recommendations = new List<string>();

        // Collect all recommendations from analyses
        foreach (var analysis in analyses)
        {
            recommendations.AddRange(analysis.Recommendations);
        }

        // Remove duplicates and prioritize
        return recommendations.Distinct().ToList();
    }

    private List<TrendInsight> GenerateTrendInsightsFromAnalysis(string category, SystemAnalysisResult analysis)
    {
        var insights = new List<TrendInsight>();

        if (analysis.IsCritical)
        {
            insights.Add(new TrendInsight
            {
                Category = $"{category} - Critical",
                Severity = analysis.Severity,
                Message = $"{analysis.MetricName} is at critical level: {analysis.CurrentValue:F2}",
                RecommendedAction = $"Implement {string.Join(", ", analysis.Recommendations)} immediately"
            });
        }
        else if (analysis.IsHigh)
        {
            insights.Add(new TrendInsight
            {
                Category = $"{category} - Warning",
                Severity = analysis.Severity,
                Message = $"{analysis.MetricName} is elevated: {analysis.CurrentValue:F2}",
                RecommendedAction = $"Consider {string.Join(", ", analysis.Recommendations)}"
            });
        }
        else
        {
            insights.Add(new TrendInsight
            {
                Category = $"{category} - Normal",
                Severity = InsightSeverity.Info,
                Message = $"{analysis.MetricName} is within normal range: {analysis.CurrentValue:F2}",
                RecommendedAction = analysis.Recommendations.Length > 0 ? string.Join(", ", analysis.Recommendations) : "Continue monitoring"
            });
        }

        return insights;
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