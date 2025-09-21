using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Extension methods for IDiagnosticReporter to simplify reporting specific diagnostics.
    /// </summary>
    internal static class DiagnosticReporterExtensions
    {
        /// <summary>
        /// Reports a duplicate handler error.
        /// </summary>
        public static void ReportDuplicateHandler(this IDiagnosticReporter reporter, Location location, string requestType, string? responseType)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.DuplicateHandler,
                location,
                requestType,
                responseType ?? "void");
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports a duplicate named handler error.
        /// </summary>
        public static void ReportDuplicateNamedHandler(this IDiagnosticReporter reporter, Location location, string requestType, string handlerName)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.NamedHandlerConflict,
                location,
                handlerName,
                requestType);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports a duplicate pipeline order error.
        /// </summary>
        public static void ReportDuplicatePipelineOrder(this IDiagnosticReporter reporter, Location location, int order, string scope)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.DuplicatePipelineOrder,
                location,
                order,
                scope);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports an invalid handler return type error.
        /// </summary>
        public static void ReportInvalidHandlerReturnType(this IDiagnosticReporter reporter, Location location, string actualType, string expectedType)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.InvalidHandlerReturnType,
                location,
                actualType,
                expectedType);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports an invalid stream handler return type error.
        /// </summary>
        public static void ReportInvalidStreamHandlerReturnType(this IDiagnosticReporter reporter, Location location, string actualType, string expectedType)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.InvalidStreamHandlerReturnType,
                location,
                actualType,
                expectedType);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports an invalid notification handler return type error.
        /// </summary>
        public static void ReportInvalidNotificationHandlerReturnType(this IDiagnosticReporter reporter, Location location, string actualType)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.InvalidNotificationHandlerReturnType,
                location,
                actualType);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports a handler missing request parameter error.
        /// </summary>
        public static void ReportHandlerMissingRequestParameter(this IDiagnosticReporter reporter, Location location, string methodName)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.HandlerMissingRequestParameter,
                location,
                methodName);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports a handler invalid request parameter error.
        /// </summary>
        public static void ReportHandlerInvalidRequestParameter(this IDiagnosticReporter reporter, Location location, string actualType, string expectedType)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.HandlerInvalidRequestParameter,
                location,
                actualType,
                expectedType);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports a handler missing cancellation token warning.
        /// </summary>
        public static void ReportHandlerMissingCancellationToken(this IDiagnosticReporter reporter, Location location, string methodName)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.HandlerMissingCancellationToken,
                location,
                methodName);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports a notification handler missing parameter error.
        /// </summary>
        public static void ReportNotificationHandlerMissingParameter(this IDiagnosticReporter reporter, Location location, string methodName)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.NotificationHandlerMissingParameter,
                location,
                methodName);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports an invalid priority value error.
        /// </summary>
        public static void ReportInvalidPriorityValue(this IDiagnosticReporter reporter, Location location, int priority)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.InvalidPriorityValue,
                location,
                priority);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports a no handlers found warning.
        /// </summary>
        public static void ReportNoHandlersFound(this IDiagnosticReporter reporter)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.NoHandlersFound,
                Location.None);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports a configuration conflict error.
        /// </summary>
        public static void ReportConfigurationConflict(this IDiagnosticReporter reporter, Location location, string details)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.ConfigurationConflict,
                location,
                details);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports an invalid pipeline scope error.
        /// </summary>
        public static void ReportInvalidPipelineScope(this IDiagnosticReporter reporter, Location location, string actualScope, string methodName, string expectedScope)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.InvalidPipelineScope,
                location,
                actualScope,
                methodName,
                expectedScope);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports an invalid handler signature error.
        /// </summary>
        public static void ReportInvalidHandlerSignature(this IDiagnosticReporter reporter, Location location, string methodName, string expectedSignature)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.InvalidHandlerSignature,
                location,
                methodName,
                expectedSignature);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports a general error.
        /// </summary>
        public static void ReportError(this IDiagnosticReporter reporter, string message, Location? location = null)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.GeneratorError,
                location ?? Location.None,
                message);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports debug information.
        /// </summary>
        public static void ReportDebug(this IDiagnosticReporter reporter, string message, Location? location = null)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.Debug,
                location ?? Location.None,
                message);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports informational message.
        /// </summary>
        public static void ReportInfo(this IDiagnosticReporter reporter, string message, Location? location = null)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.Info,
                location ?? Location.None,
                message);
            reporter.ReportDiagnostic(diagnostic);
        }
    }
}