namespace Relay.CLI.Commands.Models;

public class ReliabilityIssue
{
    public string Type { get; set; } = "";
    public string Severity { get; set; } = "";
    public int Count { get; set; }
    public string Description { get; set; } = "";
    public string Recommendation { get; set; } = "";
    public string Impact { get; set; } = "";
}


