namespace Relay.CLI.TemplateEngine;

public class GenerationOptions
{
    public string? Author { get; set; }
    public string? TargetFramework { get; set; }
    public string? DatabaseProvider { get; set; }
    public bool EnableAuth { get; set; }
    public bool EnableSwagger { get; set; } = true;
    public bool EnableDocker { get; set; } = true;
    public bool EnableHealthChecks { get; set; } = true;
    public bool EnableCaching { get; set; }
    public bool EnableTelemetry { get; set; }
    public string[]? Modules { get; set; }
}
