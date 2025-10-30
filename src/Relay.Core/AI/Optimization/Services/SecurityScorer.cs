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

        var authScore = Math.Max(0, 1.0 - (failedAuthAttempts / 100.0));
        var vulnScore = Math.Max(0, 1.0 - (knownVulnerabilities / 10.0));

        return (authScore + vulnScore + dataEncryptionEnabled) / 3.0;
    }

    public override IEnumerable<string> GetCriticalAreas(Dictionary<string, double> metrics)
    {
        var areas = new List<string>();
        var failedAuthAttempts = metrics.GetValueOrDefault("FailedAuthAttempts", 0);

        if (failedAuthAttempts > 50) areas.Add("High failed authentication attempts");

        return areas;
    }

    public override IEnumerable<string> GetRecommendations(Dictionary<string, double> metrics)
    {
        var recommendations = new List<string>();
        var failedAuthAttempts = metrics.GetValueOrDefault("FailedAuthAttempts", 0);

        if (failedAuthAttempts > 10) recommendations.Add("Review authentication security measures");

        return recommendations;
    }
}
