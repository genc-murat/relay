namespace Relay.CLI.Commands;

public class OptimizationAction
{
    public string FilePath { get; set; } = "";
    public string Type { get; set; } = "";
    public List<string> Modifications { get; set; } = new();
    public string OriginalContent { get; set; } = "";
    public string OptimizedContent { get; set; } = "";
}