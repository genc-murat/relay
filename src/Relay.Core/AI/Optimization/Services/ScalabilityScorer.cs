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

        // Sanitize input values to handle NaN, Infinity, and negative values
        threadCount = double.IsNaN(threadCount) || double.IsInfinity(threadCount) ? 50 : Math.Max(0, threadCount);
        handleCount = double.IsNaN(handleCount) || double.IsInfinity(handleCount) ? 1000 : Math.Max(0, handleCount);
        throughput = double.IsNaN(throughput) || double.IsInfinity(throughput) ? 100 : Math.Max(0, throughput);

        // Higher throughput with reasonable resource usage is better
        var threadEfficiency = Math.Min(throughput / Math.Max(threadCount, 1), 10.0) / 10.0;
        var handleEfficiency = Math.Min(throughput / Math.Max(handleCount / 100.0, 1), 10.0) / 10.0;

        var result = (threadEfficiency + handleEfficiency) / 2.0;
        
        // Ensure result is within [0, 1] range, handling any remaining NaN/Infinity
        if (double.IsNaN(result) || double.IsInfinity(result))
            return 0.5; // Neutral score
            
        return Math.Max(0.0, Math.Min(1.0, result)); // Clamp to [0, 1]
    }

    public override IEnumerable<string> GetCriticalAreas(Dictionary<string, double> metrics)
    {
        var areas = new List<string>();
        var threadCount = metrics.GetValueOrDefault("ThreadCount", 50);
        var handleCount = metrics.GetValueOrDefault("HandleCount", 1000);

        // Sanitize input values to handle NaN, Infinity, and negative values
        threadCount = Math.Max(0, threadCount); // Ensure non-negative
        handleCount = Math.Max(0, handleCount); // Ensure non-negative

        if (threadCount > 200) areas.Add("High thread count");
        if (handleCount > 5000) areas.Add("High handle count");

        return areas;
    }

    public override IEnumerable<string> GetRecommendations(Dictionary<string, double> metrics)
    {
        var recommendations = new List<string>();
        var threadCount = metrics.GetValueOrDefault("ThreadCount", 50);

        // Sanitize input value to handle NaN, Infinity, and negative values
        threadCount = Math.Max(0, threadCount); // Ensure non-negative

        if (threadCount >= 100) recommendations.Add("Consider thread pooling optimizations");

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
