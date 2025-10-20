namespace Relay.CLI.Commands;

public class AIInsightsResults
{
    public double HealthScore { get; set; }
    public char PerformanceGrade { get; set; }
    public double ReliabilityScore { get; set; }
    public string[] CriticalIssues { get; set; } = Array.Empty<string>();
    public OptimizationOpportunity[] OptimizationOpportunities { get; set; } = Array.Empty<OptimizationOpportunity>();
    public PredictionResult[] Predictions { get; set; } = Array.Empty<PredictionResult>();
}