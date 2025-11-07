using Relay.SourceGenerator.Core;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for ErrorIsolation.IsRecoverableException method.
/// Verifies correct classification of recoverable vs critical exceptions.
/// </summary>
public class ErrorIsolationIsRecoverableExceptionTests
{
    #region Recoverable Exception Tests

    [Fact]
    public void IsRecoverableException_InvalidOperationException_ReturnsTrue()
    {
        // Arrange
        var exception = new InvalidOperationException("Invalid operation");

        // Act
        var result = ErrorIsolation.IsRecoverableException(exception);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRecoverableException_ArgumentException_ReturnsTrue()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument");

        // Act
        var result = ErrorIsolation.IsRecoverableException(exception);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRecoverableException_NullReferenceException_ReturnsTrue()
    {
        // Arrange
        var exception = new NullReferenceException("Null reference");

        // Act
        var result = ErrorIsolation.IsRecoverableException(exception);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRecoverableException_CustomException_ReturnsTrue()
    {
        // Arrange
        var exception = new CustomTestException("Custom error");

        // Act
        var result = ErrorIsolation.IsRecoverableException(exception);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRecoverableException_GenericException_ReturnsTrue()
    {
        // Arrange
        var exception = new Exception("Generic error");

        // Act
        var result = ErrorIsolation.IsRecoverableException(exception);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRecoverableException_TimeoutException_ReturnsTrue()
    {
        // Arrange
        var exception = new TimeoutException("Operation timed out");

        // Act
        var result = ErrorIsolation.IsRecoverableException(exception);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRecoverableException_IOException_ReturnsTrue()
    {
        // Arrange
        var exception = new System.IO.IOException("IO error");

        // Act
        var result = ErrorIsolation.IsRecoverableException(exception);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRecoverableException_NestedException_ReturnsTrue()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");
        var exception = new Exception("Outer error", innerException);

        // Act
        var result = ErrorIsolation.IsRecoverableException(exception);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Critical Exception Tests

    [Fact]
    public void IsRecoverableException_OperationCanceledException_ReturnsFalse()
    {
        // Arrange
        var exception = new OperationCanceledException("Operation cancelled");

        // Act
        var result = ErrorIsolation.IsRecoverableException(exception);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRecoverableException_OutOfMemoryException_ReturnsFalse()
    {
        // Arrange
        var exception = new OutOfMemoryException("Out of memory");

        // Act
        var result = ErrorIsolation.IsRecoverableException(exception);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRecoverableException_StackOverflowException_ReturnsFalse()
    {
        // Arrange
        var exception = new StackOverflowException("Stack overflow");

        // Act
        var result = ErrorIsolation.IsRecoverableException(exception);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRecoverableException_OperationCanceledExceptionWithInnerException_ReturnsFalse()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");
        var exception = new OperationCanceledException("Operation cancelled", innerException);

        // Act
        var result = ErrorIsolation.IsRecoverableException(exception);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRecoverableException_OutOfMemoryExceptionWithNullMessage_ReturnsFalse()
    {
        // Arrange
        var exception = new OutOfMemoryException(null);

        // Act
        var result = ErrorIsolation.IsRecoverableException(exception);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRecoverableException_StackOverflowExceptionWithEmptyMessage_ReturnsFalse()
    {
        // Arrange
        var exception = new StackOverflowException("");

        // Act
        var result = ErrorIsolation.IsRecoverableException(exception);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void IsRecoverableException_NullException_ThrowsArgumentNullException()
    {
        // Arrange
        Exception exception = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ErrorIsolation.IsRecoverableException(exception));
    }

    [Fact]
    public void IsRecoverableException_InheritanceChain_CustomCriticalException_ReturnsFalse()
    {
        // Arrange
        var exception = new CustomCriticalException("Custom critical error");

        // Act
        var result = ErrorIsolation.IsRecoverableException(exception);

        // Assert
        Assert.False(result); // Derivatives of critical exceptions are also critical
    }

    [Fact]
    public void IsRecoverableException_InheritanceChain_CustomRecoverableException_ReturnsTrue()
    {
        // Arrange
        var exception = new CustomRecoverableException("Custom recoverable error");

        // Act
        var result = ErrorIsolation.IsRecoverableException(exception);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Helper Classes

    private class CustomTestException : Exception
    {
        public CustomTestException(string message) : base(message) { }
        public CustomTestException(string message, Exception innerException) : base(message, innerException) { }
    }

    private class CustomCriticalException : OutOfMemoryException
    {
        public CustomCriticalException(string message) : base(message) { }
    }

    private class CustomRecoverableException : InvalidOperationException
    {
        public CustomRecoverableException(string message) : base(message) { }
    }

    #endregion
}