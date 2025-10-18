using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Relay.SourceGenerator.Validators
{
    /// <summary>
    /// Validates notification handler method signatures.
    /// </summary>
    internal static class NotificationHandlerValidator
    {
        /// <summary>
        /// Validates a notification handler method signature and configuration.
        /// </summary>
        public static void ValidateNotificationHandlerMethod(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol,
            AttributeData notificationAttribute)
        {
            // Validate method signature for notifications
            ValidateNotificationHandlerSignature(context, methodDeclaration, methodSymbol);

            // Validate attribute parameters
            AttributeValidator.ValidateNotificationAttributeParameters(context, methodDeclaration, notificationAttribute);
        }

        /// <summary>
        /// Validates notification handler method signatures.
        /// </summary>
        public static void ValidateNotificationHandlerSignature(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol)
        {
            var parameters = methodSymbol.Parameters;

            // Notification handler must have at least one parameter (the notification)
            if (parameters.Length == 0)
            {
                ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.NotificationHandlerMissingParameter,
                    methodDeclaration.Identifier.GetLocation(), methodSymbol.Name);
                return;
            }

            // First parameter should be the notification type
            var notificationParameter = parameters[0];
            var notificationType = notificationParameter.Type;

            // Validate that the notification parameter implements INotification
            if (!TypeValidator.IsValidNotificationType(notificationType))
            {
                ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.HandlerInvalidRequestParameter,
                    methodDeclaration.ParameterList.Parameters[0].GetLocation(),
                    notificationType.ToDisplayString(),
                    "INotification");
                return;
            }

            // Validate return type (should be Task or ValueTask)
            var returnType = methodSymbol.ReturnType;
            if (!TypeValidator.IsValidNotificationReturnType(returnType))
            {
                ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.InvalidNotificationHandlerReturnType,
                    methodDeclaration.ReturnType.GetLocation(), returnType.ToDisplayString());
            }

            // Validate parameter order and types
            ValidateNotificationHandlerParameterOrder(context, methodDeclaration, methodSymbol);

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
        /// Validates the parameter order and types for notification handler methods.
        /// </summary>
        private static void ValidateNotificationHandlerParameterOrder(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol)
        {
            var validationContext = new ParameterValidationContext(
                TypeValidator.IsValidNotificationType,
                "INotification"
            );

            ParameterValidator.ValidateParameterOrder(context, methodDeclaration, methodSymbol, validationContext);
        }

        /// <summary>
        /// Validates async signature patterns for notification handler methods.
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
                        "notification handler",
                        methodSymbol.Name,
                        "Async notification handler methods must return Task or ValueTask");
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
                        "Notification handler methods should return Task or ValueTask for optimal async performance");
                }
            }

            if (methodDeclaration.Body == null) return;

            foreach (var awaitExpression in methodDeclaration.Body.DescendantNodes().OfType<AwaitExpressionSyntax>())
            {
                var awaitOperand = awaitExpression.Expression;
                if (awaitOperand is InvocationExpressionSyntax invocation &&
                    invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Name.Identifier.Text == "ConfigureAwait")
                {
                    // Correctly using ConfigureAwait. Do nothing.
                }
                else
                {
                    // Potentially missing ConfigureAwait.
                    // Check if the awaited type is a Task or ValueTask.
                    var awaitedType = context.SemanticModel.GetTypeInfo(awaitOperand).Type;
                    if (awaitedType != null)
                    {
                        var taskSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
                        var valueTaskSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
                        var genericTaskSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
                        var genericValueTaskSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

                        var originalDefinition = awaitedType.OriginalDefinition;

                        if (SymbolEqualityComparer.Default.Equals(originalDefinition, taskSymbol) ||
                            SymbolEqualityComparer.Default.Equals(originalDefinition, valueTaskSymbol) ||
                            SymbolEqualityComparer.Default.Equals(originalDefinition, genericTaskSymbol) ||
                            SymbolEqualityComparer.Default.Equals(originalDefinition, genericValueTaskSymbol))
                        {
                            ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.MissingConfigureAwait,
                                awaitExpression.GetLocation());
                        }
                    }
                }
            }

            foreach (var memberAccess in methodDeclaration.Body.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                if (memberAccess.Name.Identifier.Text == "Result" || memberAccess.Name.Identifier.Text == "Wait")
                {
                    var typeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression);
                    var objectType = typeInfo.Type;
                    if (objectType != null)
                    {
                        var taskSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
                        var valueTaskSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
                        var genericTaskSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
                        var genericValueTaskSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

                        var originalDefinition = objectType.OriginalDefinition;

                        if (SymbolEqualityComparer.Default.Equals(originalDefinition, taskSymbol) ||
                            SymbolEqualityComparer.Default.Equals(originalDefinition, valueTaskSymbol) ||
                            SymbolEqualityComparer.Default.Equals(originalDefinition, genericTaskSymbol) ||
                            SymbolEqualityComparer.Default.Equals(originalDefinition, genericValueTaskSymbol))
                        {
                            ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.SyncOverAsync,
                                memberAccess.Name.GetLocation());
                        }
                    }
                }
            }
        }
    }
}
