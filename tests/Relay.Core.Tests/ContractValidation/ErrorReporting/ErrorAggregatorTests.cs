using Relay.Core.ContractValidation.ErrorReporting;
using Relay.Core.ContractValidation.Models;
using System;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.ErrorReporting;

public class ErrorAggregatorTests
{
    [Fact]
    public void Constructor_WithValidMaxErrorCount_ShouldCreate()
    {
        // Act
        var aggregator = new ErrorAggregator(50);

        // Assert
        Assert.Equal(0, aggregator.ErrorCount);
        Assert.False(aggregator.HasErrors);
        Assert.False(aggregator.HasReachedMaxErrors);
    }

    [Fact]
    public void Constructor_WithZeroMaxErrorCount_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ErrorAggregator(0));
    }

    [Fact]
    public void Constructor_WithNegativeMaxErrorCount_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ErrorAggregator(-1));
    }

    [Fact]
    public void AddError_WithValidError_ShouldAddError()
    {
        // Arrange
        var aggregator = new ErrorAggregator();
        var error = ValidationError.Create("CV001", "Test error");

        // Act
        var result = aggregator.AddError(error);

        // Assert
        Assert.True(result);
        Assert.Equal(1, aggregator.ErrorCount);
        Assert.True(aggregator.HasErrors);
    }

    [Fact]
    public void AddError_WithNullError_ShouldThrowArgumentNullException()
    {
        // Arrange
        var aggregator = new ErrorAggregator();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => aggregator.AddError(null!));
    }

    [Fact]
    public void AddError_WhenMaxErrorsReached_ShouldReturnFalse()
    {
        // Arrange
        var aggregator = new ErrorAggregator(2);
        aggregator.AddError(ValidationError.Create("CV001", "Error 1"));
        aggregator.AddError(ValidationError.Create("CV002", "Error 2"));

        // Act
        var result = aggregator.AddError(ValidationError.Create("CV003", "Error 3"));

        // Assert
        Assert.False(result);
        Assert.Equal(2, aggregator.ErrorCount);
        Assert.True(aggregator.HasReachedMaxErrors);
    }

    [Fact]
    public void AddErrors_WithMultipleErrors_ShouldAddAll()
    {
        // Arrange
        var aggregator = new ErrorAggregator();
        var errors = new[]
        {
            ValidationError.Create("CV001", "Error 1"),
            ValidationError.Create("CV002", "Error 2"),
            ValidationError.Create("CV003", "Error 3")
        };

        // Act
        var addedCount = aggregator.AddErrors(errors);

        // Assert
        Assert.Equal(3, addedCount);
        Assert.Equal(3, aggregator.ErrorCount);
    }

    [Fact]
    public void AddErrors_WithNullErrors_ShouldThrowArgumentNullException()
    {
        // Arrange
        var aggregator = new ErrorAggregator();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => aggregator.AddErrors(null!));
    }

    [Fact]
    public void AddErrors_WhenMaxErrorsReached_ShouldStopAdding()
    {
        // Arrange
        var aggregator = new ErrorAggregator(2);
        var errors = new[]
        {
            ValidationError.Create("CV001", "Error 1"),
            ValidationError.Create("CV002", "Error 2"),
            ValidationError.Create("CV003", "Error 3")
        };

        // Act
        var addedCount = aggregator.AddErrors(errors);

        // Assert
        Assert.Equal(2, addedCount);
        Assert.Equal(2, aggregator.ErrorCount);
        Assert.True(aggregator.HasReachedMaxErrors);
    }

    [Fact]
    public void GetErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var aggregator = new ErrorAggregator();
        var error1 = ValidationError.Create("CV001", "Error 1");
        var error2 = ValidationError.Create("CV002", "Error 2");
        aggregator.AddError(error1);
        aggregator.AddError(error2);

        // Act
        var errors = aggregator.GetErrors();

        // Assert
        Assert.Equal(2, errors.Count);
        Assert.Contains(error1, errors);
        Assert.Contains(error2, errors);
    }

    [Fact]
    public void GetErrorsBySeverity_ShouldFilterCorrectly()
    {
        // Arrange
        var aggregator = new ErrorAggregator();
        aggregator.AddError(new ValidationError
        {
            ErrorCode = "CV001",
            Message = "Info",
            Severity = ValidationSeverity.Info
        });
        aggregator.AddError(new ValidationError
        {
            ErrorCode = "CV002",
            Message = "Warning",
            Severity = ValidationSeverity.Warning
        });
        aggregator.AddError(new ValidationError
        {
            ErrorCode = "CV003",
            Message = "Error",
            Severity = ValidationSeverity.Error
        });
        aggregator.AddError(new ValidationError
        {
            ErrorCode = "CV004",
            Message = "Critical",
            Severity = ValidationSeverity.Critical
        });

        // Act
        var errors = aggregator.GetErrorsBySeverity(ValidationSeverity.Error);

        // Assert
        Assert.Equal(2, errors.Count);
        Assert.All(errors, e => Assert.True(e.Severity >= ValidationSeverity.Error));
    }

    [Fact]
    public void GetErrorsByPath_ShouldGroupCorrectly()
    {
        // Arrange
        var aggregator = new ErrorAggregator();
        aggregator.AddError(new ValidationError
        {
            ErrorCode = "CV001",
            Message = "Error 1",
            JsonPath = "$.user.name"
        });
        aggregator.AddError(new ValidationError
        {
            ErrorCode = "CV002",
            Message = "Error 2",
            JsonPath = "$.user.name"
        });
        aggregator.AddError(new ValidationError
        {
            ErrorCode = "CV003",
            Message = "Error 3",
            JsonPath = "$.user.email"
        });

        // Act
        var errorsByPath = aggregator.GetErrorsByPath();

        // Assert
        Assert.Equal(2, errorsByPath.Count);
        Assert.Equal(2, errorsByPath["$.user.name"].Count);
        Assert.Single(errorsByPath["$.user.email"]);
    }

    [Fact]
    public void GetErrorsByPath_WithEmptyPath_ShouldUseRoot()
    {
        // Arrange
        var aggregator = new ErrorAggregator();
        aggregator.AddError(new ValidationError
        {
            ErrorCode = "CV001",
            Message = "Error",
            JsonPath = ""
        });

        // Act
        var errorsByPath = aggregator.GetErrorsByPath();

        // Assert
        Assert.Single(errorsByPath);
        Assert.True(errorsByPath.ContainsKey("root"));
    }

    [Fact]
    public void FormatErrorMessage_WithNoErrors_ShouldReturnEmptyString()
    {
        // Arrange
        var aggregator = new ErrorAggregator();

        // Act
        var message = aggregator.FormatErrorMessage();

        // Assert
        Assert.Empty(message);
    }

    [Fact]
    public void FormatErrorMessage_WithErrors_ShouldFormatCorrectly()
    {
        // Arrange
        var aggregator = new ErrorAggregator();
        aggregator.AddError(new ValidationError
        {
            ErrorCode = "CV001",
            Message = "Test error",
            JsonPath = "$.name"
        });

        // Act
        var message = aggregator.FormatErrorMessage();

        // Assert
        Assert.Contains("Validation failed with 1 error(s)", message);
        Assert.Contains("$.name", message);
        Assert.Contains("[CV001]", message);
        Assert.Contains("Test error", message);
    }

    [Fact]
    public void FormatErrorMessage_WithSuggestedFixes_ShouldIncludeSuggestions()
    {
        // Arrange
        var aggregator = new ErrorAggregator();
        aggregator.AddError(new ValidationError
        {
            ErrorCode = "CV001",
            Message = "Test error",
            JsonPath = "$.name",
            SuggestedFixes = new() { "Fix 1", "Fix 2" }
        });

        // Act
        var message = aggregator.FormatErrorMessage();

        // Assert
        Assert.Contains("Suggestions:", message);
        Assert.Contains("Fix 1", message);
        Assert.Contains("Fix 2", message);
    }

    [Fact]
    public void FormatErrorMessage_WhenMaxErrorsReached_ShouldIncludeNote()
    {
        // Arrange
        var aggregator = new ErrorAggregator(2);
        aggregator.AddError(ValidationError.Create("CV001", "Error 1"));
        aggregator.AddError(ValidationError.Create("CV002", "Error 2"));
        aggregator.AddError(ValidationError.Create("CV003", "Error 3")); // Won't be added

        // Act
        var message = aggregator.FormatErrorMessage();

        // Assert
        Assert.Contains("Maximum error count", message);
        Assert.Contains("Additional errors may exist", message);
    }

    [Fact]
    public void FormatErrorSummary_WithNoErrors_ShouldReturnNoErrorsMessage()
    {
        // Arrange
        var aggregator = new ErrorAggregator();

        // Act
        var summary = aggregator.FormatErrorSummary();

        // Assert
        Assert.Equal("No validation errors", summary);
    }

    [Fact]
    public void FormatErrorSummary_WithMixedSeverities_ShouldFormatCorrectly()
    {
        // Arrange
        var aggregator = new ErrorAggregator();
        aggregator.AddError(new ValidationError { ErrorCode = "CV001", Message = "Critical", Severity = ValidationSeverity.Critical });
        aggregator.AddError(new ValidationError { ErrorCode = "CV002", Message = "Error 1", Severity = ValidationSeverity.Error });
        aggregator.AddError(new ValidationError { ErrorCode = "CV003", Message = "Error 2", Severity = ValidationSeverity.Error });
        aggregator.AddError(new ValidationError { ErrorCode = "CV004", Message = "Warning", Severity = ValidationSeverity.Warning });

        // Act
        var summary = aggregator.FormatErrorSummary();

        // Assert
        Assert.Contains("1 critical", summary);
        Assert.Contains("2 error(s)", summary);
        Assert.Contains("1 warning(s)", summary);
    }

    [Fact]
    public void Clear_ShouldRemoveAllErrors()
    {
        // Arrange
        var aggregator = new ErrorAggregator();
        aggregator.AddError(ValidationError.Create("CV001", "Error 1"));
        aggregator.AddError(ValidationError.Create("CV002", "Error 2"));

        // Act
        aggregator.Clear();

        // Assert
        Assert.Equal(0, aggregator.ErrorCount);
        Assert.False(aggregator.HasErrors);
        Assert.False(aggregator.HasReachedMaxErrors);
    }

    [Fact]
    public void ToValidationResult_WithNoErrors_ShouldReturnValidResult()
    {
        // Arrange
        var aggregator = new ErrorAggregator();

        // Act
        var result = aggregator.ToValidationResult("TestValidator");

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal("TestValidator", result.ValidatorName);
    }

    [Fact]
    public void ToValidationResult_WithErrors_ShouldReturnInvalidResult()
    {
        // Arrange
        var aggregator = new ErrorAggregator();
        var error = ValidationError.Create("CV001", "Test error");
        aggregator.AddError(error);

        // Act
        var result = aggregator.ToValidationResult("TestValidator");

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("TestValidator", result.ValidatorName);
    }
}
