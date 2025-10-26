using Microsoft.CodeAnalysis;
using Relay.SourceGenerator.Diagnostics;

namespace Relay.SourceGenerator
{
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
}