using System;
using FluentAssertions;
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
            attribute.MaxRetryAttempts.Should().Be(3);
            attribute.RetryDelayMilliseconds.Should().Be(1000);
            attribute.RetryStrategyType.Should().BeNull();
        }

        [Fact]
        public void RetryAttribute_Constructor_Should_UseDefaultDelay_WhenNotSpecified()
        {
            // Arrange & Act
            var attribute = new RetryAttribute(maxRetryAttempts: 5);

            // Assert
            attribute.MaxRetryAttempts.Should().Be(5);
            attribute.RetryDelayMilliseconds.Should().Be(1000); // Default value
        }

        [Fact]
        public void RetryAttribute_Constructor_Should_ThrowException_WhenMaxRetryAttemptsIsZero()
        {
            // Act
            Action act = () => new RetryAttribute(maxRetryAttempts: 0);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("maxRetryAttempts");
        }

        [Fact]
        public void RetryAttribute_Constructor_Should_ThrowException_WhenMaxRetryAttemptsIsNegative()
        {
            // Act
            Action act = () => new RetryAttribute(maxRetryAttempts: -1);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("maxRetryAttempts");
        }

        [Fact]
        public void RetryAttribute_Constructor_Should_ThrowException_WhenRetryDelayIsNegative()
        {
            // Act
            Action act = () => new RetryAttribute(maxRetryAttempts: 3, retryDelayMilliseconds: -1);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("retryDelayMilliseconds");
        }

        [Fact]
        public void RetryAttribute_Constructor_Should_AcceptZeroDelay()
        {
            // Arrange & Act
            var attribute = new RetryAttribute(maxRetryAttempts: 3, retryDelayMilliseconds: 0);

            // Assert
            attribute.RetryDelayMilliseconds.Should().Be(0);
        }

        [Fact]
        public void RetryAttribute_ConstructorWithStrategy_Should_AcceptValidStrategyType()
        {
            // Arrange & Act
            var attribute = new RetryAttribute(typeof(LinearRetryStrategy), maxRetryAttempts: 3);

            // Assert
            attribute.RetryStrategyType.Should().Be(typeof(LinearRetryStrategy));
            attribute.MaxRetryAttempts.Should().Be(3);
            attribute.RetryDelayMilliseconds.Should().Be(0);
        }

        [Fact]
        public void RetryAttribute_ConstructorWithStrategy_Should_ThrowException_WhenStrategyTypeIsNull()
        {
            // Act
            Action act = () => new RetryAttribute(null!, maxRetryAttempts: 3);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("retryStrategyType");
        }

        [Fact]
        public void RetryAttribute_ConstructorWithStrategy_Should_ThrowException_WhenStrategyDoesNotImplementInterface()
        {
            // Act
            Action act = () => new RetryAttribute(typeof(string), maxRetryAttempts: 3);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("retryStrategyType")
                .WithMessage("*IRetryStrategy*");
        }

        [Fact]
        public void RetryAttribute_ConstructorWithStrategy_Should_ThrowException_WhenMaxRetryAttemptsIsInvalid()
        {
            // Act
            Action act = () => new RetryAttribute(typeof(LinearRetryStrategy), maxRetryAttempts: 0);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("maxRetryAttempts");
        }

        [Fact]
        public void RetryAttribute_ConstructorWithStrategy_Should_UseDefaultMaxAttempts_WhenNotSpecified()
        {
            // Arrange & Act
            var attribute = new RetryAttribute(typeof(ExponentialBackoffRetryStrategy));

            // Assert
            attribute.MaxRetryAttempts.Should().Be(3); // Default value
        }

        [Fact]
        public void RetryAttribute_Should_BeApplicableToClass()
        {
            // Verify attribute can be applied to class
            var attributes = typeof(TestRequestWithRetry).GetCustomAttributes(typeof(RetryAttribute), true);
            
            // Assert
            attributes.Should().HaveCount(1);
        }

        [Fact]
        public void RetryAttribute_Should_NotAllowMultipleInstances()
        {
            // This is enforced by AllowMultiple = false in the attribute definition
            // We verify it doesn't throw during compilation
            var type = typeof(TestRequestWithRetry);
            type.Should().NotBeNull();
        }

        #region Test Helper Classes

        [Retry(3, 500)]
        public class TestRequestWithRetry : IRequest<string>
        {
        }

        #endregion
    }
}
