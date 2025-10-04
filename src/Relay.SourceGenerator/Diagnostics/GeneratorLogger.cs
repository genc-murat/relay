using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Modern logger for the Relay incremental source generator.
    /// </summary>
    internal static class GeneratorLogger
    {
        /// <summary>
        /// Logs debug information via diagnostic reporter.
        /// </summary>
        public static void LogDebug(IDiagnosticReporter reporter, string message)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.Debug,
                Location.None,
                message);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Logs informational messages.
        /// </summary>
        public static void LogInfo(IDiagnosticReporter reporter, string message)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.Info,
                Location.None,
                message);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Logs warning messages.
        /// </summary>
        public static void LogWarning(IDiagnosticReporter reporter, string message)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.Info, // Use Info instead of Warning for now
                Location.None,
                message);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Logs error messages.
        /// </summary>
        public static void LogError(IDiagnosticReporter reporter, string message)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.GeneratorError,
                Location.None,
                message);
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Logs generator performance metrics.
        /// </summary>
        public static void LogPerformance(IDiagnosticReporter reporter, string operation, long elapsedMs)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.Debug,
                Location.None,
                $"Generator performance: {operation} took {elapsedMs}ms");
            reporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports a specific diagnostic.
        /// </summary>
        public static void ReportDiagnostic(IDiagnosticReporter reporter, DiagnosticDescriptor descriptor, Location? location, params object[] messageArgs)
        {
            var diagnostic = Diagnostic.Create(
                descriptor,
                location ?? Location.None,
                messageArgs);
            reporter.ReportDiagnostic(diagnostic);
        }
    }
}