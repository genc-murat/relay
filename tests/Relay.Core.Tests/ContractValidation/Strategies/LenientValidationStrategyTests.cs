using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.ContractValidation.Models;
using Relay.Core.ContractValidation.Strategies;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.Strategies;

public class LenientValidationStrategyTests
{
    private readonly Mock<ILogger<LenientValidationStrategy>> _mockLogger;
    private readonly LenientValidationStrategy _strategy;

    public LenientValidationStrategyTests()
    {
        _mockLogger = new Mock<ILogger<LenientValidationStrategy>>();
        _strategy = new LenientValidationStrategy(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LenientValidationStrategy(null!));
    }

    [Fact]
    public void Name_ShouldReturnLenient()
    {
        // Act
        var name = _strategy.Name;

        // Assert
        Assert.Equal("Lenient", name);
    }

    [Fact]
    public void ShouldValidate_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var context = new ValidationContext
        {
            ObjectType = typeof(string),
            IsRequest = true
        };

        // Act
        var result = _strategy.ShouldValidate(context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HandleResultAsync_WithValidResult_ShouldReturnResultWithoutLogging()
    {
        // Arrange
        var validResult = ValidationResult.Success();
        var context = new ValidationContext
        {
            ObjectType = typeof(string),
            IsRequest = true
        };

        // Act
        var result = await _strategy.HandleResultAsync(validResult, context);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleResultAsync_WithInvalidResult_ShouldLogWarningAndNotThrow()
    {
        // Arrange
        var error = ValidationError.Create("CV001", "Test error");
        var invalidResult = ValidationResult.Failure(error);
        var context = new ValidationContext
        {
            ObjectType = typeof(TestRequest),
            IsRequest = true
        };

        // Act
        var result = await _strategy.HandleResultAsync(invalidResult, context);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        
        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Contract validation warnings")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleResultAsync_WithMultipleErrors_ShouldLogAllErrors()
    {
        // Arrange
        var errors = new[]
        {
            ValidationError.Create("CV001", "First error"),
            ValidationError.Create("CV002", "Second error")
        };
        var invalidResult = ValidationResult.Failure(errors);
        var context = new ValidationContext
        {
            ObjectType = typeof(TestRequest),
            IsRequest = true
        };

        // Act
        var result = await _strategy.HandleResultAsync(invalidResult, context);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        
        // Verify summary warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("2 error(s) found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify individual error warnings were logged (1 summary + 2 individual = 3 total)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task HandleResultAsync_WithErrorContainingJsonPath_ShouldLogPath()
    {
        // Arrange
        var error = new ValidationError
        {
            ErrorCode = "CV001",
            Message = "Invalid value",
            JsonPath = "$.user.name"
        };
        var invalidResult = ValidationResult.Failure(error);
        var context = new ValidationContext
        {
            ObjectType = typeof(TestRequest),
            IsRequest = true
        };

        // Act
        await _strategy.HandleResultAsync(invalidResult, context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("$.user.name")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleResultAsync_WithErrorWithoutJsonPath_ShouldLogRootAsPath()
    {
        // Arrange
        var error = new ValidationError
        {
            ErrorCode = "CV001",
            Message = "Invalid value",
            JsonPath = string.Empty
        };
        var invalidResult = ValidationResult.Failure(error);
        var context = new ValidationContext
        {
            ObjectType = typeof(TestRequest),
            IsRequest = true
        };

        // Act
        await _strategy.HandleResultAsync(invalidResult, context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("root")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleResultAsync_ForRequestValidation_ShouldLogRequestType()
    {
        // Arrange
        var error = ValidationError.Create("CV001", "Test error");
        var invalidResult = ValidationResult.Failure(error);
        var context = new ValidationContext
        {
            ObjectType = typeof(TestRequest),
            IsRequest = true
        };

        // Act
        await _strategy.HandleResultAsync(invalidResult, context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleResultAsync_ForResponseValidation_ShouldLogResponseType()
    {
        // Arrange
        var error = ValidationError.Create("CV001", "Test error");
        var invalidResult = ValidationResult.Failure(error);
        var context = new ValidationContext
        {
            ObjectType = typeof(TestResponse),
            IsRequest = false
        };

        // Act
        await _strategy.HandleResultAsync(invalidResult, context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Response")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleResultAsync_WithCancellationToken_ShouldComplete()
    {
        // Arrange
        var validResult = ValidationResult.Success();
        var context = new ValidationContext
        {
            ObjectType = typeof(string),
            IsRequest = true
        };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _strategy.HandleResultAsync(validResult, context, cts.Token);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task HandleResultAsync_WithEmptyErrorsList_ShouldNotLog()
    {
        // Arrange
        var result = new ValidationResult
        {
            IsValid = false,
            Errors = new List<ValidationError>()
        };
        var context = new ValidationContext
        {
            ObjectType = typeof(string),
            IsRequest = true
        };

        // Act
        var handledResult = await _strategy.HandleResultAsync(result, context);

        // Assert
        Assert.False(handledResult.IsValid);
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    // Test helper classes
    private class TestRequest { }
    private class TestResponse { }
}
