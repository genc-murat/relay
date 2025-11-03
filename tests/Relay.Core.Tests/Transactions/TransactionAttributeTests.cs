using System;
using System.Data;
using Relay.Core.Transactions;
using Xunit;

namespace Relay.Core.Tests.Transactions
{
    /// <summary>
    /// Tests for TransactionAttribute and mandatory isolation level support.
    /// </summary>
    public class TransactionAttributeTests
    {
        [Fact]
        public void TransactionAttribute_Should_Require_IsolationLevel()
        {
            // Act
            var attribute = new TransactionAttribute(IsolationLevel.ReadCommitted);

            // Assert
            Assert.Equal(IsolationLevel.ReadCommitted, attribute.IsolationLevel);
        }

        [Fact]
        public void TransactionAttribute_Should_Reject_Unspecified_IsolationLevel()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new TransactionAttribute(IsolationLevel.Unspecified));
        }

        [Theory]
        [InlineData(IsolationLevel.ReadUncommitted)]
        [InlineData(IsolationLevel.ReadCommitted)]
        [InlineData(IsolationLevel.RepeatableRead)]
        [InlineData(IsolationLevel.Serializable)]
        [InlineData(IsolationLevel.Snapshot)]
        public void TransactionAttribute_Should_Support_All_Isolation_Levels(IsolationLevel level)
        {
            // Act
            var attribute = new TransactionAttribute(level);

            // Assert
            Assert.Equal(level, attribute.IsolationLevel);
        }

        [Fact]
        public void TransactionAttribute_Should_Have_Default_Timeout()
        {
            // Arrange & Act
            var attribute = new TransactionAttribute(IsolationLevel.ReadCommitted);

            // Assert
            Assert.Equal(30, attribute.TimeoutSeconds);
        }

        [Fact]
        public void TransactionAttribute_Should_Allow_Custom_Timeout()
        {
            // Arrange & Act
            var attribute = new TransactionAttribute(IsolationLevel.ReadCommitted)
            {
                TimeoutSeconds = 60
            };

            // Assert
            Assert.Equal(60, attribute.TimeoutSeconds);
        }

        [Fact]
        public void TransactionAttribute_Should_Default_IsReadOnly_To_False()
        {
            // Arrange & Act
            var attribute = new TransactionAttribute(IsolationLevel.ReadCommitted);

            // Assert
            Assert.False(attribute.IsReadOnly);
        }

        [Fact]
        public void TransactionAttribute_Should_Support_ReadOnly_Flag()
        {
            // Arrange & Act
            var attribute = new TransactionAttribute(IsolationLevel.ReadCommitted)
            {
                IsReadOnly = true
            };

            // Assert
            Assert.True(attribute.IsReadOnly);
        }

        [Fact]
        public void TransactionAttribute_Should_Default_UseDistributedTransaction_To_False()
        {
            // Arrange & Act
            var attribute = new TransactionAttribute(IsolationLevel.ReadCommitted);

            // Assert
            Assert.False(attribute.UseDistributedTransaction);
        }

        [Fact]
        public void TransactionAttribute_Should_Support_DistributedTransaction_Flag()
        {
            // Arrange & Act
            var attribute = new TransactionAttribute(IsolationLevel.ReadCommitted)
            {
                UseDistributedTransaction = true
            };

            // Assert
            Assert.True(attribute.UseDistributedTransaction);
        }

        [Fact]
        public void TransactionRetryAttribute_Should_Have_Default_Values()
        {
            // Arrange & Act
            var attribute = new TransactionRetryAttribute();

            // Assert
            Assert.Equal(3, attribute.MaxRetries);
            Assert.Equal(100, attribute.InitialDelayMs);
            Assert.Equal(RetryStrategy.ExponentialBackoff, attribute.Strategy);
        }

        [Fact]
        public void TransactionRetryAttribute_Should_Allow_Custom_Values()
        {
            // Arrange & Act
            var attribute = new TransactionRetryAttribute
            {
                MaxRetries = 5,
                InitialDelayMs = 200,
                Strategy = RetryStrategy.Linear
            };

            // Assert
            Assert.Equal(5, attribute.MaxRetries);
            Assert.Equal(200, attribute.InitialDelayMs);
            Assert.Equal(RetryStrategy.Linear, attribute.Strategy);
        }
    }
}
