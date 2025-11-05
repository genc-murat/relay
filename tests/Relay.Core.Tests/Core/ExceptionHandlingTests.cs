using System;
using Relay.Core;
using Xunit;

namespace Relay.Core.Tests.Core;

/// <summary>
/// Comprehensive tests for Relay exception handling
/// </summary>
public class ExceptionHandlingTests
{
    [Fact]
    public void RelayException_WithAllParameters_ShouldSetProperties()
    {
        // Arrange
        var requestType = "TestRequest";
        var handlerName = "TestHandler";
        var message = "Test error message";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new RelayException(requestType, handlerName, message, innerException);

        // Assert
        Assert.Equal(requestType, exception.RequestType);
        Assert.Equal(handlerName, exception.HandlerName);
        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void RelayException_WithoutInnerException_ShouldSetProperties()
    {
        // Arrange
        var requestType = "TestRequest";
        var handlerName = "TestHandler";
        var message = "Test error message";

        // Act
        var exception = new RelayException(requestType, handlerName, message);

        // Assert
        Assert.Equal(requestType, exception.RequestType);
        Assert.Equal(handlerName, exception.HandlerName);
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void RelayException_WithNullHandlerName_ShouldSetProperties()
    {
        // Arrange
        var requestType = "TestRequest";
        var message = "Test error message";

        // Act
        var exception = new RelayException(requestType, null, message);

        // Assert
        Assert.Equal(requestType, exception.RequestType);
        Assert.Null(exception.HandlerName);
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void HandlerNotFoundException_ShouldSetCorrectMessage()
    {
        // Arrange
        var requestType = "TestRequest";

        // Act
        var exception = new HandlerNotFoundException(requestType);

        // Assert
        Assert.Equal(requestType, exception.RequestType);
        Assert.Null(exception.HandlerName);
        Assert.Equal($"No handler found for request type '{requestType}'", exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void HandlerNotFoundException_WithHandlerName_ShouldSetCorrectMessage()
    {
        // Arrange
        var requestType = "TestRequest";
        var handlerName = "TestHandler";

        // Act
        var exception = new HandlerNotFoundException(requestType, handlerName);

        // Assert
        Assert.Equal(requestType, exception.RequestType);
        Assert.Equal(handlerName, exception.HandlerName);
        Assert.Equal($"No handler named '{handlerName}' found for request type '{requestType}'", exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void HandlerNotFoundException_ShouldInheritFromRelayException()
    {
        // Arrange & Act
        var exception = new HandlerNotFoundException("TestRequest");

        // Assert
        Assert.IsAssignableFrom<RelayException>(exception);
    }

    [Fact]
    public void MultipleHandlersException_ShouldSetCorrectMessage()
    {
        // Arrange
        var requestType = "TestRequest";

        // Act
        var exception = new MultipleHandlersException(requestType);

        // Assert
        Assert.Equal(requestType, exception.RequestType);
        Assert.Null(exception.HandlerName);
        Assert.Equal($"Multiple handlers found for request type '{requestType}'. Use named handlers or ensure only one handler is registered.", exception.Message);
    }

    [Fact]
    public void MultipleHandlersException_ShouldInheritFromRelayException()
    {
        // Arrange & Act
        var exception = new MultipleHandlersException("TestRequest");

        // Assert
        Assert.IsAssignableFrom<RelayException>(exception);
    }

    [Fact]
    public void ExceptionSerialization_ShouldPreserveProperties()
    {
        // Arrange
        var originalException = new RelayException("TestRequest", "TestHandler", "Test message", new ArgumentException("Inner"));

        // Act - Simulate serialization/deserialization
        var serialized = originalException.ToString();

        // Assert
        Assert.Contains("TestRequest", serialized);
        Assert.Contains("TestHandler", serialized);
        Assert.Contains("Test message", serialized);
    }

    [Fact]
    public void ExceptionHierarchy_ShouldBeCorrect()
    {
        // Assert
        Assert.True(typeof(Exception).IsAssignableFrom(typeof(RelayException)));
        Assert.True(typeof(RelayException).IsAssignableFrom(typeof(HandlerNotFoundException)));
        Assert.True(typeof(RelayException).IsAssignableFrom(typeof(MultipleHandlersException)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RelayException_WithInvalidRequestType_ShouldStillWork(string? requestType)
    {
        // Act & Assert - Should not throw
        var exception = new RelayException(requestType!, null, "test message");
        Assert.Equal(requestType, exception.RequestType);
    }

    [Fact]
    public void RelayException_WithNullRequestType_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RelayException(null!, null, "test message"));
    }

    [Fact]
    public void ExceptionProperties_ShouldBeReadOnly()
    {
        // Arrange
        var exception = new RelayException("TestRequest", "TestHandler", "Test message");

        // Act & Assert
        var requestTypeProperty = typeof(RelayException).GetProperty(nameof(RelayException.RequestType));
        var handlerNameProperty = typeof(RelayException).GetProperty(nameof(RelayException.HandlerName));

        Assert.NotNull(requestTypeProperty);
        Assert.False(requestTypeProperty.CanWrite);
        Assert.NotNull(handlerNameProperty);
        Assert.False(handlerNameProperty.CanWrite);
    }
}
