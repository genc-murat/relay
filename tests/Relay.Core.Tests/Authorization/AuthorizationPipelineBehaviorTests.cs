using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Authorization;
using Relay.Core.Configuration;
using Xunit;

namespace Relay.Core.Tests.Authorization
{
    public class AuthorizationPipelineBehaviorTests
    {
        private readonly Mock<IAuthorizationService> _authorizationServiceMock;
        private readonly Mock<IAuthorizationContext> _authorizationContextMock;
        private readonly Mock<ILogger<AuthorizationPipelineBehavior<TestRequest, string>>> _loggerMock;
        private readonly IOptions<RelayOptions> _options;

        public AuthorizationPipelineBehaviorTests()
        {
            _authorizationServiceMock = new Mock<IAuthorizationService>();
            _authorizationContextMock = new Mock<IAuthorizationContext>();
            _loggerMock = new Mock<ILogger<AuthorizationPipelineBehavior<TestRequest, string>>>();
            
            var relayOptions = new RelayOptions();
            _options = Options.Create(relayOptions);

            // Setup context properties
            var properties = new Dictionary<string, object>();
            _authorizationContextMock.Setup(x => x.Properties).Returns(properties);
        }

        [Fact]
        public void Constructor_Should_ThrowException_WhenAuthorizationServiceIsNull()
        {
            // Act
            Action act = () => new AuthorizationPipelineBehavior<TestRequest, string>(
                null!,
                _authorizationContextMock.Object,
                _loggerMock.Object,
                _options);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("authorizationService");
        }

        [Fact]
        public void Constructor_Should_ThrowException_WhenAuthorizationContextIsNull()
        {
            // Act
            Action act = () => new AuthorizationPipelineBehavior<TestRequest, string>(
                _authorizationServiceMock.Object,
                null!,
                _loggerMock.Object,
                _options);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("authorizationContext");
        }

        [Fact]
        public void Constructor_Should_ThrowException_WhenLoggerIsNull()
        {
            // Act
            Action act = () => new AuthorizationPipelineBehavior<TestRequest, string>(
                _authorizationServiceMock.Object,
                _authorizationContextMock.Object,
                null!,
                _options);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void Constructor_Should_ThrowException_WhenOptionsIsNull()
        {
            // Act
            Action act = () => new AuthorizationPipelineBehavior<TestRequest, string>(
                _authorizationServiceMock.Object,
                _authorizationContextMock.Object,
                _loggerMock.Object,
                null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("options");
        }

        [Fact]
        public async Task HandleAsync_Should_CallNext_WhenAuthorizationIsNotEnabled()
        {
            // Arrange
            var behavior = CreateBehavior();
            var request = new TestRequest();
            var nextCalled = false;
            RequestHandlerDelegate<string> next = () =>
            {
                nextCalled = true;
                return new ValueTask<string>("Success");
            };

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            nextCalled.Should().BeTrue();
            result.Should().Be("Success");
            _authorizationServiceMock.Verify(x => x.AuthorizeAsync(It.IsAny<IAuthorizationContext>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_Should_CallAuthorizationService_WhenAuthorizeAttributeIsPresent()
        {
            // Arrange
            var behavior = CreateBehaviorForAuthorizedRequest();
            var request = new TestAuthorizedRequest();
            _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<IAuthorizationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            RequestHandlerDelegate<string> next = () => new ValueTask<string>("Success");

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            result.Should().Be("Success");
            _authorizationServiceMock.Verify(x => x.AuthorizeAsync(_authorizationContextMock.Object, It.IsAny<CancellationToken>()), Times.Once);
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
            await act.Should().ThrowAsync<AuthorizationException>()
                .WithMessage("*Authorization failed for request*");
        }

        [Fact]
        public async Task HandleAsync_Should_ContinueToNext_WhenAuthorizationFailsAndThrowIsDisabled()
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
            nextCalled.Should().BeTrue();
            result.Should().Be("Success");
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
            contextProperties.Should().ContainKey("RequestType");
            contextProperties["RequestType"].Should().Be("Relay.Core.Tests.Authorization.AuthorizationPipelineBehaviorTests+TestAuthorizedRequest");
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
                _loggerMock.Object,
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

        [Fact]
        public async Task HandleAsync_Should_LogWarning_WhenAuthorizationFails()
        {
            // Arrange
            var relayOptions = new RelayOptions
            {
                DefaultAuthorizationOptions = new AuthorizationOptions
                {
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

            RequestHandlerDelegate<string> next = () => new ValueTask<string>("Success");

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            logger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Authorization failed")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private AuthorizationPipelineBehavior<TestRequest, string> CreateBehavior()
        {
            return new AuthorizationPipelineBehavior<TestRequest, string>(
                _authorizationServiceMock.Object,
                _authorizationContextMock.Object,
                _loggerMock.Object,
                _options);
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
