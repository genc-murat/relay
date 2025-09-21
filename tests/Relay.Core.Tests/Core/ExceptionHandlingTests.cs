using System;
using FluentAssertions;
using Xunit;
using Relay.Core;

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
        exception.RequestType.Should().Be(requestType);
        exception.HandlerName.Should().Be(handlerName);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
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
        exception.RequestType.Should().Be(requestType);
        exception.HandlerName.Should().Be(handlerName);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
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
        exception.RequestType.Should().Be(requestType);
        exception.HandlerName.Should().BeNull();
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void HandlerNotFoundException_ShouldSetCorrectMessage()
    {
        // Arrange
        var requestType = "TestRequest";

        // Act
        var exception = new HandlerNotFoundException(requestType);

        // Assert
        exception.RequestType.Should().Be(requestType);
        exception.HandlerName.Should().BeNull();
        exception.Message.Should().Be($"No handler found for request type '{requestType}'");
        exception.InnerException.Should().BeNull();
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
        exception.RequestType.Should().Be(requestType);
        exception.HandlerName.Should().Be(handlerName);
        exception.Message.Should().Be($"No handler named '{handlerName}' found for request type '{requestType}'");
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void HandlerNotFoundException_ShouldInheritFromRelayException()
    {
        // Arrange & Act
        var exception = new HandlerNotFoundException("TestRequest");

        // Assert
        exception.Should().BeAssignableTo<RelayException>();
    }

    [Fact]
    public void MultipleHandlersException_ShouldSetCorrectMessage()
    {
        // Arrange
        var requestType = "TestRequest";

        // Act
        var exception = new MultipleHandlersException(requestType);

        // Assert
        exception.RequestType.Should().Be(requestType);
        exception.HandlerName.Should().BeNull();
        exception.Message.Should().Be($"Multiple handlers found for request type '{requestType}'. Use named handlers or ensure only one handler is registered.");
    }

    [Fact]
    public void MultipleHandlersException_ShouldInheritFromRelayException()
    {
        // Arrange & Act
        var exception = new MultipleHandlersException("TestRequest");

        // Assert
        exception.Should().BeAssignableTo<RelayException>();
    }



    [Fact]
    public void ExceptionSerialization_ShouldPreserveProperties()
    {
        // Arrange
        var originalException = new RelayException("TestRequest", "TestHandler", "Test message", new ArgumentException("Inner"));

        // Act - Simulate serialization/deserialization
        var serialized = originalException.ToString();

        // Assert
        serialized.Should().Contain("TestRequest");
        serialized.Should().Contain("TestHandler");
        serialized.Should().Contain("Test message");
    }

    [Fact]
    public void ExceptionHierarchy_ShouldBeCorrect()
    {
        // Assert
        typeof(RelayException).Should().BeDerivedFrom<Exception>();
        typeof(HandlerNotFoundException).Should().BeDerivedFrom<RelayException>();
        typeof(MultipleHandlersException).Should().BeDerivedFrom<RelayException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RelayException_WithInvalidRequestType_ShouldStillWork(string? requestType)
    {
        // Act & Assert - Should not throw
        var exception = new RelayException(requestType!, null, "test message");
        exception.RequestType.Should().Be(requestType);
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

        requestTypeProperty!.CanWrite.Should().BeFalse();
        handlerNameProperty!.CanWrite.Should().BeFalse();
    }
}