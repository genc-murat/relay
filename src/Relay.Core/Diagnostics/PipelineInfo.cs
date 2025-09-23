namespace Relay.Core.Diagnostics;

/// <summary>
/// Information about a registered pipeline behavior
/// </summary>
public class PipelineInfo
{
    /// <summary>
    /// The type implementing the pipeline behavior
    /// </summary>
    public string PipelineType { get; set; } = string.Empty;

    /// <summary>
    /// The method name implementing the pipeline
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// Execution order of the pipeline
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Scope of the pipeline (Global, Request, etc.)
    /// </summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>
    /// Whether the pipeline is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}