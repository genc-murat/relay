using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Relay.SourceGenerator.Validators
{
    /// <summary>
    /// Validates method parameters for handlers and notifications.
    /// </summary>
    internal static class ParameterValidator
    {
        /// <summary>
        /// Validates parameter order and types for handler or notification handler methods.
        /// </summary>
        public static void ValidateParameterOrder(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol,
            ParameterValidationContext validationContext)
        {
            var parameters = methodSymbol.Parameters;

            // First parameter must be the request/notification
            if (parameters.Length > 0)
            {
                var firstParam = parameters[0];
                if (!validationContext.TypeValidator(firstParam.Type))
                {
                    ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.HandlerInvalidRequestParameter,
                        methodDeclaration.ParameterList.Parameters[0].GetLocation(),
                        firstParam.Type.ToDisplayString(),
                        validationContext.ExpectedTypeDescription);
                }
            }

            // If CancellationToken is present, it should be the last parameter
            var cancellationTokenIndex = -1;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Type.Name == "CancellationToken")
                {
                    cancellationTokenIndex = i;
                    break;
                }
            }

            if (cancellationTokenIndex >= 0 && cancellationTokenIndex != parameters.Length - 1)
            {
                ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.InvalidHandlerSignature,
                    methodDeclaration.ParameterList.Parameters[cancellationTokenIndex].GetLocation(),
                    "handler",
                    methodSymbol.Name,
                    "CancellationToken parameter must be the last parameter");
            }

            // Validate that there are no unexpected parameter types
            for (int i = 1; i < parameters.Length; i++)
            {
                var param = parameters[i];
                if (param.Type.Name == "CancellationToken")
                {
                    continue;
                }

                if (param.RefKind == RefKind.Ref || param.RefKind == RefKind.Out)
                {
                    ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.InvalidHandlerSignature,
                        methodDeclaration.ParameterList.Parameters[i].GetLocation(),
                        "handler",
                        methodSymbol.Name,
                        "Handler methods cannot have ref or out parameters.");
                    continue;
                }

                if (param.IsParams)
                {
                    ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.InvalidHandlerSignature,
                        methodDeclaration.ParameterList.Parameters[i].GetLocation(),
                        "handler",
                        methodSymbol.Name,
                        "Handler methods cannot have params parameters.");
                    continue;
                }

                if (param.IsOptional)
                {
                    ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.InvalidHandlerSignature,
                        methodDeclaration.ParameterList.Parameters[i].GetLocation(),
                        "handler",
                        methodSymbol.Name,
                        "Handler methods cannot have optional parameters.");
                    continue;
                }

                ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.InvalidHandlerSignature,
                    methodDeclaration.ParameterList.Parameters[i].GetLocation(),
                    "handler",
                    methodSymbol.Name,
                    $"Unexpected parameter type '{param.Type.ToDisplayString()}'. Only CancellationToken is allowed as an additional parameter.");
            }

            if (methodSymbol.IsGenericMethod)
            {
                ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.InvalidHandlerSignature,
                    methodDeclaration.Identifier.GetLocation(),
                    "handler",
                    methodSymbol.Name,
                    "Handler methods cannot be generic.");
            }


        }

        /// <summary>
        /// Checks if the method parameters include a CancellationToken.
        /// </summary>
        public static bool HasCancellationTokenParameter(ImmutableArray<IParameterSymbol> parameters)
        {
            return parameters.Any(p => p.Type.Name == "CancellationToken");
        }
    }

    /// <summary>
    /// Context for parameter validation containing type checking delegate and expected type description.
    /// </summary>
    public sealed class ParameterValidationContext
    {
        public Func<ITypeSymbol, bool> TypeValidator { get; }
        public string ExpectedTypeDescription { get; }

        public ParameterValidationContext(Func<ITypeSymbol, bool> typeValidator, string expectedTypeDescription)
        {
            TypeValidator = typeValidator;
            ExpectedTypeDescription = expectedTypeDescription;
        }
    }
}
