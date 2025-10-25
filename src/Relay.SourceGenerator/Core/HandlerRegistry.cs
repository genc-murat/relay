using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Registry for tracking handlers during compilation analysis.
    /// </summary>
    public class HandlerRegistry
    {
        public List<AnalyzerHandlerInfo> Handlers { get; } = new();

        public void AddHandler(IMethodSymbol methodSymbol, AttributeData handleAttribute, MethodDeclarationSyntax methodDeclaration)
        {
            var requestType = methodSymbol.Parameters.FirstOrDefault()?.Type;
            if (requestType == null) return;

            var nameArg = handleAttribute.NamedArguments
                .FirstOrDefault(arg => arg.Key == "Name");

            var priorityArg = handleAttribute.NamedArguments
                .FirstOrDefault(arg => arg.Key == "Priority");

            var name = nameArg.Key != null ? nameArg.Value.Value?.ToString() : null;
            var priority = priorityArg.Key != null && priorityArg.Value.Value is int p ? p : 0;

            Handlers.Add(new AnalyzerHandlerInfo
            {
                MethodSymbol = methodSymbol,
                MethodName = methodSymbol.Name,
                RequestType = requestType,
                Name = name,
                Priority = priority,
                Location = methodDeclaration.Identifier.GetLocation(),
                Attribute = handleAttribute
            });
        }
    }
}