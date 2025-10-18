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
        }
    }
}
