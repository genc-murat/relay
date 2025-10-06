namespace Relay.CLI.Commands;

internal class PipelineOptimizationResult
{
    public string Type { get; set; } = "";
    public bool Applied { get; set; }
    public string Impact { get; set; } = "";
}
