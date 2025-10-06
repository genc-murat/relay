namespace Relay.CLI.Commands;

internal class PipelineStageResult
{
    public string StageName { get; set; } = "";
    public string StageEmoji { get; set; } = "";
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Error { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> Details { get; set; } = new();
}
