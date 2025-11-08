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

        [Fact]
        public void UnsupportedDatabaseFeatureException_Default_Constructor_Should_Have_Default_Message()
        {
            // Arrange & Act
            var exception = new UnsupportedDatabaseFeatureException();

            // Assert
            Assert.Equal("The requested transaction feature is not supported by the database provider.", exception.Message);
            Assert.Null(exception.DatabaseProvider);
            Assert.Null(exception.FeatureName);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void UnsupportedDatabaseFeatureException_Message_Constructor_Should_Set_Message()
        {
            // Arrange
            var message = "Custom error message";

            // Act
            var exception = new UnsupportedDatabaseFeatureException(message);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.DatabaseProvider);
            Assert.Null(exception.FeatureName);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void UnsupportedDatabaseFeatureException_Message_And_InnerException_Constructor_Should_Set_Both()
        {
            // Arrange
            var message = "Custom error message";
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new UnsupportedDatabaseFeatureException(message, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Same(innerException, exception.InnerException);
            Assert.Null(exception.DatabaseProvider);
            Assert.Null(exception.FeatureName);
        }

        [Fact]
        public void UnsupportedDatabaseFeatureException_DatabaseProvider_And_FeatureName_Constructor_Should_Build_Message_And_Set_Properties()
        {
            // Arrange
            var databaseProvider = "SQLite";
            var featureName = "DistributedTransactions";

            // Act
            var exception = new UnsupportedDatabaseFeatureException(databaseProvider, featureName);

            // Assert
            Assert.Equal(databaseProvider, exception.DatabaseProvider);
            Assert.Equal(featureName, exception.FeatureName);
            Assert.Null(exception.InnerException);
            Assert.Contains(databaseProvider, exception.Message);
            Assert.Contains(featureName, exception.Message);
            Assert.Contains("does not support", exception.Message);
        }

        [Fact]
        public void UnsupportedDatabaseFeatureException_DatabaseProvider_FeatureName_And_InnerException_Constructor_Should_Set_All()
        {
            // Arrange
            var databaseProvider = "MySQL";
            var featureName = "NestedSavepoints";
            var innerException = new NotSupportedException("Database limitation");

            // Act
            var exception = new UnsupportedDatabaseFeatureException(databaseProvider, featureName, innerException);

            // Assert
            Assert.Equal(databaseProvider, exception.DatabaseProvider);
            Assert.Equal(featureName, exception.FeatureName);
            Assert.Same(innerException, exception.InnerException);
            Assert.Contains(databaseProvider, exception.Message);
            Assert.Contains(featureName, exception.Message);
        }

        [Fact]
        public void UnsupportedDatabaseFeatureException_Custom_Message_DatabaseProvider_And_FeatureName_Constructor_Should_Set_Custom_Message_And_Properties()
        {
            // Arrange
            var message = "Custom unsupported feature message";
            var databaseProvider = "Oracle";
            var featureName = "SnapshotIsolation";

            // Act
            var exception = new UnsupportedDatabaseFeatureException(message, databaseProvider, featureName);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(databaseProvider, exception.DatabaseProvider);
            Assert.Equal(featureName, exception.FeatureName);
            Assert.Null(exception.InnerException);
        }


    }
}
