namespace Relay.CLI.Commands;

public class OptimizationOpportunity
{
    public string Strategy { get; set; } = "";
    public string Description { get; set; } = "";
    public double ExpectedImprovement { get; set; }
    public double Confidence { get; set; }
    public string RiskLevel { get; set; } = "";
    public string Title { get; set; } = "";
}
