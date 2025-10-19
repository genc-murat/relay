using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Authorization;
using Relay.Core.Configuration.Options;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Authorization
{
    public class AuthorizationPipelineBehaviorFailureTests
    {
        private readonly Mock<IAuthorizationService> _authorizationServiceMock;
        private readonly Mock<IAuthorizationContext> _authorizationContextMock;
        private readonly IOptions<RelayOptions> _options;

        public AuthorizationPipelineBehaviorFailureTests()
        {
            _authorizationServiceMock = new Mock<IAuthorizationService>();
            _authorizationContextMock = new Mock<IAuthorizationContext>();

            var relayOptions = new RelayOptions();
            _options = Options.Create(relayOptions);

            // Setup context properties
            var properties = new Dictionary<string, object>();
            _authorizationContextMock.Setup(x => x.Properties).Returns(properties);
        }

        [Fact]
        public async Task HandleAsync_Should_ThrowAuthorizationException_WhenAuthorizationFailsAndThrowIsEnabled()
        {
            // Arrange
            var relayOptions = new RelayOptions
            {
                DefaultAuthorizationOptions = new AuthorizationOptions
                {
                    EnableAutomaticAuthorization = false,
                    ThrowOnAuthorizationFailure = true
                }
            };
            var options = Options.Create(relayOptions);

            var logger = new Mock<ILogger<AuthorizationPipelineBehavior<TestAuthorizedRequest, string>>>();
            var behavior = new AuthorizationPipelineBehavior<TestAuthorizedRequest, string>(
                _authorizationServiceMock.Object,
                _authorizationContextMock.Object,
                logger.Object,
                options);

            var request = new TestAuthorizedRequest();
            _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<IAuthorizationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            RequestHandlerDelegate<string> next = () => new ValueTask<string>("Success");

            // Act
            Func<Task> act = async () => await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            var ex = await Assert.ThrowsAsync<AuthorizationException>(act);
            Assert.Contains("Authorization failed for request", ex.Message);
        }

        [Fact]
        public async Task HandleAsync_Should_ReturnDefaultResponseAndNotCallNext_WhenAuthorizationFailsAndThrowIsDisabled()
        {
            // Arrange
            var relayOptions = new RelayOptions
            {
                DefaultAuthorizationOptions = new AuthorizationOptions
                {
                    EnableAutomaticAuthorization = false,
                    ThrowOnAuthorizationFailure = false
                }
            };
            var options = Options.Create(relayOptions);

            var logger = new Mock<ILogger<AuthorizationPipelineBehavior<TestAuthorizedRequest, string>>>();
            var behavior = new AuthorizationPipelineBehavior<TestAuthorizedRequest, string>(
                _authorizationServiceMock.Object,
                _authorizationContextMock.Object,
                logger.Object,
                options);

            var request = new TestAuthorizedRequest();
            _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<IAuthorizationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var nextCalled = false;
            RequestHandlerDelegate<string> next = () =>
            {
                nextCalled = true;
                return new ValueTask<string>("Success");
            };

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.False(nextCalled);
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleAsync_Should_ReturnDefaultResponse_WhenAuthorizationFailsAndThrowIsDisabled()
        {
            // Arrange
            var relayOptions = new RelayOptions
            {
                DefaultAuthorizationOptions = new AuthorizationOptions
                {
                    EnableAutomaticAuthorization = false,
                    ThrowOnAuthorizationFailure = false
                }
            };
            var options = Options.Create(relayOptions);

            var logger = new Mock<ILogger<AuthorizationPipelineBehavior<TestAuthorizedRequest, string>>>();
            var behavior = new AuthorizationPipelineBehavior<TestAuthorizedRequest, string>(
                _authorizationServiceMock.Object,
                _authorizationContextMock.Object,
                logger.Object,
                options);

            var request = new TestAuthorizedRequest();
            _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<IAuthorizationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var nextCalled = false;
            RequestHandlerDelegate<string> next = () =>
            {
                nextCalled = true;
                return new ValueTask<string>("Success");
            };

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.False(nextCalled);
            Assert.Null(result);
        }

        #region Test Helper Classes

        [Authorize("Admin")]
        public class TestAuthorizedRequest : IRequest<string> { }

        #endregion
    }
}