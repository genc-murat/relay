using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Information about a discovered handler.
    /// </summary>
    public class HandlerInfo
    {
        public Type? HandlerType { get; set; }
        public Type? RequestType { get; set; }
        public Type? ResponseType { get; set; }
        public string? MethodName { get; set; }
        public string? HandlerName { get; set; }
        public int Priority { get; set; }
        public bool IsAsync { get; set; }
        public bool HasCancellationToken { get; set; }
        public string? FullTypeName { get; set; }
        public string? Namespace { get; set; }

        // Additional properties for compatibility with old HandlerInfo
        public MethodDeclarationSyntax? Method { get; set; }
        public IMethodSymbol? MethodSymbol { get; set; }
        public List<RelayAttributeInfo> Attributes { get; set; } = new();
        public ITypeSymbol? RequestTypeSymbol { get; set; }
        public ITypeSymbol? ResponseTypeSymbol { get; set; }
        public ITypeSymbol? HandlerTypeSymbol { get; set; }
    }
}