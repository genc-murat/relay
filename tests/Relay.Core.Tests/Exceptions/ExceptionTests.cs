using System;
using Xunit;
using Relay.Core;

namespace Relay.Core.Tests
{
    public class ExceptionTests
    {
        [Fact]
        public void RelayException_Constructor_SetsProperties()
        {
            // Arrange
            var requestType = "TestRequest";
            var handlerName = "TestHandler";
            var message = "Test message";
            var innerException = new InvalidOperationException("Inner");

            // Act
            var exception = new RelayException(requestType, handlerName, message, innerException);

            // Assert
            Assert.Equal(requestType, exception.RequestType);
            Assert.Equal(handlerName, exception.HandlerName);
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
        }

        [Fact]
        public void RelayException_Constructor_WithNullRequestType_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new RelayException(null!, "handler", "message"));
        }

        [Fact]
        public void RelayException_Constructor_WithNullHandlerName_AllowsNull()
        {
            // Arrange
            var requestType = "TestRequest";
            var message = "Test message";

            // Act
            var exception = new RelayException(requestType, null, message);

            // Assert
            Assert.Equal(requestType, exception.RequestType);
            Assert.Null(exception.HandlerName);
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void HandlerNotFoundException_Constructor_WithRequestType_SetsMessage()
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
        public void HandlerNotFoundException_Constructor_WithRequestTypeAndHandlerName_SetsMessage()
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
        public void MultipleHandlersException_Constructor_SetsMessage()
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
        public void RelayException_IsSerializable()
        {
            // Arrange
            var requestType = "TestRequest";
            var handlerName = "TestHandler";
            var message = "Test message";
            var exception = new RelayException(requestType, handlerName, message);

            // Act & Assert - Should not throw
            var serialized = exception.ToString();
            Assert.NotNull(serialized);
            Assert.Contains(message, serialized);
        }

        [Fact]
        public void HandlerNotFoundException_InheritsFromRelayException()
        {
            // Arrange
            var requestType = "TestRequest";
            var exception = new HandlerNotFoundException(requestType);

            // Act & Assert
            Assert.IsAssignableFrom<RelayException>(exception);
            Assert.IsAssignableFrom<Exception>(exception);
        }

        [Fact]
        public void MultipleHandlersException_InheritsFromRelayException()
        {
            // Arrange
            var requestType = "TestRequest";
            var exception = new MultipleHandlersException(requestType);

            // Act & Assert
            Assert.IsAssignableFrom<RelayException>(exception);
            Assert.IsAssignableFrom<Exception>(exception);
        }

        [Fact]
        public void RelayException_ToString_IncludesRequestType()
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
        public void RelayException_ToString_IncludesHandlerName()
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
        public void RelayException_ToString_WithEmptyHandlerName_DoesNotIncludeHandlerNameSection()
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
        public void RelayException_ToString_WithWhitespaceHandlerName_DoesNotIncludeHandlerNameSection()
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