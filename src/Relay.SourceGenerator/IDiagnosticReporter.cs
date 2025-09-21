using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Interface for reporting diagnostics during source generation.
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
    /// Implementation of IDiagnosticReporter that uses GeneratorExecutionContext.
    /// </summary>
    public class GeneratorExecutionContextDiagnosticReporter : IDiagnosticReporter
    {
        private readonly GeneratorExecutionContext _context;

        public GeneratorExecutionContextDiagnosticReporter(GeneratorExecutionContext context)
        {
            _context = context;
        }

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            _context.ReportDiagnostic(diagnostic);
        }
    }
}