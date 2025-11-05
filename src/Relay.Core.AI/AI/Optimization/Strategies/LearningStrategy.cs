using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization.Strategies;

/// <summary>
/// Strategy for learning from optimization results and adapting future recommendations.
/// </summary>
internal class LearningStrategy : IOptimizationStrategy
{
    private readonly ILogger _logger;

    public string Name => "Learning";
    public int Priority => 70;

    public LearningStrategy(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool CanHandle(string operation) => operation == "LearnFromResults";

    public async ValueTask<StrategyExecutionResult> ExecuteAsync(OptimizationContext context, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            if (context.AppliedStrategies == null || context.AppliedStrategies.Length == 0)
            {
                return new StrategyExecutionResult
                {
                    Success = false,
                    StrategyName = Name,
                    ErrorMessage = "Applied strategies history is required for learning",
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }

            var learningInsights = AnalyzeOptimizationHistory(context);

            _logger.LogDebug("Learning analysis completed for {RequestType}: {InsightsCount} insights generated",
                context.RequestType?.Name ?? "Unknown", learningInsights.Parameters.Count);

            return new StrategyExecutionResult
            {
                Success = true,
                StrategyName = Name,
                Confidence = CalculateLearningConfidence(context),
                Data = learningInsights,
                ExecutionTime = DateTime.UtcNow - startTime,
                Metadata = new()
                {
                    ["request_type"] = context.RequestType?.Name ?? "Unknown",
                    ["applied_strategies_count"] = context.AppliedStrategies.Length,
                    ["analysis_time"] = DateTime.UtcNow - startTime
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in learning strategy");

            return new StrategyExecutionResult
            {
                Success = false,
                StrategyName = Name,
                ErrorMessage = ex.Message,
                ExecutionTime = DateTime.UtcNow - startTime
            };
        }
    }

    private OptimizationRecommendation AnalyzeOptimizationHistory(OptimizationContext context)
    {
        var appliedStrategies = context.AppliedStrategies!;
        var successfulStrategies = appliedStrategies.Where(s => s.Success).ToArray();
        var failedStrategies = appliedStrategies.Where(s => !s.Success).ToArray();

        // Calculate success rates by strategy type
        var strategySuccessRates = CalculateStrategySuccessRates(appliedStrategies);

        // Identify patterns in successful optimizations
        var successfulPatterns = IdentifySuccessfulPatterns(successfulStrategies);

        // Generate learning recommendations
        var recommendations = GenerateLearningRecommendations(strategySuccessRates, successfulPatterns);

        return new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.Custom, // Learning doesn't apply a specific strategy, but informs future decisions
            ConfidenceScore = Math.Min(successfulStrategies.Length / (double)appliedStrategies.Length + 0.3, 0.95),
            EstimatedImprovement = TimeSpan.FromMilliseconds(
                successfulStrategies.Average(s => s.ActualImprovement?.TotalMilliseconds ?? 0) * 0.1), // Conservative estimate
            Reasoning = $"Learned from {appliedStrategies.Length} optimizations ({successfulStrategies.Length} successful)",
            Parameters = recommendations,
            Priority = OptimizationPriority.Medium,
            EstimatedGainPercentage = successfulStrategies.Length / (double)appliedStrategies.Length * 0.15,
            Risk = RiskLevel.VeryLow
        };
    }

    private Dictionary<string, double> CalculateStrategySuccessRates(AppliedOptimizationResult[] appliedStrategies)
    {
        var strategyGroups = appliedStrategies
            .GroupBy(s => s.Strategy)
            .ToDictionary(g => g.Key.ToString(), g =>
            {
                var total = g.Count();
                var successful = g.Count(s => s.Success);
                return total > 0 ? (double)successful / total : 0.0;
            });

        return strategyGroups;
    }

    private Dictionary<string, object> IdentifySuccessfulPatterns(AppliedOptimizationResult[] successfulStrategies)
    {
        var patterns = new Dictionary<string, object>();

        if (successfulStrategies.Length == 0) return patterns;

        // Analyze timing patterns
        var avgExecutionTime = successfulStrategies
            .Where(s => s.ActualImprovement.HasValue)
            .Average(s => s.ActualImprovement!.Value.TotalMilliseconds);

        patterns["avg_improvement_ms"] = avgExecutionTime;

        // Analyze strategy combinations that work well
        var strategyCombinations = successfulStrategies
            .GroupBy(s => s.Strategy)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        patterns["top_strategies"] = strategyCombinations;

        // Analyze confidence vs actual improvement correlation
        var confidenceCorrelation = CalculateConfidenceCorrelation(successfulStrategies);
        patterns["confidence_correlation"] = confidenceCorrelation;

        return patterns;
    }

    private Dictionary<string, object> GenerateLearningRecommendations(
        Dictionary<string, double> successRates,
        Dictionary<string, object> patterns)
    {
        var recommendations = new Dictionary<string, object>();

        // Recommend strategies with high success rates
        var highSuccessStrategies = successRates
            .Where(kvp => kvp.Value > 0.7)
            .OrderByDescending(kvp => kvp.Value)
            .Take(3)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        recommendations["preferred_strategies"] = highSuccessStrategies;

        // Avoid strategies with low success rates
        var lowSuccessStrategies = successRates
            .Where(kvp => kvp.Value < 0.3)
            .Select(kvp => kvp.Key)
            .ToArray();

        recommendations["avoid_strategies"] = lowSuccessStrategies;

        // Learning insights
        recommendations["insights"] = patterns;

        // Confidence adjustment factors
        var confidenceAdjustment = patterns.GetValueOrDefault("confidence_correlation", 0.0) as double? ?? 0.0;
        recommendations["confidence_adjustment_factor"] = Math.Clamp(confidenceAdjustment, -0.2, 0.2);

        return recommendations;
    }

    private double CalculateConfidenceCorrelation(AppliedOptimizationResult[] successfulStrategies)
    {
        var correlations = successfulStrategies
            .Where(s => s.ActualImprovement.HasValue)
            .Select(s =>
            {
                var expectedImprovement = s.ExpectedImprovement?.TotalMilliseconds ?? 0;
                var actualImprovement = s.ActualImprovement!.Value.TotalMilliseconds;
                var confidence = s.ConfidenceScore;

                // Correlation between confidence and accuracy of prediction
                var accuracy = expectedImprovement > 0 ?
                    Math.Min(actualImprovement / expectedImprovement, 2.0) : 0.0; // Cap at 2x

                return (confidence, accuracy);
            })
            .ToArray();

        if (correlations.Length < 2) return 0.0;

        // Simple correlation coefficient
        var avgConfidence = correlations.Average(c => c.confidence);
        var avgAccuracy = correlations.Average(c => c.accuracy);

        var numerator = correlations.Sum(c => (c.confidence - avgConfidence) * (c.accuracy - avgAccuracy));
        var denominator = Math.Sqrt(
            correlations.Sum(c => Math.Pow(c.confidence - avgConfidence, 2)) *
            correlations.Sum(c => Math.Pow(c.accuracy - avgAccuracy, 2))
        );

        return denominator > 0 ? numerator / denominator : 0.0;
    }

    private double CalculateLearningConfidence(OptimizationContext context)
    {
        var appliedStrategies = context.AppliedStrategies!;
        var sampleSize = appliedStrategies.Length;

        // Confidence increases with sample size
        var sampleConfidence = Math.Min(sampleSize / 20.0, 1.0); // Max at 20 samples

        // Confidence based on success rate stability
        var recentSuccessRate = appliedStrategies.Skip(Math.Max(0, appliedStrategies.Length - 5)).Count(s => s.Success) / 5.0;
        var overallSuccessRate = appliedStrategies.Count(s => s.Success) / (double)sampleSize;

        var stabilityFactor = 1.0 - Math.Abs(recentSuccessRate - overallSuccessRate);

        return (sampleConfidence + stabilityFactor) / 2.0;
    }
}