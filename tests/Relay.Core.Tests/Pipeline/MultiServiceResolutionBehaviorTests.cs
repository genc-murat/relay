using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Relay.Core.Contracts.Infrastructure;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Pipeline.Behaviors;
using Xunit;

namespace Relay.Core.Tests.Pipeline
{
    /// <summary>
    /// Tests for MultiServiceResolutionBehavior pipeline behavior.
    /// </summary>
    public class MultiServiceResolutionBehaviorTests
    {
        [Fact]
        public void Constructor_Should_Throw_ArgumentNullException_When_ServiceFactory_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MultiServiceResolutionBehavior<TestRequest, string>(null!));
        }

        [Fact]
        public async Task HandleAsync_Should_Resolve_And_Call_Validators_When_Available()
        {
            // Arrange
            var validator1Mock = new Mock<IValidator<TestRequest>>();
            var validator2Mock = new Mock<IValidator<TestRequest>>();
            var validators = new[] { validator1Mock.Object, validator2Mock.Object };

            ServiceFactory serviceFactory = type =>
            {
                if (type == typeof(IEnumerable<IValidator<TestRequest>>))
                    return validators;
                return null;
            };

            var behavior = new MultiServiceResolutionBehavior<TestRequest, string>(serviceFactory);
            var request = new TestRequest();
            var nextCalled = false;
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () =>
            {
                nextCalled = true;
                return new ValueTask<string>("response");
            };

            // Act
            var result = await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal("response", result);

            validator1Mock.Verify(v => v.ValidateAsync(request, cancellationToken), Times.Once);
            validator2Mock.Verify(v => v.ValidateAsync(request, cancellationToken), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_Handle_Empty_Validators_Collection()
        {
            // Arrange
            var validators = Array.Empty<IValidator<TestRequest>>();

            ServiceFactory serviceFactory = type =>
            {
                if (type == typeof(IEnumerable<IValidator<TestRequest>>))
                    return validators;
                return null;
            };

            var behavior = new MultiServiceResolutionBehavior<TestRequest, string>(serviceFactory);
            var request = new TestRequest();
            var nextCalled = false;
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () =>
            {
                nextCalled = true;
                return new ValueTask<string>("response");
            };

            // Act
            var result = await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal("response", result);
        }

        [Fact]
        public async Task HandleAsync_Should_Propagate_Exception_From_Validator()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Validation failed");
            var validatorMock = new Mock<IValidator<TestRequest>>();
            validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            var validators = new[] { validatorMock.Object };

            ServiceFactory serviceFactory = type =>
            {
                if (type == typeof(IEnumerable<IValidator<TestRequest>>))
                    return validators;
                return null;
            };

            var behavior = new MultiServiceResolutionBehavior<TestRequest, string>(serviceFactory);
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () => new ValueTask<string>("response");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => behavior.HandleAsync(request, next, cancellationToken).AsTask());

            Assert.Equal(expectedException, exception);
        }

        [Fact]
        public async Task HandleAsync_Should_Pass_CancellationToken_To_Validators()
        {
            // Arrange
            var validatorMock = new Mock<IValidator<TestRequest>>();
            var validators = new[] { validatorMock.Object };

            ServiceFactory serviceFactory = type =>
            {
                if (type == typeof(IEnumerable<IValidator<TestRequest>>))
                    return validators;
                return null;
            };

            var behavior = new MultiServiceResolutionBehavior<TestRequest, string>(serviceFactory);
            var request = new TestRequest();
            var cancellationToken = new CancellationToken(true);

            RequestHandlerDelegate<string> next = () => new ValueTask<string>("response");

            // Act
            await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            validatorMock.Verify(v => v.ValidateAsync(request, cancellationToken), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_Always_Call_Next_Delegate_After_Validation()
        {
            // Arrange
            var validatorMock = new Mock<IValidator<TestRequest>>();
            var validators = new[] { validatorMock.Object };

            ServiceFactory serviceFactory = type =>
            {
                if (type == typeof(IEnumerable<IValidator<TestRequest>>))
                    return validators;
                return null;
            };

            var behavior = new MultiServiceResolutionBehavior<TestRequest, string>(serviceFactory);
            var request = new TestRequest();
            var nextCalled = false;
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () =>
            {
                nextCalled = true;
                return new ValueTask<string>("response");
            };

            // Act
            await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            Assert.True(nextCalled);
        }

        // Test request class
        public class TestRequest : IRequest<string> { }
    }
}