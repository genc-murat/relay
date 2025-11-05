using System;
using Xunit;

namespace Relay.Core.Transactions.Tests;

public class TransactionRetryAttributeTests
{
    [Fact]
    public void Constructor_InitializesDefaultValues()
    {
        // Arrange & Act
        var attribute = new TransactionRetryAttribute();

        // Assert
        Assert.Equal(3, attribute.MaxRetries);
        Assert.Equal(100, attribute.InitialDelayMs);
        Assert.Equal(RetryStrategy.ExponentialBackoff, attribute.Strategy);
    }

    [Fact]
    public void MaxRetries_SetToValidValue_Succeeds()
    {
        // Arrange
        var attribute = new TransactionRetryAttribute();

        // Act
        attribute.MaxRetries = 5;

        // Assert
        Assert.Equal(5, attribute.MaxRetries);
    }

    [Fact]
    public void MaxRetries_SetToZero_Succeeds()
    {
        // Arrange
        var attribute = new TransactionRetryAttribute();

        // Act
        attribute.MaxRetries = 0;

        // Assert
        Assert.Equal(0, attribute.MaxRetries);
    }

    [Fact]
    public void MaxRetries_SetToNegative_ValidateThrowsException()
    {
        // Arrange
        var attribute = new TransactionRetryAttribute();
        attribute.MaxRetries = -1;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => attribute.Validate());
        Assert.Contains("MaxRetries cannot be negative", exception.Message);
        Assert.Contains("Current value: -1", exception.Message);
    }

    [Fact]
    public void InitialDelayMs_SetToValidValue_Succeeds()
    {
        // Arrange
        var attribute = new TransactionRetryAttribute();

        // Act
        attribute.InitialDelayMs = 500;

        // Assert
        Assert.Equal(500, attribute.InitialDelayMs);
    }

    [Fact]
    public void InitialDelayMs_SetToZero_Succeeds()
    {
        // Arrange
        var attribute = new TransactionRetryAttribute();

        // Act
        attribute.InitialDelayMs = 0;

        // Assert
        Assert.Equal(0, attribute.InitialDelayMs);
    }

    [Fact]
    public void InitialDelayMs_SetToNegative_ValidateThrowsException()
    {
        // Arrange
        var attribute = new TransactionRetryAttribute();
        attribute.InitialDelayMs = -1;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => attribute.Validate());
        Assert.Contains("InitialDelayMs cannot be negative", exception.Message);
        Assert.Contains("Current value: -1", exception.Message);
    }

    [Fact]
    public void Strategy_SetToValidValue_Succeeds()
    {
        // Arrange
        var attribute = new TransactionRetryAttribute();

        // Act
        attribute.Strategy = RetryStrategy.Linear;

        // Assert
        Assert.Equal(RetryStrategy.Linear, attribute.Strategy);
    }

    [Fact]
    public void Strategy_SetToInvalidValue_ValidateThrowsException()
    {
        // Arrange - We need to use reflection to set an invalid enum value
        var attribute = new TransactionRetryAttribute();
        
        // Use reflection to set an invalid enum value
        typeof(TransactionRetryAttribute).GetProperty(nameof(TransactionRetryAttribute.Strategy))
            ?.SetValue(attribute, (RetryStrategy)999);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => attribute.Validate());
        Assert.Contains("Invalid retry strategy", exception.Message);
        Assert.Contains("Must be one of: Linear, ExponentialBackoff", exception.Message);
    }

    [Fact]
    public void Validate_WithValidConfiguration_DoesNotThrow()
    {
        // Arrange
        var attribute = new TransactionRetryAttribute
        {
            MaxRetries = 5,
            InitialDelayMs = 200,
            Strategy = RetryStrategy.Linear
        };

        // Act & Assert - Should not throw
        attribute.Validate();
    }

    [Fact]
    public void Validate_WithNegativeMaxRetries_ThrowsException()
    {
        // Arrange
        var attribute = new TransactionRetryAttribute();
        attribute.MaxRetries = -1;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => attribute.Validate());
        Assert.Contains("MaxRetries cannot be negative", exception.Message);
        Assert.Contains("Current value: -1", exception.Message);
        Assert.Contains("Use 0 to disable retries or a positive value", exception.Message);
    }

    [Fact]
    public void Validate_WithNegativeInitialDelayMs_ThrowsException()
    {
        // Arrange
        var attribute = new TransactionRetryAttribute();
        attribute.InitialDelayMs = -1;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => attribute.Validate());
        Assert.Contains("InitialDelayMs cannot be negative", exception.Message);
        Assert.Contains("Current value: -1", exception.Message);
        Assert.Contains("Use 0 for no delay or a positive value", exception.Message);
    }

    [Fact]
    public void Validate_WithInvalidStrategy_ThrowsException()
    {
        // Arrange - Using reflection to set an invalid enum value
        var attribute = new TransactionRetryAttribute();
        
        typeof(TransactionRetryAttribute).GetProperty(nameof(TransactionRetryAttribute.Strategy))
            ?.SetValue(attribute, (RetryStrategy)999);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => attribute.Validate());
        Assert.Contains("Invalid retry strategy", exception.Message);
        Assert.Contains("Must be one of: Linear, ExponentialBackoff", exception.Message);
    }

    [Fact]
    public void Validate_WithMultipleInvalidValues_ThrowsExceptionForFirstError()
    {
        // Arrange
        var attribute = new TransactionRetryAttribute();
        attribute.MaxRetries = -1;
        attribute.InitialDelayMs = -1;
        
        // Using reflection to set an invalid enum value
        typeof(TransactionRetryAttribute).GetProperty(nameof(TransactionRetryAttribute.Strategy))
            ?.SetValue(attribute, (RetryStrategy)999);

        // Act & Assert - Should throw for the first validation error (MaxRetries)
        var exception = Assert.Throws<InvalidOperationException>(() => attribute.Validate());
        Assert.Contains("MaxRetries cannot be negative", exception.Message);
    }
}
