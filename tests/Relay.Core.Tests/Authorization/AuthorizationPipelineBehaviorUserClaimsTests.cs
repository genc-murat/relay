using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Authorization;
using Relay.Core.Configuration.Options;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Authorization
{
    public class AuthorizationPipelineBehaviorUserClaimsTests
    {
        private readonly Mock<IAuthorizationService> _authorizationServiceMock;
        private readonly Mock<IAuthorizationContext> _authorizationContextMock;
        private readonly IOptions<RelayOptions> _options;

        public AuthorizationPipelineBehaviorUserClaimsTests()
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
        public async Task HandleAsync_Should_ExtractUserNameFromClaims()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "TestUser"),
                new Claim(ClaimTypes.Email, "test@example.com")
            };

            _authorizationContextMock.Setup(x => x.UserClaims).Returns(claims);

            var relayOptions = new RelayOptions
            {
                DefaultAuthorizationOptions = new AuthorizationOptions
                {
                    ThrowOnAuthorizationFailure = false
                }
            };
            var options = Options.Create(relayOptions);

            var logger = new Mock<ILogger<AuthorizationPipelineBehavior<TestAuthorizedRequest, string>>>();
            var telemetryProviderMock = new Mock<ITelemetryProvider>();
            var metricsProviderMock = new Mock<IMetricsProvider>();

            telemetryProviderMock.Setup(x => x.MetricsProvider).Returns(metricsProviderMock.Object);
            telemetryProviderMock.Setup(x => x.GetCorrelationId()).Returns("test-correlation-id");

            var behavior = new AuthorizationPipelineBehavior<TestAuthorizedRequest, string>(
                _authorizationServiceMock.Object,
                _authorizationContextMock.Object,
                logger.Object,
                options,
                telemetryProviderMock.Object);

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
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TestUser")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData(ClaimTypes.Name, "MuratDoe")]
        [InlineData(ClaimTypes.NameIdentifier, "user123")]
        [InlineData("name", "JaneDoe")]
        [InlineData("preferred_username", "preferred_user")]
        public async Task HandleAsync_Should_ExtractUserNameFromDifferentClaimTypes(string claimType, string expectedUserName)
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(claimType, expectedUserName)
            };

            _authorizationContextMock.Setup(x => x.UserClaims).Returns(claims);

            var relayOptions = new RelayOptions
            {
                DefaultAuthorizationOptions = new AuthorizationOptions
                {
                    ThrowOnAuthorizationFailure = false
                }
            };
            var options = Options.Create(relayOptions);

            var logger = new Mock<ILogger<AuthorizationPipelineBehavior<TestAuthorizedRequest, string>>>();
            var telemetryProviderMock = new Mock<ITelemetryProvider>();
            var metricsProviderMock = new Mock<IMetricsProvider>();

            telemetryProviderMock.Setup(x => x.MetricsProvider).Returns(metricsProviderMock.Object);
            telemetryProviderMock.Setup(x => x.GetCorrelationId()).Returns("test-correlation-id");

            var behavior = new AuthorizationPipelineBehavior<TestAuthorizedRequest, string>(
                _authorizationServiceMock.Object,
                _authorizationContextMock.Object,
                logger.Object,
                options,
                telemetryProviderMock.Object);

            var request = new TestAuthorizedRequest();
            _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<IAuthorizationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            RequestHandlerDelegate<string> next = () => new ValueTask<string>("Success");

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            metricsProviderMock.Verify(x => x.RecordHandlerExecution(
                It.Is<HandlerExecutionMetrics>(m =>
                    m.Properties.ContainsKey("User") &&
                    m.Properties["User"].ToString() == expectedUserName)),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_UseAnonymous_WhenNoUserClaimsAvailable()
        {
            // Arrange
            _authorizationContextMock.Setup(x => x.UserClaims).Returns((IEnumerable<Claim>)null!);

            var relayOptions = new RelayOptions
            {
                DefaultAuthorizationOptions = new AuthorizationOptions
                {
                    ThrowOnAuthorizationFailure = false
                }
            };
            var options = Options.Create(relayOptions);

            var logger = new Mock<ILogger<AuthorizationPipelineBehavior<TestAuthorizedRequest, string>>>();
            var telemetryProviderMock = new Mock<ITelemetryProvider>();
            var metricsProviderMock = new Mock<IMetricsProvider>();

            telemetryProviderMock.Setup(x => x.MetricsProvider).Returns(metricsProviderMock.Object);
            telemetryProviderMock.Setup(x => x.GetCorrelationId()).Returns("test-correlation-id");

            var behavior = new AuthorizationPipelineBehavior<TestAuthorizedRequest, string>(
                _authorizationServiceMock.Object,
                _authorizationContextMock.Object,
                logger.Object,
                options,
                telemetryProviderMock.Object);

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
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Anonymous")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #region Test Helper Classes

        [Authorize("Admin")]
        public class TestAuthorizedRequest : IRequest<string> { }

        #endregion
    }
}