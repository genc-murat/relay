using Relay.Core.ContractValidation.Models;
using System;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.Models;

public class ValidationResultTests
{
    [Fact]
    public void Success_ShouldCreateValidResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Null(result.ValidatorName);
        Assert.Equal(TimeSpan.Zero, result.ValidationDuration);
    }

    [Fact]
    public void Success_WithValidatorName_ShouldCreateValidResult()
    {
        // Arrange
        var validatorName = "TestValidator";

        // Act
        var result = ValidationResult.Success(validatorName);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(validatorName, result.ValidatorName);
    }

    [Fact]
    public void Failure_WithErrors_ShouldCreateInvalidResult()
    {
        // Arrange
        var error1 = ValidationError.Create("CV001", "Error 1");
        var error2 = ValidationError.Create("CV002", "Error 2");

        // Act
        var result = ValidationResult.Failure(error1, error2);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(error1, result.Errors);
        Assert.Contains(error2, result.Errors);
    }

    [Fact]
    public void Failure_WithValidatorNameAndErrors_ShouldCreateInvalidResult()
    {
        // Arrange
        var validatorName = "TestValidator";
        var error = ValidationError.Create("CV001", "Error");

        // Act
        var result = ValidationResult.Failure(validatorName, error);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(validatorName, result.ValidatorName);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void Failure_WithErrorCodeAndMessage_ShouldCreateInvalidResult()
    {
        // Arrange
        var errorCode = ValidationErrorCodes.SchemaNotFound;
        var message = "Schema not found";

        // Act
        var result = ValidationResult.Failure(errorCode, message);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal(errorCode, result.Errors.First().ErrorCode);
        Assert.Equal(message, result.Errors.First().Message);
    }

    [Fact]
    public void ToString_WhenValid_ShouldReturnSuccessMessage()
    {
        // Arrange
        var result = ValidationResult.Success();

        // Act
        var message = result.ToString();

        // Assert
        Assert.Equal("Validation succeeded", message);
    }

    [Fact]
    public void ToString_WithOneError_ShouldReturnSingularMessage()
    {
        // Arrange
        var error = ValidationError.Create("CV001", "Error");
        var result = ValidationResult.Failure(error);

        // Act
        var message = result.ToString();

        // Assert
        Assert.Equal("Validation failed with 1 error", message);
    }

    [Fact]
    public void ToString_WithMultipleErrors_ShouldReturnPluralMessage()
    {
        // Arrange
        var error1 = ValidationError.Create("CV001", "Error 1");
        var error2 = ValidationError.Create("CV002", "Error 2");
        var error3 = ValidationError.Create("CV003", "Error 3");
        var result = ValidationResult.Failure(error1, error2, error3);

        // Act
        var message = result.ToString();

        // Assert
        Assert.Equal("Validation failed with 3 errors", message);
    }

    [Fact]
    public void ValidationResult_WithDuration_ShouldSetCorrectly()
    {
        // Arrange
        var duration = TimeSpan.FromMilliseconds(150);

        // Act
        var result = new ValidationResult
        {
            IsValid = true,
            ValidationDuration = duration
        };

        // Assert
        Assert.Equal(duration, result.ValidationDuration);
    }
}
