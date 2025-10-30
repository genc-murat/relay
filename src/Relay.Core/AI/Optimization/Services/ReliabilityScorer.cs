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

        // Lower error rates are better
        var errorScore = 1.0 - Math.Min(errorRate, 1.0);
        var exceptionScore = Math.Max(0, 1.0 - exceptionCount / 100.0); // Normalize to 100 exceptions max

        return (errorScore + exceptionScore) / 2.0;
    }

    public override IEnumerable<string> GetCriticalAreas(Dictionary<string, double> metrics)
    {
        var areas = new List<string>();
        var errorRate = metrics.GetValueOrDefault("ErrorRate", 0);

        if (errorRate > 0.1) areas.Add("High error rate");

        return areas;
    }

    public override IEnumerable<string> GetRecommendations(Dictionary<string, double> metrics)
    {
        var recommendations = new List<string>();
        var errorRate = metrics.GetValueOrDefault("ErrorRate", 0);

        if (errorRate > 0.05) recommendations.Add("Implement better error handling and retry logic");

        return recommendations;
    }
}
