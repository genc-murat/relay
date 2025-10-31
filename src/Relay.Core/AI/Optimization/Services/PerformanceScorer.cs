using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Relay.Core.AI.Optimization.Services;

/// <summary>
/// Performance health scorer implementation
/// </summary>
public class PerformanceScorer : HealthScorerBase
{
    public PerformanceScorer(ILogger logger) : base(logger) { }

    public override string Name => "PerformanceScorer";

    protected override double CalculateScoreCore(Dictionary<string, double> metrics)
    {
        var cpuUtil = metrics.GetValueOrDefault("CpuUtilization", 0); // Keep original default
        var memoryUtil = metrics.GetValueOrDefault("MemoryUtilization", 0); // Keep original default
        var throughput = metrics.GetValueOrDefault("ThroughputPerSecond", 100);

        // Sanitize input values to handle NaN, Infinity, and negative values
        cpuUtil = SanitizeMetricValue(cpuUtil);
        memoryUtil = SanitizeMetricValue(memoryUtil);
        throughput = Math.Max(0, throughput); // Just ensure non-negative

        // Lower utilization and higher throughput is better
        var cpuScore = 1.0 - cpuUtil;
        var memoryScore = 1.0 - memoryUtil;
        var throughputScore = Math.Min(throughput / 1000.0, 1.0); // Normalize to 1000 req/sec max

        return (cpuScore + memoryScore + throughputScore) / 3.0;
    }

    public override IEnumerable<string> GetCriticalAreas(Dictionary<string, double> metrics)
    {
        var areas = new List<string>();
        var cpuUtil = metrics.GetValueOrDefault("CpuUtilization", 0); // Use same default as CalculateScoreCore
        var memoryUtil = metrics.GetValueOrDefault("MemoryUtilization", 0); // Use same default as CalculateScoreCore

        // Sanitize input values to handle NaN, Infinity, and negative values
        cpuUtil = SanitizeMetricValue(cpuUtil);
        memoryUtil = SanitizeMetricValue(memoryUtil);

        if (cpuUtil > 0.9) areas.Add("High CPU utilization");
        if (memoryUtil > 0.9) areas.Add("High memory utilization");

        return areas;
    }

    public override IEnumerable<string> GetRecommendations(Dictionary<string, double> metrics)
    {
        var recommendations = new List<string>();
        var cpuUtil = metrics.GetValueOrDefault("CpuUtilization", 0); // Use same default as CalculateScoreCore
        var memoryUtil = metrics.GetValueOrDefault("MemoryUtilization", 0); // Use same default as CalculateScoreCore

        // Sanitize input values to handle NaN, Infinity, and negative values
        cpuUtil = SanitizeMetricValue(cpuUtil);
        memoryUtil = SanitizeMetricValue(memoryUtil);

        if (cpuUtil >= 0.8) recommendations.Add("Consider optimizing CPU-intensive operations");
        if (memoryUtil >= 0.8) recommendations.Add("Consider memory optimization techniques");

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
