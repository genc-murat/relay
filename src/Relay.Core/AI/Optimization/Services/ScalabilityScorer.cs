using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Relay.Core.AI.Optimization.Services;

/// <summary>
/// Scalability health scorer implementation
/// </summary>
public class ScalabilityScorer : HealthScorerBase
{
    public ScalabilityScorer(ILogger logger) : base(logger) { }

    public override string Name => "ScalabilityScorer";

    protected override double CalculateScoreCore(Dictionary<string, double> metrics)
    {
        var threadCount = metrics.GetValueOrDefault("ThreadCount", 50);
        var handleCount = metrics.GetValueOrDefault("HandleCount", 1000);
        var throughput = metrics.GetValueOrDefault("ThroughputPerSecond", 100);

        // Higher throughput with reasonable resource usage is better
        var threadEfficiency = Math.Min(throughput / Math.Max(threadCount, 1), 10.0) / 10.0;
        var handleEfficiency = Math.Min(throughput / Math.Max(handleCount / 100.0, 1), 10.0) / 10.0;

        return (threadEfficiency + handleEfficiency) / 2.0;
    }

    public override IEnumerable<string> GetCriticalAreas(Dictionary<string, double> metrics)
    {
        var areas = new List<string>();
        var threadCount = metrics.GetValueOrDefault("ThreadCount", 50);
        var handleCount = metrics.GetValueOrDefault("HandleCount", 1000);

        if (threadCount > 200) areas.Add("High thread count");
        if (handleCount > 5000) areas.Add("High handle count");

        return areas;
    }

    public override IEnumerable<string> GetRecommendations(Dictionary<string, double> metrics)
    {
        var recommendations = new List<string>();
        var threadCount = metrics.GetValueOrDefault("ThreadCount", 50);

        if (threadCount > 100) recommendations.Add("Consider thread pooling optimizations");

        return recommendations;
    }
}
