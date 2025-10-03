using System;
using Xunit;
using Relay.Core;

namespace Relay.Core.Tests.Core
{
    public class ExceptionsTests
    {
        [Fact]
        public void RelayException_Constructor_ShouldSetProperties()
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
        public void RelayException_Constructor_WithInnerException_ShouldSetProperties()
        {
            // Arrange
            var requestType = "TestRequest";
            var handlerName = "TestHandler";
            var message = "Test error message";
            var innerException = new InvalidOperationException("Inner exception");

            // Act
            var exception = new RelayException(requestType, handlerName, message, innerException);

            // Assert
            Assert.Equal(requestType, exception.RequestType);
            Assert.Equal(handlerName, exception.HandlerName);
            Assert.Equal(message, exception.Message);
            Assert.Same(innerException, exception.InnerException);
        }

        [Fact]
        public void RelayException_Constructor_WithNullRequestType_ShouldThrowArgumentNullException()
        {
            // Arrange
            string? requestType = null;
            var handlerName = "TestHandler";
            var message = "Test error message";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new RelayException(requestType!, handlerName, message));
        }

        [Fact]
        public void RelayException_Constructor_WithNullHandlerName_ShouldBeAllowed()
        {
            // Arrange
            var requestType = "TestRequest";
            string? handlerName = null;
            var message = "Test error message";

            // Act
            var exception = new RelayException(requestType, handlerName, message);

            // Assert
            Assert.Equal(requestType, exception.RequestType);
            Assert.Null(exception.HandlerName);
        }

        [Fact]
        public void RelayException_ToString_ShouldIncludeRequestType()
        {
            // Arrange
            var requestType = "TestRequest";
            var message = "Test error message";
            var exception = new RelayException(requestType, null, message);

            // Act
            var result = exception.ToString();

            // Assert
            Assert.Contains("RequestType: TestRequest", result);
            Assert.Contains(message, result);
        }

        [Fact]
        public void RelayException_ToString_ShouldIncludeHandlerName()
        {
            // Arrange
            var requestType = "TestRequest";
            var handlerName = "TestHandler";
            var message = "Test error message";
            var exception = new RelayException(requestType, handlerName, message);

            // Act
            var result = exception.ToString();

            // Assert
            Assert.Contains("RequestType: TestRequest", result);
            Assert.Contains("HandlerName: TestHandler", result);
            Assert.Contains(message, result);
        }

        [Fact]
        public void HandlerNotFoundException_WithRequestTypeOnly_ShouldSetMessage()
        {
            // Arrange
            var requestType = "TestRequest";

            // Act
            var exception = new HandlerNotFoundException(requestType);

            // Assert
            Assert.Equal(requestType, exception.RequestType);
            Assert.Null(exception.HandlerName);
            Assert.Contains(requestType, exception.Message);
            Assert.Contains("No handler found", exception.Message);
        }

        [Fact]
        public void HandlerNotFoundException_WithHandlerName_ShouldSetMessage()
        {
            // Arrange
            var requestType = "TestRequest";
            var handlerName = "TestHandler";

            // Act
            var exception = new HandlerNotFoundException(requestType, handlerName);

            // Assert
            Assert.Equal(requestType, exception.RequestType);
            Assert.Equal(handlerName, exception.HandlerName);
            Assert.Contains(requestType, exception.Message);
            Assert.Contains(handlerName, exception.Message);
            Assert.Contains("No handler named", exception.Message);
        }

        [Fact]
        public void MultipleHandlersException_ShouldSetMessage()
        {
            // Arrange
            var requestType = "TestRequest";

            // Act
            var exception = new MultipleHandlersException(requestType);

            // Assert
            Assert.Equal(requestType, exception.RequestType);
            Assert.Null(exception.HandlerName);
            Assert.Contains(requestType, exception.Message);
            Assert.Contains("Multiple handlers found", exception.Message);
        }

        [Fact]
        public void HandlerNotFoundException_IsRelayException()
        {
            // Arrange
            var requestType = "TestRequest";

            // Act
            var exception = new HandlerNotFoundException(requestType);

            // Assert
            Assert.IsAssignableFrom<RelayException>(exception);
        }

        [Fact]
        public void MultipleHandlersException_IsRelayException()
        {
            // Arrange
            var requestType = "TestRequest";

            // Act
            var exception = new MultipleHandlersException(requestType);

            // Assert
            Assert.IsAssignableFrom<RelayException>(exception);
        }

        [Fact]
        public void RelayException_ToString_WithEmptyHandlerName_ShouldNotIncludeHandlerNameSection()
        {
            // Arrange
            var requestType = "TestRequest";
            var handlerName = "";
            var message = "Test error message";
            var exception = new RelayException(requestType, handlerName, message);

            // Act
            var result = exception.ToString();

            // Assert
            Assert.Contains("RequestType: TestRequest", result);
            Assert.DoesNotContain("HandlerName:", result);
        }

        [Fact]
        public void RelayException_ToString_WithWhitespaceHandlerName_ShouldNotIncludeHandlerNameSection()
        {
            // Arrange
            var requestType = "TestRequest";
            var handlerName = "   ";
            var message = "Test error message";
            var exception = new RelayException(requestType, handlerName, message);

            // Act
            var result = exception.ToString();

            // Assert
            Assert.Contains("RequestType: TestRequest", result);
            Assert.DoesNotContain("HandlerName:", result);
        }
    }
}
