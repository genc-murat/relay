namespace Relay.CLI.TemplateEngine;

public class GenerationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public List<string> CreatedDirectories { get; set; } = new();
    public List<string> CreatedFiles { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public TimeSpan Duration { get; set; }
}
