using Microsoft.CodeAnalysis;
using Relay.SourceGenerator.Validation;

namespace Relay.SourceGenerator.Diagnostics;

/// <summary>
/// Service interface for diagnostic operations.
/// Follows the Dependency Inversion Principle by depending on abstractions.
/// </summary>
public interface IDiagnosticService
{
    /// <summary>
    /// Reports a diagnostic message.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to report</param>
    void Report(Diagnostic diagnostic);

    /// <summary>
    /// Reports a validation result as diagnostics.
    /// </summary>
    /// <param name="validationResult">The validation result to report</param>
    void ReportValidationResult(ValidationResult validationResult);

    /// <summary>
    /// Creates a diagnostic from a descriptor and location.
    /// </summary>
    /// <param name="descriptor">The diagnostic descriptor</param>
    /// <param name="location">The location of the diagnostic</param>
    /// <param name="messageArgs">Optional message arguments</param>
    /// <returns>The created diagnostic</returns>
    Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, Location? location, params object[] messageArgs);
}
