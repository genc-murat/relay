namespace Relay.CLI.TemplateEngine;

public class TemplateMetadata
{
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Identity { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string[] Classifications { get; set; } = Array.Empty<string>();
    public string Description { get; set; } = string.Empty;
    public string? Version { get; set; }
}
