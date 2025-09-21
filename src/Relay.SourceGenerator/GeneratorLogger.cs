using Microsoft.CodeAnalysis;
using System;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Logger for the Relay source generator that reports diagnostics.
    /// </summary>
    internal static class GeneratorLogger
    {
        /// <summary>
        /// Logs debug information (only shown when debug diagnostics are enabled).
        /// </summary>
        public static void LogDebug(GeneratorExecutionContext context, string message)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.Debug,
                Location.None,
                message));
        }

        /// <summary>
        /// Logs informational messages.
        /// </summary>
        public static void LogInfo(GeneratorExecutionContext context, string message)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.Info,
                Location.None,
                message));
        }

        /// <summary>
        /// Logs error messages.
        /// </summary>
        public static void LogError(GeneratorExecutionContext context, string message, Location? location = null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.GeneratorError,
                location ?? Location.None,
                message));
        }

        /// <summary>
        /// Logs error messages with exception details.
        /// </summary>
        public static void LogError(GeneratorExecutionContext context, Exception exception, Location? location = null)
        {
            var message = $"{exception.Message}\nStack trace: {exception.StackTrace}";
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.GeneratorError,
                location ?? Location.None,
                message));
        }

        /// <summary>
        /// Logs warning messages.
        /// </summary>
        public static void LogWarning(GeneratorExecutionContext context, DiagnosticDescriptor descriptor, Location? location, params object[] messageArgs)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                descriptor,
                location ?? Location.None,
                messageArgs));
        }

        /// <summary>
        /// Reports a specific diagnostic.
        /// </summary>
        public static void ReportDiagnostic(GeneratorExecutionContext context, DiagnosticDescriptor descriptor, Location? location, params object[] messageArgs)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                descriptor,
                location ?? Location.None,
                messageArgs));
        }
    }
}