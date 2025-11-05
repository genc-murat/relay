using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Relay.Core.Validation.Exceptions;

namespace Relay.Core.Tests.Validation
{
    public class ValidationExceptionTests
    {
        [Fact]
        public void Constructor_Should_Set_RequestType_And_Errors()
        {
            // Arrange
            var requestType = typeof(string);
            var errors = new[] { "Error 1", "Error 2" };

            // Act
            var exception = new ValidationException(requestType, errors);

            // Assert
            Assert.Equal(requestType, exception.RequestType);
            Assert.Equal(errors, exception.Errors);
        }

        [Fact]
        public void Constructor_Should_Format_Message_Correctly()
        {
            // Arrange
            var requestType = typeof(TestRequest);
            var errors = new[] { "Name is required", "Age must be positive" };

            // Act
            var exception = new ValidationException(requestType, errors);

            // Assert
            Assert.Contains("Validation failed for TestRequest", exception.Message);
            Assert.Contains("Name is required", exception.Message);
            Assert.Contains("Age must be positive", exception.Message);
        }

        [Fact]
        public void Constructor_With_InnerException_Should_Set_InnerException()
        {
            // Arrange
            var requestType = typeof(string);
            var errors = new[] { "Error 1" };
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new ValidationException(requestType, errors, innerException);

            // Assert
            Assert.Equal(requestType, exception.RequestType);
            Assert.Equal(errors, exception.Errors);
            Assert.Equal(innerException, exception.InnerException);
        }

        [Fact]
        public void Constructor_With_InnerException_Should_Format_Message_Correctly()
        {
            // Arrange
            var requestType = typeof(TestRequest);
            var errors = new[] { "Validation error" };
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new ValidationException(requestType, errors, innerException);

            // Assert
            Assert.Contains("Validation failed for TestRequest", exception.Message);
            Assert.Contains("Validation error", exception.Message);
            Assert.Equal(innerException, exception.InnerException);
        }

        [Fact]
        public void Constructor_Should_Throw_ArgumentNullException_When_RequestType_Is_Null()
        {
            // Arrange
            var errors = new[] { "Error 1" };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ValidationException(null!, errors));
        }

        [Fact]
        public void Constructor_Should_Throw_ArgumentNullException_When_Errors_Is_Null()
        {
            // Arrange
            var requestType = typeof(string);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ValidationException(requestType, null!));
        }

        [Fact]
        public void Constructor_With_InnerException_Should_Throw_ArgumentNullException_When_RequestType_Is_Null()
        {
            // Arrange
            var errors = new[] { "Error 1" };
            var innerException = new InvalidOperationException();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ValidationException(null!, errors, innerException));
        }

        [Fact]
        public void Constructor_With_InnerException_Should_Throw_ArgumentNullException_When_Errors_Is_Null()
        {
            // Arrange
            var requestType = typeof(string);
            var innerException = new InvalidOperationException();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ValidationException(requestType, null!, innerException));
        }

        [Fact]
        public void Errors_Property_Should_Return_Same_Collection()
        {
            // Arrange
            var errors = new List<string> { "Error 1", "Error 2" };
            var exception = new ValidationException(typeof(string), errors);

            // Act
            var returnedErrors = exception.Errors;

            // Assert
            Assert.Same(errors, returnedErrors);
        }

        [Fact]
        public void RequestType_Property_Should_Return_Correct_Type()
        {
            // Arrange
            var requestType = typeof(int);
            var exception = new ValidationException(requestType, new[] { "Error" });

            // Act
            var returnedType = exception.RequestType;

            // Assert
            Assert.Equal(requestType, returnedType);
        }

        [Fact]
        public void Should_Handle_Empty_Errors_Collection()
        {
            // Arrange
            var requestType = typeof(string);
            var errors = Array.Empty<string>();

            // Act
            var exception = new ValidationException(requestType, errors);

            // Assert
            Assert.Equal(requestType, exception.RequestType);
            Assert.Empty(exception.Errors);
            Assert.Contains("Validation failed for String", exception.Message);
        }

        [Fact]
        public void Should_Handle_Single_Error()
        {
            // Arrange
            var requestType = typeof(TestRequest);
            var errors = new[] { "Single error" };

            // Act
            var exception = new ValidationException(requestType, errors);

            // Assert
            Assert.Equal(requestType, exception.RequestType);
            Assert.Single(exception.Errors);
            Assert.Equal("Single error", exception.Errors.First());
            Assert.Contains("Single error", exception.Message);
        }

        private class TestRequest { }
    }
}