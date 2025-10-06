namespace Relay.CLI.TemplateEngine;

public class PublishResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string PackagePath { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}
