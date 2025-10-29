using System;
using System.Collections.Generic;
using Relay.Core.ContractValidation.Models;
using Relay.Core.ContractValidation.Testing;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.Testing;

/// <summary>
/// Tests for ValidationAssertions.
/// </summary>
public class ValidationAssertionsTests
{
    [Fact]
    public void ShouldBeValid_WithValidResult_ShouldNotThrow()
    {
        // Arrange
        var result = ValidationResult.Success();

        // Act & Assert
        result.ShouldBeValid();
    }

    [Fact]
    public void ShouldBeValid_WithInvalidResult_ShouldThrow()
    {
        // Arrange
        var result = ValidationResult.Failure("CV001", "Error");

        // Act & Assert
        var ex = Assert.Throws<ValidationAssertionException>(() => result.ShouldBeValid());
        Assert.Contains("Expected validation result to be valid", ex.Message);
    }

    [Fact]
    public void ShouldBeInvalid_WithInvalidResult_ShouldNotThrow()
    {
        // Arrange
        var result = ValidationResult.Failure("CV001", "Error");

        // Act & Assert
        result.ShouldBeInvalid();
    }

    [Fact]
    public void ShouldBeInvalid_WithValidResult_ShouldThrow()
    {
        // Arrange
        var result = ValidationResult.Success();

        // Act & Assert
        var ex = Assert.Throws<ValidationAssertionException>(() => result.ShouldBeInvalid());
        Assert.Contains("Expected validation result to be invalid", ex.Message);
    }

    [Fact]
    public void ShouldHaveError_WithMatchingErrorCode_ShouldNotThrow()
    {
        // Arrange
        var error = ValidationError.Create("CV001", "Error");
        var result = ValidationResult.Failure(error);

        // Act & Assert
        result.ShouldHaveError("CV001");
    }

    [Fact]
    public void ShouldHaveError_WithoutMatchingErrorCode_ShouldThrow()
    {
        // Arrange
        var error = ValidationError.Create("CV001", "Error");
        var result = ValidationResult.Failure(error);

        // Act & Assert
        var ex = Assert.Throws<ValidationAssertionException>(() => result.ShouldHaveError("CV002"));
        Assert.Contains("Expected validation result to contain error code 'CV002'", ex.Message);
    }

    [Fact]
    public void ShouldHaveErrorAtPath_WithMatchingPath_ShouldNotThrow()
    {
        // Arrange
        var error = ValidationError.Create("CV001", "Error", "$.Name");
        var result = ValidationResult.Failure(error);

        // Act & Assert
        result.ShouldHaveErrorAtPath("$.Name");
    }

    [Fact]
    public void ShouldHaveErrorAtPath_WithoutMatchingPath_ShouldThrow()
    {
        // Arrange
        var error = ValidationError.Create("CV001", "Error", "$.Name");
        var result = ValidationResult.Failure(error);

        // Act & Assert
        var ex = Assert.Throws<ValidationAssertionException>(() => result.ShouldHaveErrorAtPath("$.Value"));
        Assert.Contains("Expected validation result to contain error at path '$.Value'", ex.Message);
    }

    [Fact]
    public void ShouldSuggestFix_WithMatchingSuggestion_ShouldNotThrow()
    {
        // Arrange
        var error = new ValidationError
        {
            ErrorCode = "CV001",
            Message = "Error",
            SuggestedFixes = new List<string> { "Fix 1", "Fix 2" }
        };

        // Act & Assert
        error.ShouldSuggestFix("Fix 1");
    }

    [Fact]
    public void ShouldSuggestFix_WithoutMatchingSuggestion_ShouldThrow()
    {
        // Arrange
        var error = new ValidationError
        {
            ErrorCode = "CV001",
            Message = "Error",
            SuggestedFixes = new List<string> { "Fix 1", "Fix 2" }
        };

        // Act & Assert
        var ex = Assert.Throws<ValidationAssertionException>(() => error.ShouldSuggestFix("Fix 3"));
        Assert.Contains("Expected validation error to suggest fix containing 'Fix 3'", ex.Message);
    }

    [Fact]
    public void ShouldHaveErrorCount_WithMatchingCount_ShouldNotThrow()
    {
        // Arrange
        var error1 = ValidationError.Create("CV001", "Error 1");
        var error2 = ValidationError.Create("CV002", "Error 2");
        var result = ValidationResult.Failure(error1, error2);

        // Act & Assert
        result.ShouldHaveErrorCount(2);
    }

    [Fact]
    public void ShouldHaveErrorCount_WithoutMatchingCount_ShouldThrow()
    {
        // Arrange
        var error = ValidationError.Create("CV001", "Error");
        var result = ValidationResult.Failure(error);

        // Act & Assert
        var ex = Assert.Throws<ValidationAssertionException>(() => result.ShouldHaveErrorCount(2));
        Assert.Contains("Expected validation result to have 2 error(s), but found 1", ex.Message);
    }

    [Fact]
    public void ShouldHaveErrorMessage_WithMatchingMessage_ShouldNotThrow()
    {
        // Arrange
        var error = ValidationError.Create("CV001", "This is an error message");
        var result = ValidationResult.Failure(error);

        // Act & Assert
        result.ShouldHaveErrorMessage("error message");
    }

    [Fact]
    public void ShouldHaveErrorMessage_WithoutMatchingMessage_ShouldThrow()
    {
        // Arrange
        var error = ValidationError.Create("CV001", "This is an error message");
        var result = ValidationResult.Failure(error);

        // Act & Assert
        var ex = Assert.Throws<ValidationAssertionException>(() => result.ShouldHaveErrorMessage("different"));
        Assert.Contains("Expected validation result to contain error message with 'different'", ex.Message);
    }

    [Fact]
    public void ShouldHaveSeverity_WithMatchingSeverity_ShouldNotThrow()
    {
        // Arrange
        var error = new ValidationError
        {
            ErrorCode = "CV001",
            Message = "Error",
            Severity = ValidationSeverity.Warning
        };

        // Act & Assert
        error.ShouldHaveSeverity(ValidationSeverity.Warning);
    }

    [Fact]
    public void ShouldHaveSeverity_WithoutMatchingSeverity_ShouldThrow()
    {
        // Arrange
        var error = new ValidationError
        {
            ErrorCode = "CV001",
            Message = "Error",
            Severity = ValidationSeverity.Error
        };

        // Act & Assert
        var ex = Assert.Throws<ValidationAssertionException>(() => error.ShouldHaveSeverity(ValidationSeverity.Warning));
        Assert.Contains("Expected validation error to have severity 'Warning', but found 'Error'", ex.Message);
    }

    [Fact]
    public void ShouldCompleteWithin_WithinDuration_ShouldNotThrow()
    {
        // Arrange
        var result = new ValidationResult
        {
            IsValid = true,
            ValidationDuration = TimeSpan.FromMilliseconds(50)
        };

        // Act & Assert
        result.ShouldCompleteWithin(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void ShouldCompleteWithin_ExceedingDuration_ShouldThrow()
    {
        // Arrange
        var result = new ValidationResult
        {
            IsValid = true,
            ValidationDuration = TimeSpan.FromMilliseconds(150)
        };

        // Act & Assert
        var ex = Assert.Throws<ValidationAssertionException>(() => result.ShouldCompleteWithin(TimeSpan.FromMilliseconds(100)));
        Assert.Contains("Expected validation to complete within 100ms", ex.Message);
    }

    [Fact]
    public void ShouldHaveExpectedAndActualValues_WithValues_ShouldNotThrow()
    {
        // Arrange
        var error = new ValidationError
        {
            ErrorCode = "CV001",
            Message = "Error",
            ExpectedValue = "expected",
            ActualValue = "actual"
        };

        // Act & Assert
        error.ShouldHaveExpectedAndActualValues();
    }

    [Fact]
    public void ShouldHaveExpectedAndActualValues_WithoutValues_ShouldThrow()
    {
        // Arrange
        var error = ValidationError.Create("CV001", "Error");

        // Act & Assert
        var ex = Assert.Throws<ValidationAssertionException>(() => error.ShouldHaveExpectedAndActualValues());
        Assert.Contains("Expected validation error to have ExpectedValue or ActualValue set", ex.Message);
    }

    [Fact]
    public void ShouldHaveValidatorName_WithValidatorName_ShouldNotThrow()
    {
        // Arrange
        var result = ValidationResult.Success("TestValidator");

        // Act & Assert
        result.ShouldHaveValidatorName();
    }

    [Fact]
    public void ShouldHaveValidatorName_WithoutValidatorName_ShouldThrow()
    {
        // Arrange
        var result = ValidationResult.Success();

        // Act & Assert
        var ex = Assert.Throws<ValidationAssertionException>(() => result.ShouldHaveValidatorName());
        Assert.Contains("Expected validation result to have a ValidatorName set", ex.Message);
    }

    [Fact]
    public void ShouldHaveValidatorName_WithMatchingName_ShouldNotThrow()
    {
        // Arrange
        var result = ValidationResult.Success("TestValidator");

        // Act & Assert
        result.ShouldHaveValidatorName("TestValidator");
    }

    [Fact]
    public void ShouldHaveValidatorName_WithoutMatchingName_ShouldThrow()
    {
        // Arrange
        var result = ValidationResult.Success("TestValidator");

        // Act & Assert
        var ex = Assert.Throws<ValidationAssertionException>(() => result.ShouldHaveValidatorName("OtherValidator"));
        Assert.Contains("Expected validation result to have validator name 'OtherValidator'", ex.Message);
    }

    [Fact]
    public void ShouldBeValid_WithNullResult_ShouldThrowArgumentNullException()
    {
        // Arrange
        ValidationResult result = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => result.ShouldBeValid());
    }

    [Fact]
    public void ShouldHaveError_WithNullErrorCode_ShouldThrowArgumentException()
    {
        // Arrange
        var result = ValidationResult.Success();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => result.ShouldHaveError(null!));
    }

    [Fact]
    public void ShouldSuggestFix_WithNullError_ShouldThrowArgumentNullException()
    {
        // Arrange
        ValidationError error = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => error.ShouldSuggestFix("fix"));
    }
}
