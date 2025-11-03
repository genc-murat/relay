using System;
using System.Data.Common;
using Xunit;
using Relay.Core.Transactions;

namespace Relay.Core.Tests.Transactions
{
    public class DefaultTransientErrorDetectorTests
    {
        private readonly DefaultTransientErrorDetector _detector;

        public DefaultTransientErrorDetectorTests()
        {
            _detector = new DefaultTransientErrorDetector();
        }

        [Fact]
        public void IsTransient_WithNullException_ReturnsFalse()
        {
            // Act
            var result = _detector.IsTransient(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsTransient_WithTimeoutException_ReturnsTrue()
        {
            // Arrange
            var exception = new TimeoutException();

            // Act
            var result = _detector.IsTransient(exception);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsTransient_WithMessageContainingTransientPattern_ReturnsTrue()
        {
            // Test each pattern in the array
            var patterns = new[]
            {
                "deadlock", "timeout", "connection", "network", 
                "unavailable", "transient", "temporary", "lock",
                "could not open", "transport-level", "communication link",
                "broken pipe", "connection reset", "connection refused",
                "host not found", "no route to host"
            };

            foreach (var pattern in patterns)
            {
                // Arrange
                var exception = new Exception($"This is a {pattern} error");

                // Act
                var result = _detector.IsTransient(exception);

                // Assert
                Assert.True(result, $"Pattern '{pattern}' should be detected as transient");
            }
        }

        [Fact]
        public void IsTransient_WithMessageNotContainingTransientPattern_ReturnsFalse()
        {
            // Arrange
            var exception = new Exception("This is a regular error");

            // Act
            var result = _detector.IsTransient(exception);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsTransient_WithDbExceptionContainingTransientPattern_ReturnsTrue()
        {
            // Arrange
            var dbException = new TestDbException("Database deadlock occurred");

            // Act
            var result = _detector.IsTransient(dbException);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsTransient_WithDbExceptionNotContainingTransientPattern_ReturnsFalse()
        {
            // Arrange
            var dbException = new TestDbException("Database constraint violation");

            // Act
            var result = _detector.IsTransient(dbException);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsTransient_WithInvalidOperationExceptionContainingConnectionPattern_ReturnsTrue()
        {
            // Arrange
            var exception = new InvalidOperationException("Connection timeout occurred");

            // Act
            var result = _detector.IsTransient(exception);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsTransient_WithInvalidOperationExceptionContainingTimeoutPattern_ReturnsTrue()
        {
            // Arrange
            var exception = new InvalidOperationException("Timeout while processing");

            // Act
            var result = _detector.IsTransient(exception);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsTransient_WithInvalidOperationExceptionNotContainingPattern_ReturnsFalse()
        {
            // Arrange
            var exception = new InvalidOperationException("Invalid operation");

            // Act
            var result = _detector.IsTransient(exception);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsTransient_WithInnerExceptionContainingTransientPattern_ReturnsTrue()
        {
            // Arrange
            var innerException = new Exception("timeout occurred");
            var outerException = new Exception("Outer exception", innerException);

            // Act
            var result = _detector.IsTransient(outerException);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsTransient_WithInnerExceptionNotContainingTransientPattern_ReturnsFalse()
        {
            // Arrange
            var innerException = new Exception("regular error");
            var outerException = new Exception("Outer exception", innerException);

            // Act
            var result = _detector.IsTransient(outerException);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsTransient_WithMultipleNestedInnerExceptions_ReturnsTrueWhenInnermostContainsPattern()
        {
            // Arrange
            var innermostException = new Exception("deadlock detected");
            var middleException = new Exception("Middle exception", innermostException);
            var outerException = new Exception("Outer exception", middleException);

            // Act
            var result = _detector.IsTransient(outerException);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsTransient_WithDbExceptionTimeoutPattern_ReturnsTrue()
        {
            // Arrange
            var dbException = new TestDbException("Connection timeout");

            // Act
            var result = _detector.IsTransient(dbException);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsTransient_WithDbExceptionNetworkPattern_ReturnsTrue()
        {
            // Arrange
            var dbException = new TestDbException("Network error occurred");

            // Act
            var result = _detector.IsTransient(dbException);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsTransient_WithDbExceptionUnavailablePattern_ReturnsTrue()
        {
            // Arrange
            var dbException = new TestDbException("Database unavailable");

            // Act
            var result = _detector.IsTransient(dbException);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsTransient_WithDbExceptionWithoutTransientPattern_ReturnsFalse()
        {
            // Arrange
            var dbException = new TestDbException("Syntax error");

            // Act
            var result = _detector.IsTransient(dbException);

            // Assert
            Assert.False(result);
        }
    }

    // Test class to simulate DbException since it's abstract
    internal class TestDbException : DbException
    {
        public TestDbException(string message) : base(message) { }
    }
}