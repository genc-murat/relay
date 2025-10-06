namespace Relay.CLI.Commands.Models.Pipeline;

// Support classes
internal class PipelineResult
{
    public List<PipelineStageResult> Stages { get; set; } = new();
    public TimeSpan TotalDuration { get; set; }
    public bool Success { get; set; }
}
