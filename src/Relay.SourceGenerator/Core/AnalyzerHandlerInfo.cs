using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Information about a discovered handler method for analyzer purposes.
    /// </summary>
    public class AnalyzerHandlerInfo
    {
        public IMethodSymbol MethodSymbol { get; set; } = null!;
        public string MethodName { get; set; } = string.Empty;
        public ITypeSymbol RequestType { get; set; } = null!;
        public string? Name { get; set; }
        public int Priority { get; set; }
        public Location Location { get; set; } = null!;
        public AttributeData? Attribute { get; set; }
    }
}