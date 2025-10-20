namespace Relay.CLI.Commands;

public class AIPredictionResults
{
    public double ExpectedThroughput { get; set; }
    public double ExpectedResponseTime { get; set; }
    public double ExpectedErrorRate { get; set; }
    public double ExpectedCpuUsage { get; set; }
    public double ExpectedMemoryUsage { get; set; }
    public PredictedBottleneck[] Bottlenecks { get; set; } = Array.Empty<PredictedBottleneck>();
    public string[] Recommendations { get; set; } = Array.Empty<string>();
}
