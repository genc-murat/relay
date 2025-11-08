using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator.Validators;

/// <summary>
/// Information about a pipeline for duplicate order validation.
/// </summary>
public struct PipelineInfo
{
    public string MethodName { get; set; }
    public int Order { get; set; }
    public int Scope { get; set; }
    public Location Location { get; set; }
    public string ContainingType { get; set; }
}
