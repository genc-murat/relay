using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Relay.Core.AI.Optimization.Services;

/// <summary>
/// Maintainability health scorer implementation
/// </summary>
public class MaintainabilityScorer : HealthScorerBase
{
    public MaintainabilityScorer(ILogger logger) : base(logger) { }

    public override string Name => "MaintainabilityScorer";

    protected override double CalculateScoreCore(Dictionary<string, double> metrics)
    {
        var errorRate = metrics.GetValueOrDefault("ErrorRate", 0.1);
        var cpuUtil = metrics.GetValueOrDefault("CpuUtilization", 0.5);
        var memoryUtil = metrics.GetValueOrDefault("MemoryUtilization", 0.5);

        var errorScore = 1.0 - Math.Min(errorRate * 2, 1.0);
        var resourceScore = 1.0 - Math.Max(cpuUtil, memoryUtil);

        return (errorScore + resourceScore) / 2.0;
    }

    public override IEnumerable<string> GetCriticalAreas(Dictionary<string, double> metrics)
    {
        var areas = new List<string>();
        var errorRate = metrics.GetValueOrDefault("ErrorRate", 0);

        if (errorRate > 0.2) areas.Add("High error rate affecting maintainability");

        return areas;
    }

    public override IEnumerable<string> GetRecommendations(Dictionary<string, double> metrics)
    {
        var recommendations = new List<string>();
        var errorRate = metrics.GetValueOrDefault("ErrorRate", 0);

        if (errorRate > 0.1) recommendations.Add("Improve code quality and testing to reduce errors");

        return recommendations;
    }
}