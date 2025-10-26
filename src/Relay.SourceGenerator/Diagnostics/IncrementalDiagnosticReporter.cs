using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Relay.SourceGenerator.Diagnostics;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Implementation of IDiagnosticReporter that uses IncrementalGeneratorInitializationContext.
    /// This is a simple implementation that stores diagnostics but doesn't report them immediately.
    /// </summary>
    public class IncrementalDiagnosticReporter : IDiagnosticReporter
    {
        private readonly ConcurrentBag<Diagnostic> _diagnostics = new();

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            _diagnostics.Add(diagnostic);
        }

        public IReadOnlyList<Diagnostic> GetDiagnostics() => _diagnostics.ToList();
    }
}