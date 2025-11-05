using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Relay.Core.AI.Optimization.Services;

/// <summary>
/// Reliability health scorer implementation
/// </summary>
public class ReliabilityScorer : HealthScorerBase
{
    public ReliabilityScorer(ILogger logger) : base(logger) { }

    public override string Name => "ReliabilityScorer";

    protected override double CalculateScoreCore(Dictionary<string, double> metrics)
    {
        var errorRate = metrics.GetValueOrDefault("ErrorRate", 0.1);
        var exceptionCount = metrics.GetValueOrDefault("ExceptionCount", 0);

        // Sanitize input values to handle NaN, Infinity, and negative values
        errorRate = SanitizeMetricValue(errorRate);
        exceptionCount = Math.Max(0, exceptionCount); // Just ensure non-negative for exception count

        // Lower error rates are better
        var errorScore = 1.0 - Math.Min(errorRate, 1.0);
        var exceptionScore = Math.Max(0, 1.0 - exceptionCount / 100.0); // Normalize to 100 exceptions max

        return (errorScore + exceptionScore) / 2.0;
    }

    public override IEnumerable<string> GetCriticalAreas(Dictionary<string, double> metrics)
    {
        var areas = new List<string>();
        var errorRate = metrics.GetValueOrDefault("ErrorRate", 0.1); // Use same default as CalculateScoreCore

        // Sanitize input value to handle NaN, Infinity, and negative values
        errorRate = SanitizeMetricValue(errorRate);

        if (errorRate > 0.1) areas.Add("High error rate");

        return areas;
    }

    public override IEnumerable<string> GetRecommendations(Dictionary<string, double> metrics)
    {
        var recommendations = new List<string>();
        var errorRate = metrics.GetValueOrDefault("ErrorRate", 0.1); // Use same default as CalculateScoreCore

        // Sanitize input value to handle NaN, Infinity, and negative values
        errorRate = SanitizeMetricValue(errorRate);

        if (errorRate >= 0.05) recommendations.Add("Implement better error handling and retry logic");

        return recommendations;
    }

    private static double SanitizeMetricValue(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            return 0.0;
        }
        
        // Cap at 1.0 to ensure values are within [0, 1] range for percentage metrics
        return Math.Min(value, 1.0);
    }
}
