using Relay.Core.ContractValidation.Models;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.Models;

public class ValidationErrorTests
{
    [Fact]
    public void Create_WithErrorCodeAndMessage_ShouldCreateValidationError()
    {
        // Arrange
        var errorCode = ValidationErrorCodes.RequiredPropertyMissing;
        var message = "Property is required";

        // Act
        var error = ValidationError.Create(errorCode, message);

        // Assert
        Assert.Equal(errorCode, error.ErrorCode);
        Assert.Equal(message, error.Message);
        Assert.Empty(error.JsonPath);
        Assert.Null(error.ExpectedValue);
        Assert.Null(error.ActualValue);
        Assert.Null(error.SchemaConstraint);
        Assert.Empty(error.SuggestedFixes);
        Assert.Equal(ValidationSeverity.Error, error.Severity);
    }

    [Fact]
    public void Create_WithErrorCodeMessageAndPath_ShouldCreateValidationError()
    {
        // Arrange
        var errorCode = ValidationErrorCodes.TypeMismatch;
        var message = "Type mismatch";
        var jsonPath = "$.user.name";

        // Act
        var error = ValidationError.Create(errorCode, message, jsonPath);

        // Assert
        Assert.Equal(errorCode, error.ErrorCode);
        Assert.Equal(message, error.Message);
        Assert.Equal(jsonPath, error.JsonPath);
    }

    [Fact]
    public void ToString_WithJsonPath_ShouldFormatCorrectly()
    {
        // Arrange
        var error = new ValidationError
        {
            ErrorCode = "CV001",
            Message = "Test error",
            JsonPath = "$.user.email"
        };

        // Act
        var result = error.ToString();

        // Assert
        Assert.Equal("[CV001] Test error at '$.user.email'", result);
    }

    [Fact]
    public void ToString_WithoutJsonPath_ShouldUseRoot()
    {
        // Arrange
        var error = new ValidationError
        {
            ErrorCode = "CV002",
            Message = "Test error"
        };

        // Act
        var result = error.ToString();

        // Assert
        Assert.Equal("[CV002] Test error at 'root'", result);
    }

    [Fact]
    public void ValidationError_WithAllProperties_ShouldSetCorrectly()
    {
        // Arrange & Act
        var error = new ValidationError
        {
            ErrorCode = ValidationErrorCodes.ConstraintViolation,
            Message = "Value exceeds maximum",
            JsonPath = "$.age",
            ExpectedValue = 100,
            ActualValue = 150,
            SchemaConstraint = "maximum: 100",
            SuggestedFixes = new() { "Reduce the value to 100 or less" },
            Severity = ValidationSeverity.Critical
        };

        // Assert
        Assert.Equal(ValidationErrorCodes.ConstraintViolation, error.ErrorCode);
        Assert.Equal("Value exceeds maximum", error.Message);
        Assert.Equal("$.age", error.JsonPath);
        Assert.Equal(100, error.ExpectedValue);
        Assert.Equal(150, error.ActualValue);
        Assert.Equal("maximum: 100", error.SchemaConstraint);
        Assert.Single(error.SuggestedFixes);
        Assert.Equal(ValidationSeverity.Critical, error.Severity);
    }
}
