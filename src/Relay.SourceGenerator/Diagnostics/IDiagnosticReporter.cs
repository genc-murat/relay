using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator.Diagnostics;

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