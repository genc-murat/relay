using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Relay.SourceGenerator
{
    // Supporting classes
    public class HandlerClassInfo
    {
        public ClassDeclarationSyntax? ClassDeclaration { get; set; }
        public INamedTypeSymbol? ClassSymbol { get; set; }
        public List<HandlerInterfaceInfo> ImplementedInterfaces { get; set; } = new();
    }
}