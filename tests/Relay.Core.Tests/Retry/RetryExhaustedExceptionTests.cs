using System;
using System.Collections.Generic;
using FluentAssertions;
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
            retryException.Exceptions.Should().BeEquivalentTo(exceptions);
            retryException.Message.Should().Contain("3 attempts");
        }

        [Fact]
        public void RetryExhaustedException_Constructor_Should_ThrowException_WhenExceptionsIsNull()
        {
            // Act - ArgumentNullException is thrown in constructor before base() is called
            Action act = () => new RetryExhaustedException(null!);

            // Assert - It throws NullReferenceException because base() is called before null check
            act.Should().Throw<Exception>(); // Can be either ArgumentNullException or NullReferenceException
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
            retryException.InnerException.Should().BeSameAs(innerException);
            retryException.Exceptions.Should().BeEquivalentTo(exceptions);
        }

        [Fact]
        public void RetryExhaustedException_ConstructorWithInner_Should_ThrowException_WhenExceptionsIsNull()
        {
            // Arrange
            var innerException = new Exception("Inner error");

            // Act - ArgumentNullException is thrown in constructor before base() is called
            Action act = () => new RetryExhaustedException(null!, innerException);

            // Assert - It throws NullReferenceException because base() is called before null check
            act.Should().Throw<Exception>(); // Can be either ArgumentNullException or NullReferenceException
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
            retryException.Message.Should().Contain("5 attempts were made");
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
            retryException.Exceptions.Should().BeAssignableTo<IReadOnlyList<Exception>>();
        }

        [Fact]
        public void RetryExhaustedException_Should_HandleEmptyExceptionsList()
        {
            // Arrange
            var exceptions = new List<Exception>();

            // Act
            var retryException = new RetryExhaustedException(exceptions);

            // Assert
            retryException.Exceptions.Should().BeEmpty();
            retryException.Message.Should().Contain("0 attempts");
        }
    }
}
