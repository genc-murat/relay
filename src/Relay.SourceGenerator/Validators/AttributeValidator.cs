using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Relay.SourceGenerator.Validators
{
    /// <summary>
    /// Validates Relay attribute parameters.
    /// </summary>
    internal static class AttributeValidator
    {
        /// <summary>
        /// Validates Handle attribute parameters.
        /// </summary>
        public static void ValidateHandleAttributeParameters(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            AttributeData handleAttribute)
        {
            // Validate Priority parameter if present
            var priorityArg = handleAttribute.NamedArguments
                .FirstOrDefault(arg => arg.Key == "Priority");

            if (priorityArg.Key != null && priorityArg.Value.Value is not int)
            {
                ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.InvalidPriorityValue,
                    methodDeclaration.AttributeLists.First().GetLocation(),
                    priorityArg.Value.Value?.ToString() ?? "null");
            }
        }

        /// <summary>
        /// Validates Notification attribute parameters.
        /// </summary>
        public static void ValidateNotificationAttributeParameters(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            AttributeData notificationAttribute)
        {
            // Validate Priority parameter if present
            var priorityArg = notificationAttribute.NamedArguments
                .FirstOrDefault(arg => arg.Key == "Priority");

            if (priorityArg.Key != null && priorityArg.Value.Value is not int)
            {
                ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.InvalidPriorityValue,
                    methodDeclaration.AttributeLists.First().GetLocation(),
                    priorityArg.Value.Value?.ToString() ?? "null");
            }
        }

        /// <summary>
        /// Validates Pipeline attribute parameters.
        /// </summary>
        public static void ValidatePipelineAttributeParameters(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            AttributeData pipelineAttribute)
        {
            // Validate Order parameter if present
            var orderArg = pipelineAttribute.NamedArguments
                .FirstOrDefault(arg => arg.Key == "Order");

            if (orderArg.Key != null && orderArg.Value.Value is not int)
            {
                ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.InvalidPriorityValue,
                    methodDeclaration.AttributeLists.First().GetLocation(),
                    orderArg.Value.Value?.ToString() ?? "null");
            }

            // Validate Scope parameter if present
            var scopeArg = pipelineAttribute.NamedArguments
                .FirstOrDefault(arg => arg.Key == "Scope");

            if (scopeArg.Key != null && scopeArg.Value.Value is int scopeValue)
            {
                // Validate scope value is within PipelineScope enum range (0-3)
                if (scopeValue < 0 || scopeValue > 3)
                {
                    ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.InvalidPipelineScope,
                        methodDeclaration.AttributeLists.First().GetLocation(),
                        scopeValue.ToString(),
                        methodDeclaration.Identifier.Text,
                        "All, Requests, Streams, or Notifications");
                }
            }
        }
    }
}
