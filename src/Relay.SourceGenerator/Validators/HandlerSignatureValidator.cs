using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Relay.SourceGenerator.Validators
{
    /// <summary>
    /// Validates handler method signatures for proper async patterns and parameter types.
    /// </summary>
    internal static class HandlerSignatureValidator
    {
        /// <summary>
        /// Validates a handler method signature and configuration.
        /// </summary>
        public static void ValidateHandlerMethod(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol,
            AttributeData handleAttribute)
        {
            // Validate method signature
            ValidateHandlerSignature(context, methodDeclaration, methodSymbol);

            // Validate attribute parameters
            AttributeValidator.ValidateHandleAttributeParameters(context, methodDeclaration, handleAttribute);
        }

        /// <summary>
        /// Validates handler method signatures for proper async patterns and parameter types.
        /// </summary>
        public static void ValidateHandlerSignature(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol)
        {
            var parameters = methodSymbol.Parameters;

            // Handler must have at least one parameter (the request)
            if (parameters.Length == 0)
            {
                ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.HandlerMissingRequestParameter,
                    methodDeclaration.Identifier.GetLocation(), methodSymbol.Name);
                return;
            }

            // First parameter should be the request type
            var requestParameter = parameters[0];
            var requestType = requestParameter.Type;

            // Validate that the request parameter implements IRequest or IRequest<T>
            if (!TypeValidator.IsValidRequestType(requestType))
            {
                ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.HandlerInvalidRequestParameter,
                    methodDeclaration.ParameterList.Parameters[0].GetLocation(),
                    requestType.ToDisplayString(),
                    "IRequest or IRequest<TResponse>");
                return;
            }

            // Validate return type matches request interface
            ValidateHandlerReturnType(context, methodDeclaration, methodSymbol, requestType);

            // Validate parameter order and types
            ValidateHandlerParameterOrder(context, methodDeclaration, methodSymbol);

            // Check for CancellationToken parameter (warning if missing)
            if (!ParameterValidator.HasCancellationTokenParameter(parameters))
            {
                ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.HandlerMissingCancellationToken,
                    methodDeclaration.Identifier.GetLocation(), methodSymbol.Name);
            }

            // Validate async signature patterns
            ValidateAsyncSignaturePattern(context, methodDeclaration, methodSymbol);
        }

        /// <summary>
        /// Validates handler return types match the request interface expectations.
        /// </summary>
        private static void ValidateHandlerReturnType(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol,
            ITypeSymbol requestType)
        {
            var returnType = methodSymbol.ReturnType;
            var requestInterfaces = requestType.AllInterfaces;

            // Check if request implements IStreamRequest<TResponse>
            var streamRequestInterface = requestInterfaces
                .FirstOrDefault(i => i.Name == "IStreamRequest" && i.TypeArguments.Length == 1);

            if (streamRequestInterface != null)
            {
                // Stream request - validate return type is IAsyncEnumerable<TResponse>
                var expectedResponseType = streamRequestInterface.TypeArguments[0];
                if (!TypeValidator.IsValidStreamHandlerReturnType(returnType, expectedResponseType))
                {
                    ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.InvalidStreamHandlerReturnType,
                        methodDeclaration.ReturnType.GetLocation(),
                        returnType.ToDisplayString(),
                        expectedResponseType.ToDisplayString());
                }
                return;
            }

            // Check if request implements IRequest<TResponse>
            var genericRequestInterface = requestInterfaces
                .FirstOrDefault(i => i.Name == "IRequest" && i.TypeArguments.Length == 1);

            if (genericRequestInterface != null)
            {
                // Request returns a response - validate return type
                var expectedResponseType = genericRequestInterface.TypeArguments[0];
                if (!TypeValidator.IsValidHandlerReturnType(returnType, expectedResponseType))
                {
                    ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.InvalidHandlerReturnType,
                        methodDeclaration.ReturnType.GetLocation(),
                        returnType.ToDisplayString(),
                        expectedResponseType.ToDisplayString());
                }
            }
            else
            {
                // Check if request implements IRequest (void)
                var voidRequestInterface = requestInterfaces
                    .FirstOrDefault(i => i.Name == "IRequest" && i.TypeArguments.Length == 0);

                if (voidRequestInterface != null)
                {
                    // Request doesn't return a response - should return Task or ValueTask
                    if (!TypeValidator.IsValidVoidHandlerReturnType(returnType))
                    {
                        ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.InvalidHandlerReturnType,
                            methodDeclaration.ReturnType.GetLocation(),
                            returnType.ToDisplayString(),
                            "Task or ValueTask");
                    }
                }
            }
        }

        /// <summary>
        /// Validates the parameter order and types for handler methods.
        /// </summary>
        private static void ValidateHandlerParameterOrder(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol)
        {
            var validationContext = new ParameterValidationContext(
                TypeValidator.IsValidRequestType,
                "IRequest or IRequest<TResponse>"
            );

            ParameterValidator.ValidateParameterOrder(context, methodDeclaration, methodSymbol, validationContext);
        }

        /// <summary>
        /// Validates async signature patterns for handler methods.
        /// </summary>
        private static void ValidateAsyncSignaturePattern(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol)
        {
            var returnType = methodSymbol.ReturnType;

            // Check if method is marked as async but returns wrong type
            var isAsync = methodDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword));

            if (isAsync)
            {
                // Async methods should return Task or ValueTask (with or without generic parameter)
                if (returnType.Name != "Task" && returnType.Name != "ValueTask")
                {
                    ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.InvalidHandlerSignature,
                        methodDeclaration.ReturnType.GetLocation(),
                        methodSymbol.Name,
                        "Async handler methods must return Task, Task<T>, ValueTask, or ValueTask<T>");
                }
            }
            else
            {
                // Non-async methods should still return Task/ValueTask for proper async handling
                if (returnType.Name != "Task" && returnType.Name != "ValueTask")
                {
                    ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.PerformanceWarning,
                        methodDeclaration.ReturnType.GetLocation(),
                        methodSymbol.Name,
                        "Handler methods should return Task or ValueTask for optimal async performance");
                }
            }
        }
    }
}
