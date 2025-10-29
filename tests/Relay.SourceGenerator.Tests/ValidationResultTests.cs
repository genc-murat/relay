using Xunit;
using Relay.SourceGenerator.Validation;
using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for ValidationResult and related classes.
/// </summary>
public class ValidationResultTests
{
    [Fact]
    public void ValidationResult_NewInstance_IsValid()
    {
        // Arrange & Act
        var result = new ValidationResult();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void AddError_AddsErrorToCollection()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddError("Test error");

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Test error", result.Errors[0].Message);
    }

    [Fact]
    public void AddWarning_AddsWarningToCollection()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddWarning("Test warning");

        // Assert
        Assert.True(result.IsValid); // Warnings don't affect validity
        Assert.Single(result.Warnings);
        Assert.Equal("Test warning", result.Warnings[0].Message);
    }

    [Fact]
    public void AddError_WithLocation_StoresLocation()
    {
        // Arrange
        var result = new ValidationResult();
        var location = Location.None;

        // Act
        result.AddError("Test error", location);

        // Assert
        Assert.Equal(location, result.Errors[0].Location);
    }

    [Fact]
    public void Merge_CombinesErrorsAndWarnings()
    {
        // Arrange
        var result1 = new ValidationResult();
        result1.AddError("Error 1");
        result1.AddWarning("Warning 1");

        var result2 = new ValidationResult();
        result2.AddError("Error 2");
        result2.AddWarning("Warning 2");

        // Act
        result1.Merge(result2);

        // Assert
        Assert.Equal(2, result1.Errors.Count);
        Assert.Equal(2, result1.Warnings.Count);
        Assert.Contains(result1.Errors, e => e.Message == "Error 1");
        Assert.Contains(result1.Errors, e => e.Message == "Error 2");
    }

    [Fact]
    public void Merge_WithNull_DoesNotThrow()
    {
        // Arrange
        var result = new ValidationResult();

        // Act & Assert
        result.Merge(null!);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Success_ReturnsValidResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Failure_ReturnsInvalidResultWithError()
    {
        // Act
        var result = ValidationResult.Failure("Test error");

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Test error", result.Errors[0].Message);
    }

    [Fact]
    public void ValidationError_StoresMessageCorrectly()
    {
        // Act
        var error = new ValidationError("Test message");

        // Assert
        Assert.Equal("Test message", error.Message);
        Assert.Null(error.Location);
        Assert.Null(error.Descriptor);
    }

    [Fact]
    public void ValidationError_WithNullMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ValidationError(null!));
    }

    [Fact]
    public void ValidationWarning_StoresMessageCorrectly()
    {
        // Act
        var warning = new ValidationWarning("Test warning");

        // Assert
        Assert.Equal("Test warning", warning.Message);
        Assert.Null(warning.Location);
        Assert.Null(warning.Descriptor);
    }

    [Fact]
    public void ValidationWarning_WithNullMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ValidationWarning(null!));
    }

    [Fact]
    public void IsValid_WithMultipleErrors_ReturnsFalse()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddError("Error 1");
        result.AddError("Error 2");
        result.AddError("Error 3");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(3, result.Errors.Count);
    }

    [Fact]
    public void IsValid_WithOnlyWarnings_ReturnsTrue()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddWarning("Warning 1");
        result.AddWarning("Warning 2");

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(2, result.Warnings.Count);
    }
}
