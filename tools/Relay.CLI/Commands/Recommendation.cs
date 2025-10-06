namespace Relay.CLI.Commands;

public class Recommendation
{
    public string Category { get; set; } = "";
    public string Priority { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Actions { get; set; } = new();
    public string EstimatedImpact { get; set; } = "";
}


