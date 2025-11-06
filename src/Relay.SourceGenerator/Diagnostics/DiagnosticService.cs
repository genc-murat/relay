using System;
using Microsoft.CodeAnalysis;
using Relay.SourceGenerator.Validation;

namespace Relay.SourceGenerator.Diagnostics;

/// <summary>
/// Default implementation of <see cref="IDiagnosticService"/>.
/// </summary>
public class DiagnosticService : IDiagnosticService
{
    private readonly IDiagnosticReporter _reporter;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticService"/> class.
    /// </summary>
    /// <param name="reporter">The diagnostic reporter</param>
    public DiagnosticService(IDiagnosticReporter reporter)
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
    }

    /// <inheritdoc/>
    public void Report(Diagnostic diagnostic)
    {
        if (diagnostic == null) throw new ArgumentNullException(nameof(diagnostic));
        _reporter.ReportDiagnostic(diagnostic);
    }

    /// <inheritdoc/>
    public void ReportValidationResult(ValidationResult validationResult)
    {
        if (validationResult == null) throw new ArgumentNullException(nameof(validationResult));

        // Report all errors
        foreach (var error in validationResult.Errors)
        {
            var diagnostic = error.Descriptor != null && error.Location != null
                ? Diagnostic.Create(error.Descriptor, error.Location, error.Message)
                : Diagnostic.Create(
                    DiagnosticDescriptors.GeneratorError,
                    error.Location ?? Location.None,
                    error.Message);

            _reporter.ReportDiagnostic(diagnostic);
        }

        // Report all warnings
        foreach (var warning in validationResult.Warnings)
        {
            var diagnostic = warning.Descriptor != null && warning.Location != null
                ? Diagnostic.Create(warning.Descriptor, warning.Location, warning.Message)
                : Diagnostic.Create(
                    DiagnosticDescriptors.PerformanceWarning,
                    warning.Location ?? Location.None,
                    "Performance",
                    warning.Message);

            _reporter.ReportDiagnostic(diagnostic);
        }
    }

    /// <inheritdoc/>
    public Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, Location? location, params object[] messageArgs)
    {
        if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));
        return Diagnostic.Create(descriptor, location ?? Location.None, messageArgs);
    }
}
