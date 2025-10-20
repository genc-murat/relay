namespace Relay.CLI.Commands;

public class AIOptimizationResults
{
    public OptimizationResult[] AppliedOptimizations { get; set; } = Array.Empty<OptimizationResult>();
    public double OverallImprovement { get; set; }
}
