using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Authorization;
using Relay.Core.Configuration.Options.Authorization;
using Relay.Core.Configuration.Options.Core;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Authorization
{
    public class AuthorizationPipelineBehaviorConfigurationTests
    {
        private readonly Mock<IAuthorizationService> _authorizationServiceMock;
        private readonly Mock<IAuthorizationContext> _authorizationContextMock;
        private readonly IOptions<RelayOptions> _options;

        public AuthorizationPipelineBehaviorConfigurationTests()
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
        public async Task HandleAsync_Should_AddRequestInfoToContext()
        {
            // Arrange
            var behavior = CreateBehaviorForAuthorizedRequest();
            var request = new TestAuthorizedRequest();
            _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<IAuthorizationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var contextProperties = new Dictionary<string, object>();
            _authorizationContextMock.Setup(x => x.Properties).Returns(contextProperties);

            RequestHandlerDelegate<string> next = () => new ValueTask<string>("Success");

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.Contains("RequestType", contextProperties.Keys);
            Assert.Equal("Relay.Core.Tests.Authorization.AuthorizationPipelineBehaviorConfigurationTests+TestAuthorizedRequest", contextProperties["RequestType"]);
        }

        [Fact]
        public async Task HandleAsync_Should_UseHandlerSpecificOptions_WhenAvailable()
        {
            // Arrange
            var handlerKey = typeof(TestAuthorizedRequest).FullName!;
            var handlerSpecificOptions = new AuthorizationOptions
            {
                EnableAutomaticAuthorization = true,
                ThrowOnAuthorizationFailure = true
            };

            var relayOptions = new RelayOptions();
            relayOptions.AuthorizationOverrides[handlerKey] = handlerSpecificOptions;
            var options = Options.Create(relayOptions);

            var logger = new Mock<ILogger<AuthorizationPipelineBehavior<TestAuthorizedRequest, string>>>();
            var behavior = new AuthorizationPipelineBehavior<TestAuthorizedRequest, string>(
                _authorizationServiceMock.Object,
                _authorizationContextMock.Object,
                logger.Object,
                options);

            var request = new TestAuthorizedRequest();
            _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<IAuthorizationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            RequestHandlerDelegate<string> next = () => new ValueTask<string>("Success");

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            _authorizationServiceMock.Verify(x => x.AuthorizeAsync(It.IsAny<IAuthorizationContext>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_RespectCancellationToken()
        {
            // Arrange
            var behavior = CreateBehaviorForAuthorizedRequest();
            var request = new TestAuthorizedRequest();
            var cts = new CancellationTokenSource();

            _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<IAuthorizationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            RequestHandlerDelegate<string> next = () => new ValueTask<string>("Success");

            // Act
            var result = await behavior.HandleAsync(request, next, cts.Token);

            // Assert
            _authorizationServiceMock.Verify(x => x.AuthorizeAsync(It.IsAny<IAuthorizationContext>(), cts.Token), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_EnableAuthorization_WhenGloballyEnabled()
        {
            // Arrange
            var relayOptions = new RelayOptions
            {
                DefaultAuthorizationOptions = new AuthorizationOptions
                {
                    EnableAutomaticAuthorization = true,
                    ThrowOnAuthorizationFailure = true
                }
            };
            var options = Options.Create(relayOptions);

            var behavior = new AuthorizationPipelineBehavior<TestRequest, string>(
                _authorizationServiceMock.Object,
                _authorizationContextMock.Object,
                new Mock<ILogger<AuthorizationPipelineBehavior<TestRequest, string>>>().Object,
                options);

            var request = new TestRequest();
            _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<IAuthorizationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            RequestHandlerDelegate<string> next = () => new ValueTask<string>("Success");

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            _authorizationServiceMock.Verify(x => x.AuthorizeAsync(It.IsAny<IAuthorizationContext>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        private AuthorizationPipelineBehavior<TestAuthorizedRequest, string> CreateBehaviorForAuthorizedRequest()
        {
            var logger = new Mock<ILogger<AuthorizationPipelineBehavior<TestAuthorizedRequest, string>>>();
            return new AuthorizationPipelineBehavior<TestAuthorizedRequest, string>(
                _authorizationServiceMock.Object,
                _authorizationContextMock.Object,
                logger.Object,
                _options);
        }

        #region Test Helper Classes

        public class TestRequest : IRequest<string> { }

        [Authorize("Admin")]
        public class TestAuthorizedRequest : IRequest<string> { }

        #endregion
    }
}