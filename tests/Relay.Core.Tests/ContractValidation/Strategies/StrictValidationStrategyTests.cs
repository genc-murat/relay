using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.ContractValidation;
using Relay.Core.ContractValidation.Models;
using Relay.Core.ContractValidation.Strategies;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.Strategies;

public class StrictValidationStrategyTests
{
    private readonly StrictValidationStrategy _strategy;

    public StrictValidationStrategyTests()
    {
        _strategy = new StrictValidationStrategy();
    }

    [Fact]
    public void Name_ShouldReturnStrict()
    {
        // Act
        var name = _strategy.Name;

        // Assert
        Assert.Equal("Strict", name);
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
    public async Task HandleResultAsync_WithValidResult_ShouldReturnResultWithoutThrowing()
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
    }

    [Fact]
    public async Task HandleResultAsync_WithInvalidResult_ShouldThrowContractValidationException()
    {
        // Arrange
        var error = ValidationError.Create("CV001", "Test error");
        var invalidResult = ValidationResult.Failure(error);
        var context = new ValidationContext
        {
            ObjectType = typeof(string),
            IsRequest = true
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ContractValidationException>(
            async () => await _strategy.HandleResultAsync(invalidResult, context));

        Assert.Equal(typeof(string), exception.ObjectType);
        Assert.Single(exception.Errors);
        Assert.Contains("Test error", exception.Errors);
    }

    [Fact]
    public async Task HandleResultAsync_WithMultipleErrors_ShouldThrowWithAllErrors()
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
            ObjectType = typeof(string),
            IsRequest = true
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ContractValidationException>(
            async () => await _strategy.HandleResultAsync(invalidResult, context));

        Assert.Equal(2, exception.Errors.Count());
        Assert.Contains("First error", exception.Errors);
        Assert.Contains("Second error", exception.Errors);
    }

    [Fact]
    public async Task HandleResultAsync_WithErrorsContainingJsonPath_ShouldIncludePathInException()
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
            ObjectType = typeof(string),
            IsRequest = true
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ContractValidationException>(
            async () => await _strategy.HandleResultAsync(invalidResult, context));

        Assert.Single(exception.Errors);
        var errorMessage = exception.Errors.First();
        Assert.Contains("$.user.name", errorMessage);
        Assert.Contains("Invalid value", errorMessage);
    }

    [Fact]
    public async Task HandleResultAsync_WithCancellationToken_ShouldRespectCancellation()
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

        // Assert - Should complete without throwing OperationCanceledException for valid results
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task HandleResultAsync_WithEmptyErrorsList_ShouldNotThrow()
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

        // Assert - Should not throw when errors list is empty even if IsValid is false
        Assert.False(handledResult.IsValid);
    }
}
