using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Interface for reporting diagnostics in the modern incremental source generator.
    /// </summary>
    public interface IDiagnosticReporter
    {
        /// <summary>
        /// Reports a diagnostic message.
        /// </summary>
        /// <param name="diagnostic">The diagnostic to report.</param>
        void ReportDiagnostic(Diagnostic diagnostic);
    }

    /// <summary>
    /// Alias for SourceOutputDiagnosticReporter to make the naming more explicit.
    /// This class wraps SourceProductionContext for diagnostic reporting.
    /// </summary>
    public class SourceProductionContextDiagnosticReporter : SourceOutputDiagnosticReporter
    {
        public SourceProductionContextDiagnosticReporter(SourceProductionContext context)
            : base(context)
        {
        }
    }
}