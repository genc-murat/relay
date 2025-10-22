using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Relay.Core.AI;

namespace Relay.Core.AI.Optimization.Services;

/// <summary>
/// Service for applying machine learning enhancements to optimization strategies
/// </summary>
public class MachineLearningEnhancementService
{
    private readonly ILogger _logger;

    public MachineLearningEnhancementService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public MachineLearningEnhancement ApplyMachineLearningEnhancements(
        OptimizationRecommendation baseRecommendation,
        RequestAnalysisData analysisData,
        Dictionary<string, double> systemMetrics)
    {
        if (baseRecommendation == null) throw new ArgumentNullException(nameof(baseRecommendation));
        if (analysisData == null) throw new ArgumentNullException(nameof(analysisData));
        if (systemMetrics == null) throw new ArgumentNullException(nameof(systemMetrics));

        // Apply ML-based adjustments to the recommendation
        var enhancedStrategy = EnhanceStrategyWithML(baseRecommendation.Strategy, analysisData);
        var enhancedConfidence = AdjustConfidenceWithML(baseRecommendation.ConfidenceScore, analysisData);
        var reasoning = GenerateMLReasoning(analysisData, systemMetrics);

        var enhancement = new MachineLearningEnhancement
        {
            AlternativeStrategy = enhancedStrategy,
            EnhancedConfidence = enhancedConfidence,
            Reasoning = reasoning
        };

        // Add additional parameters based on ML insights
        var insights = GenerateMLInsights(analysisData, systemMetrics);
        foreach (var insight in insights)
        {
            enhancement.AdditionalParameters[$"insight_{insights.IndexOf(insight)}"] = insight;
        }

        // Add trend information
        var performanceTrend = analysisData.CalculatePerformanceTrend();
        if (Math.Abs(performanceTrend) >= 0.05)
        {
            enhancement.AdditionalParameters["trend_direction"] = performanceTrend > 0 ? "improving" : "degrading";
            enhancement.AdditionalParameters["trend_magnitude"] = Math.Abs(performanceTrend);
            enhancement.AdditionalParameters["trend_confidence"] = 0.7;
        }

        return enhancement;
    }

    private OptimizationStrategy EnhanceStrategyWithML(OptimizationStrategy baseStrategy, RequestAnalysisData analysisData)
    {
        // Simple ML-based strategy enhancement
        if (analysisData.ErrorRate > 0.2 && baseStrategy != OptimizationStrategy.CircuitBreaker)
        {
            return OptimizationStrategy.CircuitBreaker;
        }

        if (analysisData.CacheHitRatio > 0.8 && baseStrategy == OptimizationStrategy.None)
        {
            return OptimizationStrategy.EnableCaching;
        }

        return baseStrategy;
    }

    private double AdjustConfidenceWithML(double baseConfidence, RequestAnalysisData analysisData)
    {
        // Adjust confidence based on historical success rates
        var historicalSuccessRate = analysisData.SuccessRate;
        var adjustment = (historicalSuccessRate - 0.5) * 0.2; // +/- 0.2 adjustment

        return Math.Max(0.1, Math.Min(0.95, baseConfidence + adjustment));
    }



    private string GenerateMLReasoning(RequestAnalysisData analysisData, Dictionary<string, double> systemMetrics)
    {
        var reasons = new List<string>();

        if (analysisData.ErrorRate > 0.2)
            reasons.Add("High error rate detected, switching to circuit breaker strategy");

        if (analysisData.CacheHitRatio > 0.8)
            reasons.Add("Excellent cache performance, enabling caching optimizations");

        if (analysisData.RepeatRequestRate > 0.7)
            reasons.Add("High repeat request rate indicates strong caching opportunity");

        if (analysisData.DatabaseCalls > 10)
            reasons.Add("Multiple database calls suggest optimization potential");

        var cpuUtil = systemMetrics.GetValueOrDefault("CpuUtilization", 0);
        if (cpuUtil > 0.8)
            reasons.Add("High CPU utilization may impact optimization effectiveness");

        return string.Join("; ", reasons);
    }

    private List<string> GenerateMLInsights(RequestAnalysisData analysisData, Dictionary<string, double> systemMetrics)
    {
        var insights = new List<string>();

        if (analysisData.RepeatRequestRate > 0.7)
            insights.Add("High repeat request rate suggests strong caching opportunity");

        if (analysisData.DatabaseCalls > 10)
            insights.Add("Multiple database calls per request indicate optimization potential");

        var cpuUtil = systemMetrics.GetValueOrDefault("CpuUtilization", 0);
        if (cpuUtil > 0.8)
            insights.Add("High CPU utilization may limit parallel processing effectiveness");

        return insights;
    }
}

