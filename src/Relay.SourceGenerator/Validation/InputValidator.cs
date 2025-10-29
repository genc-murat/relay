using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Relay.SourceGenerator.Core;
using Relay.SourceGenerator.Diagnostics;
using Relay.SourceGenerator.Discovery;

namespace Relay.SourceGenerator.Validation
{
    /// <summary>
    /// Validates input data for the source generator to ensure robustness and prevent errors.
    /// Implements comprehensive null checks, empty collection handling, and invalid symbol detection.
    /// </summary>
    public static class InputValidator
    {
        /// <summary>
        /// Validates handler class information for null and invalid symbols.
        /// </summary>
        /// <param name="handlerClass">The handler class to validate</param>
        /// <param name="diagnosticReporter">Reporter for validation errors</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool ValidateHandlerClass(HandlerClassInfo? handlerClass, IDiagnosticReporter diagnosticReporter)
        {
            if (handlerClass == null)
            {
                return false; // Null handler class, skip silently
            }

            if (handlerClass.ClassSymbol == null)
            {
                ReportInvalidSymbol("handler class", handlerClass.ClassDeclaration?.Identifier.Text ?? "unknown", diagnosticReporter);
                return false;
            }

            if (handlerClass.ClassDeclaration == null)
            {
                ReportInvalidSymbol("handler class declaration", handlerClass.ClassSymbol.Name, diagnosticReporter);
                return false;
            }

            if (handlerClass.ImplementedInterfaces == null || handlerClass.ImplementedInterfaces.Count == 0)
            {
                // No interfaces implemented, skip silently (will be filtered out)
                return false;
            }

            // Validate each implemented interface
            foreach (var interfaceInfo in handlerClass.ImplementedInterfaces)
            {
                if (!ValidateHandlerInterface(interfaceInfo, handlerClass.ClassSymbol.Name, diagnosticReporter))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validates handler interface information.
        /// </summary>
        private static bool ValidateHandlerInterface(HandlerInterfaceInfo interfaceInfo, string className, IDiagnosticReporter diagnosticReporter)
        {
            if (interfaceInfo == null)
            {
                return false;
            }

            if (interfaceInfo.InterfaceSymbol == null)
            {
                ReportInvalidSymbol("handler interface", className, diagnosticReporter);
                return false;
            }

            // Validate request type for all handler types
            if (interfaceInfo.RequestType == null)
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.InvalidHandlerSignature,
                    Location.None,
                    "handler",
                    className,
                    "Request type cannot be determined");
                diagnosticReporter.ReportDiagnostic(diagnostic);
                return false;
            }

            // Validate response type for request and stream handlers
            if (interfaceInfo.InterfaceType == HandlerType.Request || interfaceInfo.InterfaceType == HandlerType.Stream)
            {
                // Response type can be null for void handlers, so we don't validate it here
            }

            return true;
        }

        /// <summary>
        /// Validates a collection of handler classes, filtering out invalid ones.
        /// </summary>
        /// <param name="handlerClasses">Collection of handler classes to validate</param>
        /// <param name="diagnosticReporter">Reporter for validation errors</param>
        /// <returns>List of valid handler classes</returns>
        public static List<HandlerClassInfo> ValidateAndFilterHandlerClasses(
            IEnumerable<HandlerClassInfo?> handlerClasses,
            IDiagnosticReporter diagnosticReporter)
        {
            if (handlerClasses == null)
            {
                return new List<HandlerClassInfo>();
            }

            var validHandlers = new List<HandlerClassInfo>();

            foreach (var handlerClass in handlerClasses)
            {
                if (ValidateHandlerClass(handlerClass, diagnosticReporter))
                {
                    validHandlers.Add(handlerClass!);
                }
            }

            return validHandlers;
        }

        /// <summary>
        /// Validates generation options for null and invalid values.
        /// </summary>
        /// <param name="options">Generation options to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool ValidateGenerationOptions(Generators.GenerationOptions? options)
        {
            if (options == null)
            {
                return false;
            }

            // Validate MaxDegreeOfParallelism
            if (options.MaxDegreeOfParallelism < 1 || options.MaxDegreeOfParallelism > 64)
            {
                // Clamp to safe values
                options.MaxDegreeOfParallelism = Math.Max(1, Math.Min(64, options.MaxDegreeOfParallelism));
            }

            return true;
        }

        /// <summary>
        /// Validates a compilation context.
        /// </summary>
        /// <param name="context">Compilation context to validate</param>
        /// <param name="diagnosticReporter">Reporter for validation errors</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool ValidateCompilationContext(RelayCompilationContext? context, IDiagnosticReporter diagnosticReporter)
        {
            if (context == null)
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.GeneratorError,
                    Location.None,
                    "Compilation context is null");
                diagnosticReporter.ReportDiagnostic(diagnostic);
                return false;
            }

            if (context.Compilation == null)
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.GeneratorError,
                    Location.None,
                    "Compilation is null");
                diagnosticReporter.ReportDiagnostic(diagnostic);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates a method symbol for handler discovery.
        /// </summary>
        /// <param name="methodSymbol">Method symbol to validate</param>
        /// <param name="methodName">Name of the method for error reporting</param>
        /// <param name="diagnosticReporter">Reporter for validation errors</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool ValidateMethodSymbol(IMethodSymbol? methodSymbol, string methodName, IDiagnosticReporter diagnosticReporter)
        {
            if (methodSymbol == null)
            {
                ReportInvalidSymbol("method", methodName, diagnosticReporter);
                return false;
            }

            if (methodSymbol.ContainingType == null)
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.GeneratorError,
                    Location.None,
                    $"Method '{methodName}' has no containing type");
                diagnosticReporter.ReportDiagnostic(diagnostic);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates a type symbol.
        /// </summary>
        /// <param name="typeSymbol">Type symbol to validate</param>
        /// <param name="typeName">Name of the type for error reporting</param>
        /// <param name="diagnosticReporter">Reporter for validation errors</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool ValidateTypeSymbol(ITypeSymbol? typeSymbol, string typeName, IDiagnosticReporter diagnosticReporter)
        {
            if (typeSymbol == null)
            {
                ReportInvalidSymbol("type", typeName, diagnosticReporter);
                return false;
            }

            if (typeSymbol.TypeKind == TypeKind.Error)
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.GeneratorError,
                    Location.None,
                    $"Type '{typeName}' has errors and cannot be analyzed");
                diagnosticReporter.ReportDiagnostic(diagnostic);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a collection is null or empty.
        /// </summary>
        /// <typeparam name="T">Type of collection elements</typeparam>
        /// <param name="collection">Collection to check</param>
        /// <returns>True if null or empty, false otherwise</returns>
        public static bool IsNullOrEmpty<T>(IEnumerable<T>? collection)
        {
            return collection == null || !collection.Any();
        }

        /// <summary>
        /// Reports an invalid symbol error.
        /// </summary>
        private static void ReportInvalidSymbol(string symbolType, string symbolName, IDiagnosticReporter diagnosticReporter)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.GeneratorError,
                Location.None,
                $"Invalid {symbolType} symbol for '{symbolName}'");
            diagnosticReporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Validates that a string is not null or whitespace.
        /// </summary>
        /// <param name="value">String value to validate</param>
        /// <param name="parameterName">Parameter name for error reporting</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool ValidateString(string? value, string parameterName)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Safely gets a count from a collection, returning 0 if null.
        /// </summary>
        /// <typeparam name="T">Type of collection elements</typeparam>
        /// <param name="collection">Collection to count</param>
        /// <returns>Count of elements, or 0 if null</returns>
        public static int SafeCount<T>(IEnumerable<T>? collection)
        {
            return collection?.Count() ?? 0;
        }

        /// <summary>
        /// Validates handler discovery result.
        /// </summary>
        /// <param name="result">Discovery result to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool ValidateDiscoveryResult(HandlerDiscoveryResult? result)
        {
            if (result == null)
            {
                return false;
            }

            // Result is valid even if empty - it just means no handlers were found
            return true;
        }
    }
}
