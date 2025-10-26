using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator.Diagnostics;

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