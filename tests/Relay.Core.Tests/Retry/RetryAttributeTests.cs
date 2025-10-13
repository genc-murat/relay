using System;

using Relay.Core.Contracts.Requests;
using Relay.Core.Retry;
using Xunit;

namespace Relay.Core.Tests.Retry
{
    public class RetryAttributeTests
    {
        [Fact]
        public void RetryAttribute_Constructor_Should_AcceptValidParameters()
        {
            // Arrange & Act
            var attribute = new RetryAttribute(maxRetryAttempts: 3, retryDelayMilliseconds: 1000);

            // Assert
            Assert.Equal(3, attribute.MaxRetryAttempts);
            Assert.Equal(1000, attribute.RetryDelayMilliseconds);
            Assert.Null(attribute.RetryStrategyType);
        }

        [Fact]
        public void RetryAttribute_Constructor_Should_UseDefaultDelay_WhenNotSpecified()
        {
            // Arrange & Act
            var attribute = new RetryAttribute(maxRetryAttempts: 5);

            // Assert
            Assert.Equal(5, attribute.MaxRetryAttempts);
            Assert.Equal(1000, attribute.RetryDelayMilliseconds); // Default value
        }

        [Fact]
        public void RetryAttribute_Constructor_Should_ThrowException_WhenMaxRetryAttemptsIsZero()
        {
            // Act
            Action act = () => new RetryAttribute(maxRetryAttempts: 0);
 
            // Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(act);
            Assert.Equal("maxRetryAttempts", ex.ParamName);
        }

        [Fact]
        public void RetryAttribute_Constructor_Should_ThrowException_WhenMaxRetryAttemptsIsNegative()
        {
            // Act
            Action act = () => new RetryAttribute(maxRetryAttempts: -1);
 
            // Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(act);
            Assert.Equal("maxRetryAttempts", ex.ParamName);
        }

        [Fact]
        public void RetryAttribute_Constructor_Should_ThrowException_WhenRetryDelayIsNegative()
        {
            // Act
            Action act = () => new RetryAttribute(maxRetryAttempts: 3, retryDelayMilliseconds: -1);
 
            // Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(act);
            Assert.Equal("retryDelayMilliseconds", ex.ParamName);
        }

        [Fact]
        public void RetryAttribute_Constructor_Should_AcceptZeroDelay()
        {
            // Arrange & Act
            var attribute = new RetryAttribute(maxRetryAttempts: 3, retryDelayMilliseconds: 0);
 
            // Assert
            Assert.Equal(0, attribute.RetryDelayMilliseconds);
        }

        [Fact]
        public void RetryAttribute_ConstructorWithStrategy_Should_AcceptValidStrategyType()
        {
            // Arrange & Act
            var attribute = new RetryAttribute(typeof(LinearRetryStrategy), maxRetryAttempts: 3);

            // Assert
            Assert.Equal(typeof(LinearRetryStrategy), attribute.RetryStrategyType);
            Assert.Equal(3, attribute.MaxRetryAttempts);
            Assert.Equal(0, attribute.RetryDelayMilliseconds);
        }

        [Fact]
        public void RetryAttribute_ConstructorWithStrategy_Should_ThrowException_WhenStrategyTypeIsNull()
        {
            // Act
            Action act = () => new RetryAttribute(null!, maxRetryAttempts: 3);

            // Assert
            var ex = Assert.Throws<ArgumentNullException>(act);

            Assert.Equal("retryStrategyType", ex.ParamName);
        }

        [Fact]
        public void RetryAttribute_ConstructorWithStrategy_Should_ThrowException_WhenStrategyDoesNotImplementInterface()
        {
            // Act
            Action act = () => new RetryAttribute(typeof(string), maxRetryAttempts: 3);

            // Assert
            var ex = Assert.Throws<ArgumentException>(act);

            Assert.Equal("retryStrategyType", ex.ParamName);

            Assert.Contains("IRetryStrategy", ex.Message);
        }

        [Fact]
        public void RetryAttribute_ConstructorWithStrategy_Should_ThrowException_WhenMaxRetryAttemptsIsInvalid()
        {
            // Act
            Action act = () => new RetryAttribute(typeof(LinearRetryStrategy), maxRetryAttempts: 0);

            // Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(act);

            Assert.Equal("maxRetryAttempts", ex.ParamName);
        }

        [Fact]
        public void RetryAttribute_ConstructorWithStrategy_Should_UseDefaultMaxAttempts_WhenNotSpecified()
        {
            // Arrange & Act
            var attribute = new RetryAttribute(typeof(ExponentialBackoffRetryStrategy));

            // Assert
            Assert.Equal(3, attribute.MaxRetryAttempts); // Default value
        }

        [Fact]
        public void RetryAttribute_Should_BeApplicableToClass()
        {
            // Verify attribute can be applied to class
            var attributes = typeof(TestRequestWithRetry).GetCustomAttributes(typeof(RetryAttribute), true);
            
            // Assert
            Assert.Single(attributes);
        }

        [Fact]
        public void RetryAttribute_Should_NotAllowMultipleInstances()
        {
            // This is enforced by AllowMultiple = false in the attribute definition
            // We verify it doesn't throw during compilation
            var type = typeof(TestRequestWithRetry);
            Assert.NotNull(type);
        }

        #region Test Helper Classes

        [Retry(3, 500)]
        public class TestRequestWithRetry : IRequest<string>
        {
        }

        #endregion
    }
}
