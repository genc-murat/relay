using System;
using System.Collections.Generic;
using System.Linq;
using Relay.Core.ContractValidation.Models;

namespace Relay.Core.ContractValidation.ErrorReporting;

/// <summary>
/// Default implementation of IValidationErrorReporter.
/// </summary>
public class DefaultValidationErrorReporter : IValidationErrorReporter
{
    /// <inheritdoc />
    public ValidationResult FormatErrors(IEnumerable<ValidationError> errors, ValidationContext context)
    {
        if (errors == null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var errorList = errors.ToList();

        // Enhance errors with suggested fixes if they don't already have them
        var enhancedErrors = new List<ValidationError>();
        foreach (var error in errorList)
        {
            if (!error.SuggestedFixes.Any())
            {
                var suggestedFixes = GenerateSuggestedFixes(error);
                if (suggestedFixes.Any())
                {
                    // Create a new error with suggested fixes
                    var enhancedError = new ValidationError
                    {
                        ErrorCode = error.ErrorCode,
                        Message = error.Message,
                        JsonPath = error.JsonPath,
                        ExpectedValue = error.ExpectedValue,
                        ActualValue = error.ActualValue,
                        SchemaConstraint = error.SchemaConstraint,
                        Severity = error.Severity,
                        SuggestedFixes = suggestedFixes
                    };
                    enhancedErrors.Add(enhancedError);
                }
                else
                {
                    enhancedErrors.Add(error);
                }
            }
            else
            {
                enhancedErrors.Add(error);
            }
        }

        return new ValidationResult
        {
            IsValid = false,
            Errors = enhancedErrors,
            ValidatorName = "DefaultContractValidator"
        };
    }

    /// <inheritdoc />
    public List<string> GenerateSuggestedFixes(ValidationError error)
    {
        if (error == null)
        {
            return new List<string>();
        }

        var suggestions = new List<string>();

        // Generate suggestions based on error code
        switch (error.ErrorCode)
        {
            case ValidationErrorCodes.RequiredPropertyMissing:
                suggestions.Add($"Add the required property '{error.JsonPath}' to the object");
                if (error.ExpectedValue != null)
                {
                    suggestions.Add($"Set the property value to match the expected type: {error.ExpectedValue}");
                }
                break;

            case ValidationErrorCodes.TypeMismatch:
                if (error.ExpectedValue != null && error.ActualValue != null)
                {
                    suggestions.Add($"Change the type from '{error.ActualValue.GetType().Name}' to '{error.ExpectedValue}'");
                }
                else if (error.ExpectedValue != null)
                {
                    suggestions.Add($"Ensure the property type matches the expected type: {error.ExpectedValue}");
                }
                break;

            case ValidationErrorCodes.ConstraintViolation:
                if (error.SchemaConstraint != null)
                {
                    suggestions.Add($"Ensure the value satisfies the constraint: {error.SchemaConstraint}");
                }
                if (error.ExpectedValue != null)
                {
                    suggestions.Add($"Expected value: {error.ExpectedValue}");
                }
                break;

            case ValidationErrorCodes.SchemaNotFound:
                suggestions.Add("Ensure the schema file exists in the configured schema directories");
                suggestions.Add("Check the schema naming convention matches the type name");
                suggestions.Add("Verify the schema is embedded as a resource if using embedded schemas");
                break;

            case ValidationErrorCodes.SchemaParsingFailed:
                suggestions.Add("Validate the schema JSON syntax");
                suggestions.Add("Ensure the schema follows the JSON Schema specification");
                suggestions.Add("Check for missing required schema properties");
                break;

            case ValidationErrorCodes.ValidationTimeout:
                suggestions.Add("Increase the validation timeout in configuration");
                suggestions.Add("Simplify the schema to reduce validation complexity");
                suggestions.Add("Check for circular references in the schema");
                break;

            default:
                // Generic suggestions for unknown error codes
                if (!string.IsNullOrEmpty(error.JsonPath))
                {
                    suggestions.Add($"Review the property at path '{error.JsonPath}'");
                }
                if (error.SchemaConstraint != null)
                {
                    suggestions.Add($"Check the schema constraint: {error.SchemaConstraint}");
                }
                break;
        }

        return suggestions;
    }
}
