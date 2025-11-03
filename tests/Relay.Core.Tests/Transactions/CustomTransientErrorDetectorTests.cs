using System;
using Xunit;
using Relay.Core.Transactions;

namespace Relay.Core.Tests.Transactions
{
    public class CustomTransientErrorDetectorTests
    {
        [Fact]
        public void Constructor_WithNullPredicate_ThrowsArgumentNullException()
        {
            // Arrange
            Func<Exception, bool> predicate = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CustomTransientErrorDetector(predicate));
        }

        [Fact]
        public void Constructor_WithValidPredicate_SetsPredicate()
        {
            // Arrange
            Func<Exception, bool> predicate = ex => true;

            // Act
            var detector = new CustomTransientErrorDetector(predicate);

            // Assert
            // We can't directly access _predicate, but we can verify it works by calling IsTransient
            var exception = new Exception();
            var result = detector.IsTransient(exception);
            
            Assert.True(result); // Should return true as per our predicate
        }

        [Fact]
        public void IsTransient_WithNullException_ReturnsFalse()
        {
            // Arrange
            Func<Exception, bool> predicate = ex => true; // This should not be called since exception is null
            var detector = new CustomTransientErrorDetector(predicate);

            // Act
            var result = detector.IsTransient(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsTransient_WithExceptionAndPredicateReturnsTrue_ReturnsTrue()
        {
            // Arrange
            var testException = new InvalidOperationException();
            Func<Exception, bool> predicate = ex => ex is InvalidOperationException;
            var detector = new CustomTransientErrorDetector(predicate);

            // Act
            var result = detector.IsTransient(testException);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsTransient_WithExceptionAndPredicateReturnsFalse_ReturnsFalse()
        {
            // Arrange
            var testException = new InvalidOperationException();
            Func<Exception, bool> predicate = ex => ex is ArgumentException;
            var detector = new CustomTransientErrorDetector(predicate);

            // Act
            var result = detector.IsTransient(testException);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsTransient_WithExceptionAndPredicateThatChecksProperties_ReturnsCorrectValue()
        {
            // Arrange
            var sqlException = new TestSqlException { Number = 1205 }; // Simulating SQL Server deadlock
            Func<Exception, bool> predicate = ex =>
            {
                if (ex is TestSqlException sqlEx)
                    return sqlEx.Number == 1205; // Deadlock
                
                return ex is TimeoutException;
            };
            var detector = new CustomTransientErrorDetector(predicate);

            // Act
            var result = detector.IsTransient(sqlException);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsTransient_WithTimeoutExceptionAndPredicateCheckingForTimeout_ReturnsTrue()
        {
            // Arrange
            var timeoutException = new TimeoutException();
            Func<Exception, bool> predicate = ex => ex is TimeoutException;
            var detector = new CustomTransientErrorDetector(predicate);

            // Act
            var result = detector.IsTransient(timeoutException);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsTransient_WithNonTransientException_ReturnsFalse()
        {
            // Arrange
            var normalException = new ArgumentException();
            Func<Exception, bool> predicate = ex => ex is TimeoutException; // Only checking for timeout
            var detector = new CustomTransientErrorDetector(predicate);

            // Act
            var result = detector.IsTransient(normalException);

            // Assert
            Assert.False(result);
        }
    }

    // Test class to simulate SQL exception with Number property for testing predicate logic
    internal class TestSqlException : Exception
    {
        public int Number { get; set; }
        public override string Message => "Test SQL Exception";
    }
}