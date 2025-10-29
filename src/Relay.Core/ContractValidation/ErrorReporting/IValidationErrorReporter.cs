using System.Collections.Generic;
using Relay.Core.ContractValidation.Models;

namespace Relay.Core.ContractValidation.ErrorReporting;

/// <summary>
/// Interface for formatting and reporting validation errors.
/// </summary>
public interface IValidationErrorReporter
{
    /// <summary>
    /// Formats validation errors into a structured result.
    /// </summary>
    /// <param name="errors">The validation errors to format.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>A formatted validation result.</returns>
    ValidationResult FormatErrors(IEnumerable<ValidationError> errors, ValidationContext context);

    /// <summary>
    /// Generates suggested fixes for common validation errors.
    /// </summary>
    /// <param name="error">The validation error.</param>
    /// <returns>A list of suggested fixes.</returns>
    List<string> GenerateSuggestedFixes(ValidationError error);
}
