using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Information about a handler class and its implemented interfaces.
    /// </summary>
    public class HandlerClassInfo
    {
        /// <summary>
        /// Gets or sets the class declaration syntax node.
        /// </summary>
        public ClassDeclarationSyntax ClassDeclaration { get; set; } = null!;

        /// <summary>
        /// Gets or sets the semantic symbol for the handler class.
        /// </summary>
        public INamedTypeSymbol ClassSymbol { get; set; } = null!;

        /// <summary>
        /// Gets the list of handler interfaces implemented by this class.
        /// </summary>
        public List<HandlerInterfaceInfo> ImplementedInterfaces { get; set; } = new();
    }
}