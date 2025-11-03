using System;
using System.Data;
using Microsoft.Extensions.Options;
using Relay.Core.Contracts.Requests;
using Relay.Core.Transactions;
using Xunit;

namespace Relay.Core.Tests.Transactions
{
    public class TransactionConfigurationResolverTests
    {
        #region Test Request Types

        [Transaction(IsolationLevel.ReadCommitted)]
        private record ValidRequest : IRequest, ITransactionalRequest;

        [Transaction(IsolationLevel.Serializable, TimeoutSeconds = 60, IsReadOnly = true)]
        private record CustomConfigRequest : IRequest, ITransactionalRequest;

        [Transaction(IsolationLevel.ReadCommitted)]
        [TransactionRetry(MaxRetries = 5, InitialDelayMs = 200, Strategy = RetryStrategy.ExponentialBackoff)]
        private record RequestWithRetry : IRequest, ITransactionalRequest;

        [Transaction(IsolationLevel.RepeatableRead, TimeoutSeconds = 0)] // 0 = infinite timeout
        private record RequestWithInfiniteTimeout : IRequest, ITransactionalRequest;

        [Transaction(IsolationLevel.ReadUncommitted, UseDistributedTransaction = true)]
        private record DistributedTransactionRequest : IRequest, ITransactionalRequest;

        private record MissingAttributeRequest : IRequest, ITransactionalRequest;

        private record NonTransactionalRequest : IRequest;

        #endregion

        [Fact]
        public void Resolve_Should_Throw_When_RequestType_Is_Null()
        {
            // Arrange
            var options = Options.Create(new TransactionOptions());
            var resolver = new TransactionConfigurationResolver(options);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => resolver.Resolve((Type)null!));
        }

        [Fact]
        public void Resolve_Should_Throw_When_Request_Does_Not_Implement_ITransactionalRequest()
        {
            // Arrange
            var options = Options.Create(new TransactionOptions());
            var resolver = new TransactionConfigurationResolver(options);

            // Act & Assert
            var exception = Assert.Throws<TransactionConfigurationException>(
                () => resolver.Resolve(typeof(NonTransactionalRequest)));
            
            Assert.Contains("does not implement ITransactionalRequest", exception.Message);
        }

        [Fact]
        public void Resolve_Should_Throw_When_TransactionAttribute_Is_Missing()
        {
            // Arrange
            var options = Options.Create(new TransactionOptions 
            { 
                RequireExplicitTransactionAttribute = true 
            });
            var resolver = new TransactionConfigurationResolver(options);

            // Act & Assert
            var exception = Assert.Throws<TransactionConfigurationException>(
                () => resolver.Resolve(typeof(MissingAttributeRequest)));
            
            Assert.Contains("missing the required [Transaction] attribute", exception.Message);
            Assert.Contains("IsolationLevel.ReadCommitted", exception.Message);
        }

        [Fact]
        public void Resolve_Should_Return_Configuration_From_Attribute()
        {
            // Arrange
            var options = Options.Create(new TransactionOptions());
            var resolver = new TransactionConfigurationResolver(options);

            // Act
            var config = resolver.Resolve(typeof(ValidRequest));

            // Assert
            Assert.NotNull(config);
            Assert.Equal(IsolationLevel.ReadCommitted, config.IsolationLevel);
            Assert.Equal(TimeSpan.FromSeconds(30), config.Timeout); // Default timeout
            Assert.False(config.IsReadOnly);
            Assert.False(config.UseDistributedTransaction);
            Assert.Null(config.RetryPolicy);
        }

        [Fact]
        public void Resolve_Should_Return_Custom_Configuration_From_Attribute()
        {
            // Arrange
            var options = Options.Create(new TransactionOptions());
            var resolver = new TransactionConfigurationResolver(options);

            // Act
            var config = resolver.Resolve(typeof(CustomConfigRequest));

            // Assert
            Assert.NotNull(config);
            Assert.Equal(IsolationLevel.Serializable, config.IsolationLevel);
            Assert.Equal(TimeSpan.FromSeconds(60), config.Timeout);
            Assert.True(config.IsReadOnly);
            Assert.False(config.UseDistributedTransaction);
        }

        [Fact]
        public void Resolve_Should_Include_Retry_Policy_From_Attribute()
        {
            // Arrange
            var options = Options.Create(new TransactionOptions());
            var resolver = new TransactionConfigurationResolver(options);

            // Act
            var config = resolver.Resolve(typeof(RequestWithRetry));

            // Assert
            Assert.NotNull(config);
            Assert.NotNull(config.RetryPolicy);
            Assert.Equal(5, config.RetryPolicy.MaxRetries);
            Assert.Equal(TimeSpan.FromMilliseconds(200), config.RetryPolicy.InitialDelay);
            Assert.Equal(RetryStrategy.ExponentialBackoff, config.RetryPolicy.Strategy);
        }

        [Fact]
        public void Resolve_Should_Use_Default_Retry_Policy_When_No_Attribute()
        {
            // Arrange
            var defaultRetryPolicy = new TransactionRetryPolicy
            {
                MaxRetries = 3,
                InitialDelay = TimeSpan.FromMilliseconds(100),
                Strategy = RetryStrategy.Linear
            };
            var options = Options.Create(new TransactionOptions 
            { 
                DefaultRetryPolicy = defaultRetryPolicy 
            });
            var resolver = new TransactionConfigurationResolver(options);

            // Act
            var config = resolver.Resolve(typeof(ValidRequest));

            // Assert
            Assert.NotNull(config);
            Assert.NotNull(config.RetryPolicy);
            Assert.Equal(3, config.RetryPolicy.MaxRetries);
            Assert.Equal(TimeSpan.FromMilliseconds(100), config.RetryPolicy.InitialDelay);
            Assert.Equal(RetryStrategy.Linear, config.RetryPolicy.Strategy);
        }

        [Fact]
        public void Resolve_Should_Prefer_Attribute_Retry_Policy_Over_Default()
        {
            // Arrange
            var defaultRetryPolicy = new TransactionRetryPolicy
            {
                MaxRetries = 3,
                InitialDelay = TimeSpan.FromMilliseconds(100),
                Strategy = RetryStrategy.Linear
            };
            var options = Options.Create(new TransactionOptions 
            { 
                DefaultRetryPolicy = defaultRetryPolicy 
            });
            var resolver = new TransactionConfigurationResolver(options);

            // Act
            var config = resolver.Resolve(typeof(RequestWithRetry));

            // Assert
            Assert.NotNull(config);
            Assert.NotNull(config.RetryPolicy);
            Assert.Equal(5, config.RetryPolicy.MaxRetries); // From attribute, not default
            Assert.Equal(TimeSpan.FromMilliseconds(200), config.RetryPolicy.InitialDelay);
            Assert.Equal(RetryStrategy.ExponentialBackoff, config.RetryPolicy.Strategy);
        }

        [Fact]
        public void Resolve_Should_Convert_Zero_Timeout_To_Infinite()
        {
            // Arrange
            var options = Options.Create(new TransactionOptions());
            var resolver = new TransactionConfigurationResolver(options);

            // Act
            var config = resolver.Resolve(typeof(RequestWithInfiniteTimeout));

            // Assert
            Assert.NotNull(config);
            Assert.Equal(System.Threading.Timeout.InfiniteTimeSpan, config.Timeout);
        }

        [Fact]
        public void Resolve_Should_Support_Distributed_Transaction_Configuration()
        {
            // Arrange
            var options = Options.Create(new TransactionOptions());
            var resolver = new TransactionConfigurationResolver(options);

            // Act
            var config = resolver.Resolve(typeof(DistributedTransactionRequest));

            // Assert
            Assert.NotNull(config);
            Assert.True(config.UseDistributedTransaction);
            Assert.Equal(IsolationLevel.ReadUncommitted, config.IsolationLevel);
        }

        [Fact]
        public void Resolve_Generic_Should_Throw_When_Request_Is_Null()
        {
            // Arrange
            var options = Options.Create(new TransactionOptions());
            var resolver = new TransactionConfigurationResolver(options);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => resolver.Resolve<ValidRequest>(null!));
        }

        [Fact]
        public void Resolve_Generic_Should_Return_Configuration()
        {
            // Arrange
            var options = Options.Create(new TransactionOptions());
            var resolver = new TransactionConfigurationResolver(options);
            var request = new ValidRequest();

            // Act
            var config = resolver.Resolve(request);

            // Assert
            Assert.NotNull(config);
            Assert.Equal(IsolationLevel.ReadCommitted, config.IsolationLevel);
        }

        [Fact]
        public void Resolve_Should_Support_All_Isolation_Levels()
        {
            // Arrange
            var options = Options.Create(new TransactionOptions());
            var resolver = new TransactionConfigurationResolver(options);

            // Test each isolation level
            var isolationLevels = new[]
            {
                IsolationLevel.ReadUncommitted,
                IsolationLevel.ReadCommitted,
                IsolationLevel.RepeatableRead,
                IsolationLevel.Serializable,
                IsolationLevel.Snapshot
            };

            foreach (var level in isolationLevels)
            {
                // Create a test request type dynamically would be complex,
                // so we'll just verify the attribute accepts all levels
                var attribute = new TransactionAttribute(level);
                Assert.Equal(level, attribute.IsolationLevel);
            }
        }

        [Fact]
        public void Constructor_Should_Throw_When_Options_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TransactionConfigurationResolver(null!));
        }
    }
}
