namespace Relay.CLI.Commands;

public class PredictedBottleneck
{
    public string Component { get; set; } = "";
    public string Description { get; set; } = "";
    public double Probability { get; set; }
    public string Impact { get; set; } = "";
}