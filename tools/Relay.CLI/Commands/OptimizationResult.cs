namespace Relay.CLI.Commands;

public class OptimizationResult
{
    public string Strategy { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Success { get; set; }
    public double PerformanceGain { get; set; }
}
