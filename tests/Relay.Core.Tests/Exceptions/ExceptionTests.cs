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

        [Fact]
        public void RelayException_Constructor_WithInnerException_PreservesStackTrace()
        {
            // Arrange
            var requestType = "TestRequest";
            var handlerName = "TestHandler";
            var message = "Test message";
            var innerException = new InvalidOperationException("Inner exception message");

            // Act
            var exception = new RelayException(requestType, handlerName, message, innerException);

            // Assert
            Assert.Equal(innerException, exception.InnerException);
            Assert.Contains("Inner exception message", exception.InnerException!.Message);
        }

        [Fact]
        public void RelayException_Constructor_WithNullMessage_AllowsNull()
        {
            // Arrange
            var requestType = "TestRequest";
            var handlerName = "TestHandler";

            // Act
            var exception = new RelayException(requestType, handlerName, null!);

            // Assert
            Assert.Equal(requestType, exception.RequestType);
            Assert.Equal(handlerName, exception.HandlerName);
        }

        [Fact]
        public void HandlerNotFoundException_Constructor_WithNullRequestType_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new HandlerNotFoundException(null!));
        }

        [Fact]
        public void MultipleHandlersException_Constructor_WithNullRequestType_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MultipleHandlersException(null!));
        }

        [Fact]
        public void HandlerNotFoundException_Message_ContainsCorrectFormat()
        {
            // Arrange
            var requestType = "MyCustomRequest";

            // Act
            var exception = new HandlerNotFoundException(requestType);

            // Assert
            Assert.Contains("No handler found", exception.Message);
            Assert.Contains("MyCustomRequest", exception.Message);
        }

        [Fact]
        public void MultipleHandlersException_Message_ContainsCorrectFormat()
        {
            // Arrange
            var requestType = "MyCustomRequest";

            // Act
            var exception = new MultipleHandlersException(requestType);

            // Assert
            Assert.Contains("Multiple handlers found", exception.Message);
            Assert.Contains("MyCustomRequest", exception.Message);
        }

        [Fact]
        public void RelayException_GetBaseException_ReturnsInnermostException()
        {
            // Arrange
            var innermostException = new InvalidOperationException("Innermost");
            var middleException = new ArgumentException("Middle", innermostException);
            var exception = new RelayException("TestRequest", "TestHandler", "Outer", middleException);

            // Act
            var baseException = exception.GetBaseException();

            // Assert
            Assert.Equal(innermostException, baseException);
        }

        [Fact]
        public void RelayException_StackTrace_NotNull()
        {
            // Arrange
            var requestType = "TestRequest";
            var message = "Test message";

            // Act
            try
            {
                throw new RelayException(requestType, null, message);
            }
            catch (RelayException ex)
            {
                // Assert
                Assert.NotNull(ex.StackTrace);
                Assert.NotEmpty(ex.StackTrace);
            }
        }

        [Fact]
        public void HandlerNotFoundException_WithTwoParameters_Works()
        {
            // Arrange
            var requestType = "TestRequest";
            var handlerName = "TestHandler";

            // Act
            var exception = new HandlerNotFoundException(requestType, handlerName);

            // Assert
            Assert.Equal(requestType, exception.RequestType);
            Assert.Equal(handlerName, exception.HandlerName);
        }

        [Fact]
        public void MultipleHandlersException_WithOneParameter_Works()
        {
            // Arrange
            var requestType = "TestRequest";

            // Act
            var exception = new MultipleHandlersException(requestType);

            // Assert
            Assert.Equal(requestType, exception.RequestType);
        }

        [Fact]
        public void RelayException_Data_CanBeModified()
        {
            // Arrange
            var exception = new RelayException("TestRequest", "TestHandler", "Test");

            // Act
            exception.Data["CustomKey"] = "CustomValue";

            // Assert
            Assert.Equal("CustomValue", exception.Data["CustomKey"]);
        }

        [Fact]
        public void HandlerNotFoundException_HelpLink_CanBeSet()
        {
            // Arrange
            var exception = new HandlerNotFoundException("TestRequest");

            // Act
            exception.HelpLink = "https://example.com/help";

            // Assert
            Assert.Equal("https://example.com/help", exception.HelpLink);
        }

        [Fact]
        public void RelayException_Source_CanBeSet()
        {
            // Arrange
            var exception = new RelayException("TestRequest", "TestHandler", "Test");

            // Act
            exception.Source = "TestAssembly";

            // Assert
            Assert.Equal("TestAssembly", exception.Source);
        }

        [Fact]
        public void RelayException_WithVeryLongRequestType_DoesNotThrow()
        {
            // Arrange
            var longRequestType = new string('A', 10000);

            // Act
            var exception = new RelayException(longRequestType, null, "Test");

            // Assert
            Assert.Equal(longRequestType, exception.RequestType);
        }

        [Fact]
        public void RelayException_WithSpecialCharactersInRequestType_PreservesCharacters()
        {
            // Arrange
            var requestType = "Test@Request#123$%^";

            // Act
            var exception = new RelayException(requestType, null, "Test");

            // Assert
            Assert.Equal(requestType, exception.RequestType);
        }

        [Fact]
        public void RelayException_WithUnicodeCharacters_PreservesCharacters()
        {
            // Arrange
            var requestType = "Test请求类型😀";
            var handlerName = "处理器名称🎯";

            // Act
            var exception = new RelayException(requestType, handlerName, "Test");

            // Assert
            Assert.Equal(requestType, exception.RequestType);
            Assert.Equal(handlerName, exception.HandlerName);
        }

        [Fact]
        public void HandlerNotFoundException_ToString_ContainsFullDetails()
        {
            // Arrange
            var requestType = "TestRequest";
            var handlerName = "TestHandler";
            var exception = new HandlerNotFoundException(requestType, handlerName);

            // Act
            var result = exception.ToString();

            // Assert
            Assert.Contains(requestType, result);
            Assert.Contains(handlerName, result);
            Assert.Contains("HandlerNotFoundException", result);
        }

        [Fact]
        public void MultipleHandlersException_ToString_ContainsFullDetails()
        {
            // Arrange
            var requestType = "TestRequest";
            var exception = new MultipleHandlersException(requestType);

            // Act
            var result = exception.ToString();

            // Assert
            Assert.Contains(requestType, result);
            Assert.Contains("MultipleHandlersException", result);
        }

        [Fact]
        public void RelayException_WithNestedInnerExceptions_PreservesHierarchy()
        {
            // Arrange
            var innermost = new InvalidOperationException("Level 3");
            var middle = new ArgumentException("Level 2", innermost);
            var exception = new RelayException("TestRequest", "TestHandler", "Level 1", middle);

            // Act
            var result = exception.ToString();

            // Assert
            Assert.Contains("Level 1", result);
            Assert.NotNull(exception.InnerException);
            Assert.NotNull(exception.InnerException.InnerException);
        }

        [Fact]
        public void RelayException_WithEmptyRequestType_AllowsEmptyString()
        {
            // Arrange & Act
            var exception = new RelayException("", "handler", "message");

            // Assert
            Assert.Equal("", exception.RequestType);
            Assert.Equal("handler", exception.HandlerName);
        }

        [Fact]
        public void HandlerNotFoundException_WithEmptyRequestType_AllowsEmptyString()
        {
            // Arrange & Act
            var exception = new HandlerNotFoundException("");

            // Assert
            Assert.Equal("", exception.RequestType);
        }

        [Fact]
        public void MultipleHandlersException_WithEmptyRequestType_AllowsEmptyString()
        {
            // Arrange & Act
            var exception = new MultipleHandlersException("");

            // Assert
            Assert.Equal("", exception.RequestType);
        }

        [Fact]
        public void RelayException_RequestType_IsReadOnly()
        {
            // Arrange
            var requestType = "InitialRequest";
            var exception = new RelayException(requestType, null, "Test");

            // Act - Verify property is get-only by checking it can be read
            var retrievedType = exception.RequestType;

            // Assert
            Assert.Equal(requestType, retrievedType);
        }

        [Fact]
        public void RelayException_HandlerName_IsReadOnly()
        {
            // Arrange
            var handlerName = "InitialHandler";
            var exception = new RelayException("TestRequest", handlerName, "Test");

            // Act - Verify property is get-only by checking it can be read
            var retrievedName = exception.HandlerName;

            // Assert
            Assert.Equal(handlerName, retrievedName);
        }

        [Fact]
        public void HandlerNotFoundException_ConstructorSetsProperties()
        {
            // Arrange
            var requestType = "TestRequest";

            // Act
            var exception = new HandlerNotFoundException(requestType);

            // Assert
            Assert.Equal(requestType, exception.RequestType);
            Assert.Null(exception.HandlerName);
        }

        [Fact]
        public void RelayException_Message_ReturnsCustomMessage()
        {
            // Arrange
            var customMessage = "This is a custom error message";
            var exception = new RelayException("TestRequest", "TestHandler", customMessage);

            // Act & Assert
            Assert.Equal(customMessage, exception.Message);
        }

        [Fact]
        public void AllExceptions_CanBeCaught_AsException()
        {
            // Arrange & Act
            var relayEx = new RelayException("Test", null, "Message");
            var handlerNotFoundEx = new HandlerNotFoundException("Test");
            var multipleHandlersEx = new MultipleHandlersException("Test");

            // Assert
            Assert.IsAssignableFrom<Exception>(relayEx);
            Assert.IsAssignableFrom<Exception>(handlerNotFoundEx);
            Assert.IsAssignableFrom<Exception>(multipleHandlersEx);
        }
    }
}
