using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Syntax receiver for the Relay source generator that collects candidate methods.
    /// This is used by the legacy generator pattern for compatibility with tests.
    /// </summary>
    public class RelaySyntaxReceiver : ISyntaxReceiver
    {
        /// <summary>
        /// Gets the list of candidate methods that might be Relay handlers.
        /// </summary>
        public List<MethodDeclarationSyntax> CandidateMethods { get; } = new List<MethodDeclarationSyntax>();

        /// <summary>
        /// Visits each syntax node and collects candidate handler methods.
        /// </summary>
        /// <param name="syntaxNode">The syntax node to visit.</param>
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // Look for method declarations that might be Relay handlers
            if (syntaxNode is MethodDeclarationSyntax methodDeclaration)
            {
                // Check if method has any attributes that might be Relay attributes
                if (HasRelayAttribute(methodDeclaration))
                {
                    CandidateMethods.Add(methodDeclaration);
                }
            }
        }

        /// <summary>
        /// Checks if a method declaration has any Relay-related attributes.
        /// </summary>
        /// <param name="method">The method declaration to check.</param>
        /// <returns>True if the method has Relay attributes, false otherwise.</returns>
        private static bool HasRelayAttribute(MethodDeclarationSyntax method)
        {
            return method.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(attr => IsRelayAttributeName(attr.Name.ToString()));
        }

        /// <summary>
        /// Checks if an attribute name is a known Relay attribute.
        /// </summary>
        /// <param name="attributeName">The attribute name to check.</param>
        /// <returns>True if it's a Relay attribute, false otherwise.</returns>
        private static bool IsRelayAttributeName(string attributeName)
        {
            // Remove the "Attribute" suffix if present
            var name = attributeName.EndsWith("Attribute") 
                ? attributeName.Substring(0, attributeName.Length - 9) 
                : attributeName;

            return name switch
            {
                // Core Relay attributes
                "Handle" => true,
                "Notification" => true,
                "Pipeline" => true,
                "Endpoint" => true,
                "ExposeAsEndpoint" => true,
                
                // Handler type attributes
                "RequestHandler" => true,
                "NotificationHandler" => true,
                "PipelineHandler" => true,
                "EndpointHandler" => true,
                "RelayHandler" => true,
                
                // CQRS attributes
                "Command" => true,
                "Query" => true,
                "Event" => true,
                
                _ => false
            };
        }

        /// <summary>
        /// Gets the total number of candidate methods found.
        /// </summary>
        public int CandidateCount => CandidateMethods.Count;

        /// <summary>
        /// Clears all collected candidate methods.
        /// </summary>
        public void Clear()
        {
            CandidateMethods.Clear();
        }

        /// <summary>
        /// Gets candidate methods filtered by attribute type.
        /// </summary>
        /// <param name="attributeName">The attribute name to filter by.</param>
        /// <returns>Methods that have the specified attribute.</returns>
        public IEnumerable<MethodDeclarationSyntax> GetCandidatesByAttribute(string attributeName)
        {
            return CandidateMethods.Where(method =>
                method.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Any(attr => IsAttributeMatch(attr.Name.ToString(), attributeName)));
        }

        /// <summary>
        /// Checks if an attribute matches the specified name.
        /// </summary>
        /// <param name="attributeName">The actual attribute name.</param>
        /// <param name="targetName">The target name to match.</param>
        /// <returns>True if they match, false otherwise.</returns>
        private static bool IsAttributeMatch(string attributeName, string targetName)
        {
            var cleanName = attributeName.EndsWith("Attribute")
                ? attributeName.Substring(0, attributeName.Length - 9)
                : attributeName;

            return cleanName.Equals(targetName, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}