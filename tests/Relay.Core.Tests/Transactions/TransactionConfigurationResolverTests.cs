using System;
using System.Data;
using System.Reflection;
using Microsoft.Extensions.Options;
using Xunit;
using Relay.Core.Transactions;

namespace Relay.Core.Tests.Transactions
{
    public class TransactionConfigurationResolverTests
    {
        private readonly TransactionOptions _options;
        private readonly TransactionConfigurationResolver _resolver;

        public TransactionConfigurationResolverTests()
        {
            _options = new TransactionOptions { RequireExplicitTransactionAttribute = true };
            var optionsWrapper = new OptionsWrapper<TransactionOptions>(_options);
            _resolver = new TransactionConfigurationResolver(optionsWrapper);
        }

        [Fact]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TransactionConfigurationResolver(null));
        }

        [Fact]
        public void Resolve_WithNullRequestType_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _resolver.Resolve((Type)null));
        }

        [Fact]
        public void Resolve_WithNonTransactionalRequest_ThrowsTransactionConfigurationException()
        {
            // Arrange
            var requestType = typeof(object); // Does not implement ITransactionalRequest

            // Act & Assert
            var ex = Assert.Throws<TransactionConfigurationException>(() => _resolver.Resolve(requestType));
            Assert.Contains("does not implement ITransactionalRequest", ex.Message);
        }

        [Fact]
        public void Resolve_WithTransactionalRequestMissingAttribute_ThrowsTransactionConfigurationException()
        {
            // Arrange
            var requestType = typeof(RequestWithoutAttribute); // Implements ITransactionalRequest but no TransactionAttribute

            // Act & Assert
            var ex = Assert.Throws<TransactionConfigurationException>(() => _resolver.Resolve(requestType));
            Assert.Contains("missing the required [Transaction] attribute", ex.Message);
        }

        [Fact]
        public void Resolve_WithTransactionalRequestMissingAttributeAndRequireExplicitFalse_ThrowsTransactionConfigurationException()
        {
            // Arrange
            var options = new TransactionOptions { RequireExplicitTransactionAttribute = false };
            var optionsWrapper = new OptionsWrapper<TransactionOptions>(options);
            var resolver = new TransactionConfigurationResolver(optionsWrapper);
            var requestType = typeof(RequestWithoutAttribute);

            // Act & Assert
            var ex = Assert.Throws<TransactionConfigurationException>(() => resolver.Resolve(requestType));
            Assert.Contains("is missing transaction configuration", ex.Message);
        }

        [Fact]
        public void Resolve_WithValidTransactionalRequest_ReturnsCorrectConfiguration()
        {
            // Arrange
            var requestType = typeof(ValidTransactionalRequest);

            // Act
            var config = _resolver.Resolve(requestType);

            // Assert
            Assert.NotNull(config);
            Assert.Equal(IsolationLevel.ReadCommitted, config.IsolationLevel);
            Assert.Equal(TimeSpan.FromSeconds(30), config.Timeout);
            Assert.False(config.IsReadOnly);
            Assert.False(config.UseDistributedTransaction);
            Assert.Null(config.RetryPolicy);
        }

        [Fact]
        public void Resolve_WithValidTransactionalRequestWithAllAttributes_ReturnsCorrectConfiguration()
        {
            // Arrange
            var requestType = typeof(ValidTransactionalRequestWithAttributes);

            // Act
            var config = _resolver.Resolve(requestType);

            // Assert
            Assert.NotNull(config);
            Assert.Equal(IsolationLevel.RepeatableRead, config.IsolationLevel);
            Assert.Equal(TimeSpan.FromSeconds(60), config.Timeout);
            Assert.False(config.IsReadOnly);  // Changed from True to False to match test class
            Assert.False(config.UseDistributedTransaction);  // Changed from True to False to match test class
            Assert.NotNull(config.RetryPolicy);
            Assert.Equal(3, config.RetryPolicy.MaxRetries);
            Assert.Equal(TimeSpan.FromMilliseconds(500), config.RetryPolicy.InitialDelay);
            Assert.Equal(RetryStrategy.Linear, config.RetryPolicy.Strategy);
        }

        [Fact]
        public void Resolve_WithInheritedAttributes_ReturnsCorrectConfiguration()
        {
            // Arrange
            var requestType = typeof(DerivedTransactionalRequest);

            // Act
            var config = _resolver.Resolve(requestType);

            // Assert
            Assert.NotNull(config);
            Assert.Equal(IsolationLevel.Serializable, config.IsolationLevel);
            Assert.Equal(TimeSpan.FromSeconds(120), config.Timeout);
            Assert.True(config.IsReadOnly);
            Assert.False(config.UseDistributedTransaction);
            Assert.NotNull(config.RetryPolicy);
            Assert.Equal(2, config.RetryPolicy.MaxRetries);
            Assert.Equal(TimeSpan.FromMilliseconds(1000), config.RetryPolicy.InitialDelay);
            Assert.Equal(RetryStrategy.ExponentialBackoff, config.RetryPolicy.Strategy);
        }

        [Fact]
        public void Resolve_WithInvalidTransactionAttribute_ThrowsTransactionConfigurationException()
        {
            // Arrange
            var requestType = typeof(InvalidTransactionalRequest); // Has TransactionAttribute with invalid values

            // Act & Assert
            var ex = Assert.Throws<TransactionConfigurationException>(() => _resolver.Resolve(requestType));
            Assert.Contains("Invalid transaction configuration", ex.Message);
        }

        [Fact]
        public void Resolve_WithInvalidTimeoutInAttribute_ThrowsTransactionConfigurationException()
        {
            // Arrange
            var requestType = typeof(InvalidTimeoutRequest);

            // Act & Assert
            var ex = Assert.Throws<TransactionConfigurationException>(() => _resolver.Resolve(requestType));
            Assert.Contains("Invalid transaction configuration", ex.Message);
        }

        [Fact]
        public void Resolve_WithInvalidRetryAttribute_ThrowsTransactionConfigurationException()
        {
            // Arrange
            var requestType = typeof(InvalidRetryAttributeRequest);

            // Act & Assert
            var ex = Assert.Throws<TransactionConfigurationException>(() => _resolver.Resolve(requestType));
            Assert.Contains("Invalid retry configuration", ex.Message);
        }

        [Fact]
        public void Resolve_WithUnspecifiedIsolationLevel_ThrowsArgumentException()
        {
            // Arrange
            var requestType = typeof(RequestWithUnspecifiedIsolationLevel);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _resolver.Resolve(requestType));
            Assert.Contains("Transaction isolation level cannot be Unspecified", ex.Message);
        }

        [Fact]
        public void Resolve_WithGenericMethodAndValidRequest_ReturnsCorrectConfiguration()
        {
            // Arrange
            var request = new ValidTransactionalRequest();

            // Act
            var config = _resolver.Resolve(request);

            // Assert
            Assert.NotNull(config);
            Assert.Equal(IsolationLevel.ReadCommitted, config.IsolationLevel);
        }

        [Fact]
        public void Resolve_WithGenericMethodAndNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _resolver.Resolve((ValidTransactionalRequest)null));
        }

        [Fact]
        public void ConvertTimeoutToTimeSpan_WithZeroTimeout_ReturnsInfiniteTimeSpan()
        {
            // Arrange
            var resolverType = typeof(TransactionConfigurationResolver);
            var method = resolverType.GetMethod("ConvertTimeoutToTimeSpan", 
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = method.Invoke(_resolver, new object[] { 0 });

            // Assert
            Assert.Equal(System.Threading.Timeout.InfiniteTimeSpan, (TimeSpan)result);
        }

        [Fact]
        public void ConvertTimeoutToTimeSpan_WithNegativeOneTimeout_ReturnsInfiniteTimeSpan()
        {
            // Arrange
            var resolverType = typeof(TransactionConfigurationResolver);
            var method = resolverType.GetMethod("ConvertTimeoutToTimeSpan", 
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = method.Invoke(_resolver, new object[] { -1 });

            // Assert
            Assert.Equal(System.Threading.Timeout.InfiniteTimeSpan, (TimeSpan)result);
        }

        [Fact]
        public void ConvertTimeoutToTimeSpan_WithPositiveTimeout_ReturnsCorrectTimeSpan()
        {
            // Arrange
            var resolverType = typeof(TransactionConfigurationResolver);
            var method = resolverType.GetMethod("ConvertTimeoutToTimeSpan", 
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = method.Invoke(_resolver, new object[] { 45 });

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(45), (TimeSpan)result);
        }

        [Fact]
        public void ConvertTimeoutToTimeSpan_WithNegativeTimeoutLessThanMinusOne_ThrowsTransactionConfigurationException()
        {
            // Arrange
            var resolverType = typeof(TransactionConfigurationResolver);
            var method = resolverType.GetMethod("ConvertTimeoutToTimeSpan", 
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Act & Assert
            var targetInvocationEx = Assert.Throws<TargetInvocationException>(() => 
                method.Invoke(_resolver, new object[] { -5 }));
            
            // The actual exception is wrapped in the TargetInvocationException
            Assert.IsType<TransactionConfigurationException>(targetInvocationEx.InnerException);
            Assert.Contains("Invalid timeout value", targetInvocationEx.InnerException.Message);
        }

        [Fact]
        public void BuildRetryPolicy_WithRetryAttribute_ReturnsCorrectPolicy()
        {
            // Arrange
            var retryAttribute = new TransactionRetryAttribute
            {
                MaxRetries = 5,
                InitialDelayMs = 2000,
                Strategy = RetryStrategy.ExponentialBackoff
            };
            
            var resolverType = typeof(TransactionConfigurationResolver);
            var method = resolverType.GetMethod("BuildRetryPolicy", 
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = method.Invoke(_resolver, new object[] { retryAttribute });

            // Assert
            var policy = (TransactionRetryPolicy)result;
            Assert.NotNull(policy);
            Assert.Equal(5, policy.MaxRetries);
            Assert.Equal(TimeSpan.FromMilliseconds(2000), policy.InitialDelay);
            Assert.Equal(RetryStrategy.ExponentialBackoff, policy.Strategy);
        }

        [Fact]
        public void BuildRetryPolicy_WithNullRetryAttribute_ReturnsNull()
        {
            // Arrange
            var resolverType = typeof(TransactionConfigurationResolver);
            var method = resolverType.GetMethod("BuildRetryPolicy",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = method.Invoke(_resolver, new object[] { null });

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildRetryPolicy_WithNullRetryAttributeAndDefaultPolicy_ReturnsDefaultPolicy()
        {
            // Arrange
            var defaultPolicy = new TransactionRetryPolicy
            {
                MaxRetries = 2,
                InitialDelay = TimeSpan.FromMilliseconds(100),
                Strategy = RetryStrategy.Linear
            };
            var options = new TransactionOptions
            {
                RequireExplicitTransactionAttribute = true,
                DefaultRetryPolicy = defaultPolicy
            };
            var optionsWrapper = new OptionsWrapper<TransactionOptions>(options);
            var resolver = new TransactionConfigurationResolver(optionsWrapper);

            var resolverType = typeof(TransactionConfigurationResolver);
            var method = resolverType.GetMethod("BuildRetryPolicy",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = method.Invoke(resolver, new object[] { null });

            // Assert
            var policy = (TransactionRetryPolicy)result;
            Assert.NotNull(policy);
            Assert.Equal(2, policy.MaxRetries);
            Assert.Equal(TimeSpan.FromMilliseconds(100), policy.InitialDelay);
            Assert.Equal(RetryStrategy.Linear, policy.Strategy);
        }

        // Test classes
        private class RequestWithoutAttribute : ITransactionalRequest
        {
            public object Handle() => new object();
        }

        [Transaction(IsolationLevel.ReadCommitted, TimeoutSeconds = 30)]
        private class ValidTransactionalRequest : ITransactionalRequest
        {
            public object Handle() => new object();
        }

        [Transaction(IsolationLevel.RepeatableRead, TimeoutSeconds = 60, IsReadOnly = false, UseDistributedTransaction = false)]
        [TransactionRetry(MaxRetries = 3, InitialDelayMs = 500, Strategy = RetryStrategy.Linear)]
        private class ValidTransactionalRequestWithAttributes : ITransactionalRequest
        {
            public object Handle() => new object();
        }

        [Transaction(IsolationLevel.ReadCommitted, TimeoutSeconds = -10)] // Invalid timeout
        private class InvalidTransactionalRequest : ITransactionalRequest
        {
            public object Handle() => new object();
        }

        [Transaction(IsolationLevel.ReadCommitted, TimeoutSeconds = -10)] // Invalid timeout value to trigger validation error
        private class InvalidTimeoutRequest : ITransactionalRequest
        {
            public object Handle() => new object();
        }

        [Transaction(IsolationLevel.ReadCommitted)]
        [TransactionRetry(MaxRetries = -1)] // Invalid max retries
        private class InvalidRetryAttributeRequest : ITransactionalRequest
        {
            public object Handle() => new object();
        }

        [Transaction(IsolationLevel.Unspecified)]
        private class RequestWithUnspecifiedIsolationLevel : ITransactionalRequest
        {
            public object Handle() => new object();
        }

        [Transaction(IsolationLevel.Serializable, TimeoutSeconds = 120, IsReadOnly = true, UseDistributedTransaction = false)]
        [TransactionRetry(MaxRetries = 2, InitialDelayMs = 1000, Strategy = RetryStrategy.ExponentialBackoff)]
        private class BaseTransactionalRequest : ITransactionalRequest
        {
            public object Handle() => new object();
        }

        private class DerivedTransactionalRequest : BaseTransactionalRequest
        {
        }
    }
}
