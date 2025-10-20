using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator;

/// <summary>
/// Represents a pipeline registration for validation.
/// </summary>
public class PipelineRegistration
{
    public ITypeSymbol? PipelineType { get; set; }
    public IMethodSymbol? Method { get; set; }
    public int Order { get; set; }
    public PipelineScope Scope { get; set; }
    public Location Location { get; set; } = null!;
    public AttributeData? Attribute { get; set; }
}
