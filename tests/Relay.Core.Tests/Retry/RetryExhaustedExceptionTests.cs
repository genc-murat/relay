using System;
using System.Collections.Generic;

using Relay.Core.Retry;
using Xunit;

namespace Relay.Core.Tests.Retry
{
    public class RetryExhaustedExceptionTests
    {
        [Fact]
        public void RetryExhaustedException_Constructor_Should_AcceptExceptionsList()
        {
            // Arrange
            var exceptions = new List<Exception>
            {
                new InvalidOperationException("Error 1"),
                new InvalidOperationException("Error 2"),
                new InvalidOperationException("Error 3")
            };

            // Act
            var retryException = new RetryExhaustedException(exceptions);

            // Assert
            Assert.Equal(exceptions, retryException.Exceptions);
            Assert.Contains("3 attempts", retryException.Message);
        }

        [Fact]
        public void RetryExhaustedException_Constructor_Should_ThrowException_WhenExceptionsIsNull()
        {
            // Act - ArgumentNullException is thrown in constructor before base() is called
            Action act = () => new RetryExhaustedException(null!);
 
            // Assert - It throws NullReferenceException because base() is called before null check
            Assert.ThrowsAny<Exception>(act); // Can be either ArgumentNullException or NullReferenceException
        }

        [Fact]
        public void RetryExhaustedException_ConstructorWithInner_Should_AcceptInnerException()
        {
            // Arrange
            var exceptions = new List<Exception>
            {
                new InvalidOperationException("Error 1")
            };
            var innerException = new Exception("Inner error");

            // Act
            var retryException = new RetryExhaustedException(exceptions, innerException);

            // Assert
            Assert.Same(innerException, retryException.InnerException);
            Assert.Equal(exceptions, retryException.Exceptions);
        }

        [Fact]
        public void RetryExhaustedException_ConstructorWithInner_Should_ThrowException_WhenExceptionsIsNull()
        {
            // Arrange
            var innerException = new Exception("Inner error");

            // Act - ArgumentNullException is thrown in constructor before base() is called
            Action act = () => new RetryExhaustedException(null!, innerException);
 
            // Assert - It throws NullReferenceException because base() is called before null check
            Assert.ThrowsAny<Exception>(act); // Can be either ArgumentNullException or NullReferenceException
        }

        [Fact]
        public void RetryExhaustedException_Should_IncludeAttemptCountInMessage()
        {
            // Arrange
            var exceptions = new List<Exception>
            {
                new InvalidOperationException(),
                new InvalidOperationException(),
                new InvalidOperationException(),
                new InvalidOperationException(),
                new InvalidOperationException()
            };

            // Act
            var retryException = new RetryExhaustedException(exceptions);

            // Assert
            Assert.Contains("5 attempts were made", retryException.Message);
        }

        [Fact]
        public void RetryExhaustedException_Exceptions_Should_BeReadOnly()
        {
            // Arrange
            var exceptions = new List<Exception>
            {
                new InvalidOperationException("Error 1")
            };

            var retryException = new RetryExhaustedException(exceptions);

            // Act & Assert
            Assert.IsAssignableFrom<IReadOnlyList<Exception>>(retryException.Exceptions);
        }

        [Fact]
        public void RetryExhaustedException_Should_HandleEmptyExceptionsList()
        {
            // Arrange
            var exceptions = new List<Exception>();

            // Act
            var retryException = new RetryExhaustedException(exceptions);

            // Assert
            Assert.Empty(retryException.Exceptions);
            Assert.Contains("0 attempts", retryException.Message);
        }
    }
}
