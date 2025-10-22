using System;
using System.Collections.Generic;
using Relay.Core.ContractValidation;
using Xunit;

namespace Relay.Core.Tests.ContractValidation;

/// <summary>
/// Tests for ContractValidationException class
/// </summary>
public class ContractValidationExceptionTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var objectType = typeof(string);
        var errors = new[] { "Error 1", "Error 2" };

        // Act
        var exception = new ContractValidationException(objectType, errors);

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(objectType, exception.ObjectType);
        Assert.Equal(errors, exception.Errors);
    }

    [Fact]
    public void Constructor_WithValidParametersAndInnerException_CreatesInstance()
    {
        // Arrange
        var objectType = typeof(int);
        var errors = new[] { "Validation failed" };
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new ContractValidationException(objectType, errors, innerException);

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(objectType, exception.ObjectType);
        Assert.Equal(errors, exception.Errors);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void Constructor_WithNullObjectType_ThrowsArgumentNullException()
    {
        // Arrange
        var errors = new[] { "Error" };

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ContractValidationException(null, errors));
        Assert.Equal("objectType", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullErrors_ThrowsArgumentNullException()
    {
        // Arrange
        var objectType = typeof(TestClass);

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ContractValidationException(objectType, null));
        Assert.Equal("errors", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullObjectTypeAndInnerException_ThrowsArgumentNullException()
    {
        // Arrange
        var errors = new[] { "Error" };
        var innerException = new InvalidOperationException("Inner");

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ContractValidationException(null, errors, innerException));
        Assert.Equal("objectType", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullErrorsAndInnerException_ThrowsArgumentNullException()
    {
        // Arrange
        var objectType = typeof(TestClass);
        var innerException = new InvalidOperationException("Inner");

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ContractValidationException(objectType, null, innerException));
        Assert.Equal("errors", ex.ParamName);
    }



    #endregion

    #region Property Tests

    [Fact]
    public void ObjectType_Property_ReturnsCorrectValue()
    {
        // Arrange
        var objectType = typeof(TestClass);
        var errors = new[] { "Error" };
        var exception = new ContractValidationException(objectType, errors);

        // Act
        var result = exception.ObjectType;

        // Assert
        Assert.Equal(objectType, result);
    }

    [Fact]
    public void Errors_Property_ReturnsCorrectValue()
    {
        // Arrange
        var objectType = typeof(TestClass);
        var errors = new[] { "Error 1", "Error 2", "Error 3" };
        var exception = new ContractValidationException(objectType, errors);

        // Act
        var result = exception.Errors;

        // Assert
        Assert.Equal(errors, result);
    }

    [Fact]
    public void Errors_Property_IsReadOnly()
    {
        // Arrange
        var objectType = typeof(TestClass);
        var errors = new[] { "Error" };
        var exception = new ContractValidationException(objectType, errors);

        // Act & Assert
        // The property should be read-only, so we can't modify it
        Assert.IsAssignableFrom<IEnumerable<string>>(exception.Errors);
    }

    #endregion

    #region Message Tests

    [Fact]
    public void Message_SingleError_FormatsCorrectly()
    {
        // Arrange
        var objectType = typeof(TestClass);
        var errors = new[] { "Single error" };

        // Act
        var exception = new ContractValidationException(objectType, errors);

        // Assert
        Assert.Equal("Contract validation failed for TestClass. Errors: Single error", exception.Message);
    }

    [Fact]
    public void Message_MultipleErrors_FormatsCorrectly()
    {
        // Arrange
        var objectType = typeof(TestClass);
        var errors = new[] { "Error 1", "Error 2", "Error 3" };

        // Act
        var exception = new ContractValidationException(objectType, errors);

        // Assert
        Assert.Equal("Contract validation failed for TestClass. Errors: Error 1, Error 2, Error 3", exception.Message);
    }

    [Fact]
    public void Message_EmptyErrors_FormatsCorrectly()
    {
        // Arrange
        var objectType = typeof(TestClass);
        var errors = Array.Empty<string>();

        // Act
        var exception = new ContractValidationException(objectType, errors);

        // Assert
        Assert.Equal("Contract validation failed for TestClass. Errors: ", exception.Message);
    }

    [Fact]
    public void Message_WithInnerException_IncludesInnerException()
    {
        // Arrange
        var objectType = typeof(TestClass);
        var errors = new[] { "Validation error" };
        var innerException = new InvalidOperationException("Inner message");

        // Act
        var exception = new ContractValidationException(objectType, errors, innerException);

        // Assert
        Assert.Equal("Contract validation failed for TestClass. Errors: Validation error", exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void Message_GenericType_FormatsCorrectly()
    {
        // Arrange
        var objectType = typeof(List<string>);
        var errors = new[] { "List validation failed" };

        // Act
        var exception = new ContractValidationException(objectType, errors);

        // Assert
        Assert.Equal("Contract validation failed for List`1. Errors: List validation failed", exception.Message);
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void InheritsFromException()
    {
        // Arrange
        var objectType = typeof(TestClass);
        var errors = new[] { "Error" };

        // Act
        var exception = new ContractValidationException(objectType, errors);

        // Assert
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void CanBeCaughtAsException()
    {
        // Arrange
        var objectType = typeof(TestClass);
        var errors = new[] { "Error" };

        // Act
        Exception exception = new ContractValidationException(objectType, errors);

        // Assert
        Assert.IsAssignableFrom<Exception>(exception);
        Assert.IsType<ContractValidationException>(exception);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Errors_WithSpecialCharacters_HandledCorrectly()
    {
        // Arrange
        var objectType = typeof(TestClass);
        var errors = new[] { "Error with, comma", "Error with: colon", "Error with; semicolon" };

        // Act
        var exception = new ContractValidationException(objectType, errors);

        // Assert
        Assert.Equal("Contract validation failed for TestClass. Errors: Error with, comma, Error with: colon, Error with; semicolon", exception.Message);
    }

    [Fact]
    public void Errors_WithEmptyStrings_HandledCorrectly()
    {
        // Arrange
        var objectType = typeof(TestClass);
        var errors = new[] { "", "Valid error", "" };

        // Act
        var exception = new ContractValidationException(objectType, errors);

        // Assert
        Assert.Equal("Contract validation failed for TestClass. Errors: , Valid error, ", exception.Message);
    }

    #endregion

    private class TestClass
    {
        // Test class for type testing
    }
}