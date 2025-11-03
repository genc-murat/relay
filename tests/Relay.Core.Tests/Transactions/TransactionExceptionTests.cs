using System;
using System.Data;
using Relay.Core.Transactions;
using Xunit;

namespace Relay.Core.Tests.Transactions
{
    /// <summary>
    /// Tests for transaction-related exceptions.
    /// </summary>
    public class TransactionExceptionTests
    {
        [Fact]
        public void TransactionConfigurationException_Should_Contain_Message()
        {
            // Arrange & Act
            var exception = new TransactionConfigurationException("Configuration error");

            // Assert
            Assert.Contains("Configuration error", exception.Message);
        }

        [Fact]
        public void TransactionConfigurationException_Should_Support_Inner_Exception()
        {
            // Arrange
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new TransactionConfigurationException("Configuration error", innerException);

            // Assert
            Assert.Same(innerException, exception.InnerException);
        }

        [Fact]
        public void TransactionTimeoutException_Should_Contain_Transaction_Info()
        {
            // Arrange
            var transactionId = "test-tx-123";
            var timeout = TimeSpan.FromSeconds(30);
            var elapsed = TimeSpan.FromSeconds(35);

            // Act
            var exception = new TransactionTimeoutException(transactionId, timeout, elapsed);

            // Assert
            Assert.Contains(transactionId, exception.Message);
            Assert.Equal(timeout, exception.Timeout);
            Assert.Equal(elapsed, exception.Elapsed);
        }

        [Fact]
        public void SavepointException_Should_Contain_Message()
        {
            // Arrange & Act
            var exception = new SavepointException("Savepoint error");

            // Assert
            Assert.Contains("Savepoint error", exception.Message);
        }

        [Fact]
        public void SavepointAlreadyExistsException_Should_Contain_Savepoint_Name()
        {
            // Arrange & Act
            var exception = new SavepointAlreadyExistsException("sp1");

            // Assert
            Assert.Contains("sp1", exception.Message);
        }

        [Fact]
        public void SavepointNotFoundException_Should_Contain_Savepoint_Name()
        {
            // Arrange & Act
            var exception = new SavepointNotFoundException("sp1");

            // Assert
            Assert.Contains("sp1", exception.Message);
        }

        [Fact]
        public void ReadOnlyTransactionViolationException_Should_Have_Message()
        {
            // Arrange & Act
            var exception = new ReadOnlyTransactionViolationException("Cannot modify data");

            // Assert
            Assert.Contains("Cannot modify data", exception.Message);
        }

        [Fact]
        public void DistributedTransactionException_Should_Have_Message()
        {
            // Arrange & Act
            var exception = new DistributedTransactionException("Distributed transaction failed");

            // Assert
            Assert.Contains("Distributed transaction failed", exception.Message);
        }

        [Fact]
        public void NestedTransactionException_Should_Have_Message()
        {
            // Arrange & Act
            var exception = new NestedTransactionException(
                "Nested transaction error",
                "tx-123",
                1,
                "TestRequest");

            // Assert
            Assert.Contains("Nested transaction error", exception.Message);
            Assert.Equal("tx-123", exception.TransactionId);
            Assert.Equal(1, exception.NestingLevel);
            Assert.Equal("TestRequest", exception.RequestType);
        }

        [Fact]
        public void TransactionRetryExhaustedException_Should_Contain_Retry_Info()
        {
            // Arrange
            var maxRetries = 3;
            var innerException = new TimeoutException("Timeout");

            // Act
            var exception = new TransactionRetryExhaustedException(maxRetries, innerException);

            // Assert
            Assert.Contains("3", exception.Message);
            Assert.Same(innerException, exception.InnerException);
        }
    }
}
