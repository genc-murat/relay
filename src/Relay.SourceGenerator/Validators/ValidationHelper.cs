using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Relay.SourceGenerator.Validators
{
    /// <summary>
    /// Helper methods for validation and diagnostic reporting.
    /// </summary>
    internal static class ValidationHelper
    {
        /// <summary>
        /// Reports a diagnostic to the syntax node analysis context.
        /// </summary>
        public static void ReportDiagnostic(
            SyntaxNodeAnalysisContext context,
            DiagnosticDescriptor descriptor,
            Location location,
            params object[] messageArgs)
        {
            var diagnostic = Diagnostic.Create(descriptor, location, messageArgs);
            context.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports a diagnostic to the compilation analysis context.
        /// </summary>
        public static void ReportDiagnostic(
            CompilationAnalysisContext context,
            DiagnosticDescriptor descriptor,
            Location location,
            params object[] messageArgs)
        {
            var diagnostic = Diagnostic.Create(descriptor, location, messageArgs);
            context.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Safely gets the declared symbol for a method declaration.
        /// </summary>
        public static IMethodSymbol? TryGetDeclaredSymbol(
            SemanticModel semanticModel,
            Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax methodDeclaration)
        {
            try
            {
                return semanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol;
            }
            catch (Exception)
            {
                // Symbol resolution can fail in incomplete or malformed code
                return null;
            }
        }

        /// <summary>
        /// Safely gets the semantic model for a syntax tree.
        /// </summary>
        public static SemanticModel? TryGetSemanticModel(
            Compilation compilation,
            SyntaxTree syntaxTree)
        {
            try
            {
                return compilation.GetSemanticModel(syntaxTree);
            }
            catch (Exception)
            {
                // Semantic model creation can fail for invalid syntax trees
                return null;
            }
        }

        /// <summary>
        /// Gets an attribute from a method symbol by its full name.
        /// </summary>
        public static AttributeData? GetAttribute(IMethodSymbol methodSymbol, string attributeFullName)
        {
            try
            {
                var attributes = methodSymbol.GetAttributes();
                foreach (var attr in attributes)
                {
                    if (attr.AttributeClass?.ToDisplayString() == attributeFullName)
                        return attr;
                }
                return null;
            }
            catch (Exception)
            {
                // Symbol resolution can fail in incomplete or malformed code
                // Return null to gracefully handle the error
                return null;
            }
        }
    }
}
