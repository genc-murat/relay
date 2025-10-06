namespace Relay.CLI.Commands.Models.Pipeline;

internal class PipelineOptimizationResult
{
    public string Type { get; set; } = "";
    public bool Applied { get; set; }
    public string Impact { get; set; } = "";
}
