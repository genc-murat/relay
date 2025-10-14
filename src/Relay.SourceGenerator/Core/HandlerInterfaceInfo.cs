using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator
{
    public class HandlerInterfaceInfo
    {
        public HandlerType InterfaceType { get; set; }
        public INamedTypeSymbol? InterfaceSymbol { get; set; }
        public ITypeSymbol? RequestType { get; set; }
        public ITypeSymbol? ResponseType { get; set; }
    }
}