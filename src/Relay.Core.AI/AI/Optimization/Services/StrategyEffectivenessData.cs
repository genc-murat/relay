using Relay.Core.AI;
/// <summary>
/// Data class for strategy effectiveness information
/// </summary>
public class StrategyEffectivenessData
{
    public OptimizationStrategy Strategy { get; set; }
    public int TotalApplications { get; set; }
    public double SuccessRate { get; set; }
    public double AverageImprovement { get; set; }
    public double OverallEffectiveness { get; set; }
}
