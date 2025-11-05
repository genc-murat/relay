using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Relay.Core.AI.Optimization.Services;

/// <summary>
/// Security health scorer implementation
/// </summary>
public class SecurityScorer : HealthScorerBase
{
    public SecurityScorer(ILogger logger) : base(logger) { }

    public override string Name => "SecurityScorer";

    protected override double CalculateScoreCore(Dictionary<string, double> metrics)
    {
        // Simplified security scoring based on available metrics
        var failedAuthAttempts = metrics.GetValueOrDefault("FailedAuthAttempts", 0);
        var knownVulnerabilities = metrics.GetValueOrDefault("KnownVulnerabilities", 0);
        var dataEncryptionEnabled = metrics.GetValueOrDefault("DataEncryptionEnabled", 1);

        // Sanitize input values to handle NaN, Infinity, and negative values
        failedAuthAttempts = double.IsNaN(failedAuthAttempts) || double.IsInfinity(failedAuthAttempts) ? 0 : Math.Max(0, failedAuthAttempts);
        knownVulnerabilities = double.IsNaN(knownVulnerabilities) || double.IsInfinity(knownVulnerabilities) ? 0 : Math.Max(0, knownVulnerabilities);
        dataEncryptionEnabled = SanitizeMetricValue(dataEncryptionEnabled); // This is a binary or percentage value

        var authScore = Math.Max(0, 1.0 - (failedAuthAttempts / 100.0));
        var vulnScore = Math.Max(0, 1.0 - (knownVulnerabilities / 10.0));

        var result = (authScore + vulnScore + dataEncryptionEnabled) / 3.0;
        
        // Ensure result is within [0, 1] range, handling any remaining NaN/Infinity
        if (double.IsNaN(result) || double.IsInfinity(result))
            return 0.5; // Neutral score
            
        return Math.Max(0.0, Math.Min(1.0, result)); // Clamp to [0, 1]
    }

    public override IEnumerable<string> GetCriticalAreas(Dictionary<string, double> metrics)
    {
        var areas = new List<string>();
        var failedAuthAttempts = metrics.GetValueOrDefault("FailedAuthAttempts", 0);

        // Sanitize input value to handle NaN, Infinity, and negative values
        failedAuthAttempts = Math.Max(0, failedAuthAttempts); // Ensure non-negative

        if (failedAuthAttempts > 50) areas.Add("High failed authentication attempts");

        return areas;
    }

    public override IEnumerable<string> GetRecommendations(Dictionary<string, double> metrics)
    {
        var recommendations = new List<string>();
        var failedAuthAttempts = metrics.GetValueOrDefault("FailedAuthAttempts", 0);

        // Sanitize input value to handle NaN, Infinity, and negative values
        failedAuthAttempts = Math.Max(0, failedAuthAttempts); // Ensure non-negative

        if (failedAuthAttempts >= 10) recommendations.Add("Review authentication security measures");

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
