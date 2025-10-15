namespace Relay.CLI.Commands;

public class PredictionResult
{
    public string Metric { get; set; } = "";
    public string PredictedValue { get; set; } = "";
    public double Confidence { get; set; }
}