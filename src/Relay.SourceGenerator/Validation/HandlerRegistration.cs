using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Represents a handler registration for validation.
    /// </summary>
    public class HandlerRegistration
    {
        public ITypeSymbol RequestType { get; set; } = null!;
        public ITypeSymbol? ResponseType { get; set; }
        public IMethodSymbol Method { get; set; } = null!;
        public string? Name { get; set; }
        public int Priority { get; set; }
        public HandlerKind Kind { get; set; }
        public Location Location { get; set; } = null!;
        public AttributeData? Attribute { get; set; }
    }
}