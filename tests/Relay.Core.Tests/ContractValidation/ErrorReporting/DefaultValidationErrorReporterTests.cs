using Relay.Core.ContractValidation.ErrorReporting;
using Relay.Core.ContractValidation.Models;
using Relay.Core.Metadata.MessageQueue;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.ErrorReporting;

public class DefaultValidationErrorReporterTests
{
    private readonly DefaultValidationErrorReporter _reporter = new();

    [Fact]
    public void FormatErrors_WithNullErrors_ShouldThrowArgumentNullException()
    {
        // Arrange
        var context = ValidationContext.ForRequest(typeof(object), null, null);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _reporter.FormatErrors(null!, context));
    }

    [Fact]
    public void FormatErrors_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var errors = new List<ValidationError>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _reporter.FormatErrors(errors, null!));
    }

    [Fact]
    public void FormatErrors_WithErrors_ShouldReturnValidationResult()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            ValidationError.Create(ValidationErrorCodes.RequiredPropertyMissing, "Property missing", "$.name")
        };
        var context = ValidationContext.ForRequest(typeof(object), null, null);

        // Act
        var result = _reporter.FormatErrors(errors, context);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("DefaultContractValidator", result.ValidatorName);
    }

    [Fact]
    public void FormatErrors_WithErrorsWithoutSuggestedFixes_ShouldAddSuggestions()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new ValidationError
            {
                ErrorCode = ValidationErrorCodes.RequiredPropertyMissing,
                Message = "Property missing",
                JsonPath = "$.email"
            }
        };
        var context = ValidationContext.ForRequest(typeof(object), null, null);

        // Act
        var result = _reporter.FormatErrors(errors, context);

        // Assert
        Assert.Single(result.Errors);
        Assert.NotEmpty(result.Errors[0].SuggestedFixes);
    }

    [Fact]
    public void GenerateSuggestedFixes_WithNullError_ShouldReturnEmptyList()
    {
        // Act
        var suggestions = _reporter.GenerateSuggestedFixes(null!);

        // Assert
        Assert.Empty(suggestions);
    }

    [Fact]
    public void GenerateSuggestedFixes_ForRequiredPropertyMissing_ShouldReturnSuggestions()
    {
        // Arrange
        var error = new ValidationError
        {
            ErrorCode = ValidationErrorCodes.RequiredPropertyMissing,
            JsonPath = "$.user.email",
            ExpectedValue = "string"
        };

        // Act
        var suggestions = _reporter.GenerateSuggestedFixes(error);

        // Assert
        Assert.NotEmpty(suggestions);
        Assert.Contains(suggestions, s => s.Contains("Add the required property"));
        Assert.Contains(suggestions, s => s.Contains("expected type"));
    }

    [Fact]
    public void GenerateSuggestedFixes_ForTypeMismatch_ShouldReturnSuggestions()
    {
        // Arrange
        var error = new ValidationError
        {
            ErrorCode = ValidationErrorCodes.TypeMismatch,
            ExpectedValue = "string",
            ActualValue = 123
        };

        // Act
        var suggestions = _reporter.GenerateSuggestedFixes(error);

        // Assert
        Assert.NotEmpty(suggestions);
        Assert.Contains(suggestions, s => s.Contains("Change the type"));
    }

    [Fact]
    public void GenerateSuggestedFixes_ForConstraintViolation_ShouldReturnSuggestions()
    {
        // Arrange
        var error = new ValidationError
        {
            ErrorCode = ValidationErrorCodes.ConstraintViolation,
            SchemaConstraint = "maxLength: 50",
            ExpectedValue = "50 characters or less"
        };

        // Act
        var suggestions = _reporter.GenerateSuggestedFixes(error);

        // Assert
        Assert.NotEmpty(suggestions);
        Assert.Contains(suggestions, s => s.Contains("constraint"));
        Assert.Contains(suggestions, s => s.Contains("Expected value"));
    }

    [Fact]
    public void GenerateSuggestedFixes_ForSchemaNotFound_ShouldReturnSuggestions()
    {
        // Arrange
        var error = new ValidationError
        {
            ErrorCode = ValidationErrorCodes.SchemaNotFound
        };

        // Act
        var suggestions = _reporter.GenerateSuggestedFixes(error);

        // Assert
        Assert.NotEmpty(suggestions);
        Assert.Contains(suggestions, s => s.Contains("schema file exists"));
        Assert.Contains(suggestions, s => s.Contains("naming convention"));
        Assert.Contains(suggestions, s => s.Contains("embedded"));
    }

    [Fact]
    public void GenerateSuggestedFixes_ForSchemaParsingFailed_ShouldReturnSuggestions()
    {
        // Arrange
        var error = new ValidationError
        {
            ErrorCode = ValidationErrorCodes.SchemaParsingFailed
        };

        // Act
        var suggestions = _reporter.GenerateSuggestedFixes(error);

        // Assert
        Assert.NotEmpty(suggestions);
        Assert.Contains(suggestions, s => s.Contains("JSON syntax"));
        Assert.Contains(suggestions, s => s.Contains("JSON Schema specification"));
    }

    [Fact]
    public void GenerateSuggestedFixes_ForValidationTimeout_ShouldReturnSuggestions()
    {
        // Arrange
        var error = new ValidationError
        {
            ErrorCode = ValidationErrorCodes.ValidationTimeout
        };

        // Act
        var suggestions = _reporter.GenerateSuggestedFixes(error);

        // Assert
        Assert.NotEmpty(suggestions);
        Assert.Contains(suggestions, s => s.Contains("timeout"));
        Assert.Contains(suggestions, s => s.Contains("Simplify"));
        Assert.Contains(suggestions, s => s.Contains("circular references"));
    }

    [Fact]
    public void GenerateSuggestedFixes_ForUnknownErrorCode_ShouldReturnGenericSuggestions()
    {
        // Arrange
        var error = new ValidationError
        {
            ErrorCode = "UNKNOWN",
            JsonPath = "$.data",
            SchemaConstraint = "custom constraint"
        };

        // Act
        var suggestions = _reporter.GenerateSuggestedFixes(error);

        // Assert
        Assert.NotEmpty(suggestions);
        Assert.Contains(suggestions, s => s.Contains("$.data"));
        Assert.Contains(suggestions, s => s.Contains("constraint"));
    }

    [Fact]
    public void FormatErrors_WithMultipleErrors_ShouldEnhanceAllErrors()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new ValidationError
            {
                ErrorCode = ValidationErrorCodes.RequiredPropertyMissing,
                Message = "Missing property",
                JsonPath = "$.name"
            },
            new ValidationError
            {
                ErrorCode = ValidationErrorCodes.TypeMismatch,
                Message = "Type mismatch",
                JsonPath = "$.age",
                ExpectedValue = "number",
                ActualValue = "string"
            }
        };
        var context = ValidationContext.ForRequest(typeof(object), null, null);

        // Act
        var result = _reporter.FormatErrors(errors, context);

        // Assert
        Assert.Equal(2, result.Errors.Count);
        Assert.All(result.Errors, error => Assert.NotEmpty(error.SuggestedFixes));
    }
}
