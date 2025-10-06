namespace Relay.CLI.Commands.Models.Performance;

public class PerformanceIssue
{
    public string Type { get; set; } = "";
    public string Severity { get; set; } = "";
    public int Count { get; set; }
    public string Description { get; set; } = "";
    public string Recommendation { get; set; } = "";
    public string PotentialImprovement { get; set; } = "";
}


