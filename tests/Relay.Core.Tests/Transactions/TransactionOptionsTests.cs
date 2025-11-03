using System;
using Relay.Core.Transactions;
using Xunit;

namespace Relay.Core.Tests.Transactions
{
    /// <summary>
    /// Tests for TransactionOptions and TransactionRetryPolicy.
    /// </summary>
    public class TransactionOptionsTests
    {
        [Fact]
        public void TransactionOptions_Should_Have_Default_Timeout()
        {
            // Arrange & Act
            var options = new TransactionOptions();

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(30), options.DefaultTimeout);
        }

        [Fact]
        public void TransactionOptions_Should_Allow_Custom_Default_Timeout()
        {
            // Arrange & Act
            var options = new TransactionOptions
            {
                DefaultTimeout = TimeSpan.FromMinutes(2)
            };

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(2), options.DefaultTimeout);
        }

        [Fact]
        public void TransactionOptions_Should_Default_RequireExplicitTransactionAttribute_To_True()
        {
            // Arrange & Act
            var options = new TransactionOptions();

            // Assert
            Assert.True(options.RequireExplicitTransactionAttribute);
        }

        [Fact]
        public void TransactionRetryPolicy_Should_Have_Default_Values()
        {
            // Arrange & Act
            var policy = new TransactionRetryPolicy();

            // Assert
            Assert.Equal(3, policy.MaxRetries);
            Assert.Equal(TimeSpan.FromMilliseconds(100), policy.InitialDelay);
            Assert.Equal(RetryStrategy.ExponentialBackoff, policy.Strategy);
        }

        [Fact]
        public void TransactionRetryPolicy_Should_Allow_Custom_Values()
        {
            // Arrange & Act
            var policy = new TransactionRetryPolicy
            {
                MaxRetries = 5,
                InitialDelay = TimeSpan.FromMilliseconds(200),
                Strategy = RetryStrategy.Linear
            };

            // Assert
            Assert.Equal(5, policy.MaxRetries);
            Assert.Equal(TimeSpan.FromMilliseconds(200), policy.InitialDelay);
            Assert.Equal(RetryStrategy.Linear, policy.Strategy);
        }

        [Fact]
        public void TransactionRetryPolicy_Should_Support_Custom_Retry_Predicate()
        {
            // Arrange
            var policy = new TransactionRetryPolicy
            {
                ShouldRetry = ex => ex is InvalidOperationException
            };

            // Act
            var shouldRetry1 = policy.ShouldRetry?.Invoke(new InvalidOperationException());
            var shouldRetry2 = policy.ShouldRetry?.Invoke(new ArgumentException());

            // Assert
            Assert.True(shouldRetry1);
            Assert.False(shouldRetry2);
        }

        [Fact]
        public void TransactionMetrics_Should_Have_All_Properties()
        {
            // Arrange & Act
            var metrics = new TransactionMetrics
            {
                TotalTransactions = 100,
                SuccessfulTransactions = 90,
                FailedTransactions = 10,
                RolledBackTransactions = 5,
                TimeoutTransactions = 2,
                AverageDurationMs = 150.5
            };

            // Assert
            Assert.Equal(100, metrics.TotalTransactions);
            Assert.Equal(90, metrics.SuccessfulTransactions);
            Assert.Equal(10, metrics.FailedTransactions);
            Assert.Equal(5, metrics.RolledBackTransactions);
            Assert.Equal(2, metrics.TimeoutTransactions);
            Assert.Equal(150.5, metrics.AverageDurationMs);
        }

        [Fact]
        public void TransactionEventContext_Should_Have_All_Properties()
        {
            // Arrange & Act
            var context = new TransactionEventContext
            {
                TransactionId = "test-tx-123",
                RequestType = "CreateOrderCommand",
                IsolationLevel = System.Data.IsolationLevel.Serializable,
                NestingLevel = 2,
                Timestamp = DateTime.UtcNow,
                Metadata = new System.Collections.Generic.Dictionary<string, object> { { "key", "value" } }
            };

            // Assert
            Assert.Equal("test-tx-123", context.TransactionId);
            Assert.Equal("CreateOrderCommand", context.RequestType);
            Assert.Equal(System.Data.IsolationLevel.Serializable, context.IsolationLevel);
            Assert.Equal(2, context.NestingLevel);
            Assert.NotNull(context.Metadata);
            Assert.Single(context.Metadata);
        }
    }
}
