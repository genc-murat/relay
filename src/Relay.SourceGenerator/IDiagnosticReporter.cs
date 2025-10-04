using Microsoft.CodeAnalysis;
using System.Collections.Generic;

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
    /// Implementation of IDiagnosticReporter that uses IncrementalGeneratorInitializationContext.
    /// This is a simple implementation that stores diagnostics but doesn't report them immediately.
    /// </summary>
    public class IncrementalDiagnosticReporter : IDiagnosticReporter
    {
        private readonly List<Diagnostic> _diagnostics = new();

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            _diagnostics.Add(diagnostic);
        }

        public IReadOnlyList<Diagnostic> GetDiagnostics() => _diagnostics;
    }

    /// <summary>
    /// Implementation of IDiagnosticReporter for source output context.
    /// </summary>
    public class SourceOutputDiagnosticReporter : IDiagnosticReporter
    {
        private readonly SourceProductionContext _context;

        public SourceOutputDiagnosticReporter(SourceProductionContext context)
        {
            _context = context;
        }

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            _context.ReportDiagnostic(diagnostic);
        }
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