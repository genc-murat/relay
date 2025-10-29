using Microsoft.CodeAnalysis;
using Relay.SourceGenerator.Diagnostics;

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
        public static void ReportInvalidSignature(this IDiagnosticReporter reporter, Location location, string handlerType, string methodName, string issue)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.InvalidHandlerSignature,
                location,
                handlerType,
                methodName,
                issue);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports an invalid handler signature error (legacy overload for backward compatibility).
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

        /// <summary>
        /// Reports a missing Relay.Core reference error.
        /// </summary>
        public static void ReportMissingReference(this IDiagnosticReporter reporter, Location? location = null)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.MissingRelayCoreReference,
                location ?? Location.None);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports an invalid configuration value error.
        /// </summary>
        public static void ReportInvalidConfigurationValue(this IDiagnosticReporter reporter, Location location, string propertyName, string value, string reason)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.InvalidConfigurationValue,
                location,
                propertyName,
                value,
                reason);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports a missing required attribute error.
        /// </summary>
        public static void ReportMissingRequiredAttribute(this IDiagnosticReporter reporter, Location location, string methodName, string attributeName)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.MissingRequiredAttribute,
                location,
                methodName,
                attributeName);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports an obsolete handler pattern warning.
        /// </summary>
        public static void ReportObsoleteHandlerPattern(this IDiagnosticReporter reporter, Location location, string handlerName, string obsoletePattern, string recommendedPattern)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.ObsoleteHandlerPattern,
                location,
                handlerName,
                obsoletePattern,
                recommendedPattern);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports a performance bottleneck warning.
        /// </summary>
        public static void ReportPerformanceBottleneck(this IDiagnosticReporter reporter, Location location, string handlerName, string issue, string suggestion)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.PerformanceBottleneck,
                location,
                handlerName,
                issue,
                suggestion);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports a diagnostic with severity configuration applied.
        /// </summary>
        /// <param name="reporter">The diagnostic reporter</param>
        /// <param name="descriptor">The diagnostic descriptor</param>
        /// <param name="location">The location of the diagnostic</param>
        /// <param name="configuration">The diagnostic severity configuration (optional)</param>
        /// <param name="messageArgs">Message format arguments</param>
        public static void ReportConfiguredDiagnostic(
            this IDiagnosticReporter reporter,
            DiagnosticDescriptor descriptor,
            Location location,
            DiagnosticSeverityConfiguration? configuration,
            params object[] messageArgs)
        {
            if (configuration != null)
            {
                // Check if diagnostic is suppressed
                if (configuration.IsSuppressed(descriptor.Id))
                {
                    return;
                }

                // Apply severity configuration
                descriptor = configuration.ApplyConfiguration(descriptor);
            }

            var diagnostic = Diagnostic.Create(descriptor, location, messageArgs);
            reporter.ReportDiagnostic(diagnostic);
        }
    }
}