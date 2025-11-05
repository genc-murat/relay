using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Authorization;
using Relay.Core.Configuration.Options;
using Relay.Core.Configuration.Options.Authorization;
using Relay.Core.Configuration.Options.Core;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using Relay.Core.Testing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Authorization
{
    public class AuthorizationPipelineBehaviorTelemetryTests
    {
        private readonly Mock<IAuthorizationService> _authorizationServiceMock;
        private readonly Mock<IAuthorizationContext> _authorizationContextMock;
        private readonly IOptions<RelayOptions> _options;

        public AuthorizationPipelineBehaviorTelemetryTests()
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
        public async Task HandleAsync_Should_RecordTelemetryMetrics_WhenTelemetryProviderIsAvailable()
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
            var telemetryProviderMock = new Mock<ITelemetryProvider>();
            var metricsProviderMock = new Mock<IMetricsProvider>();
            var activityMock = new Activity("test");

            telemetryProviderMock.Setup(x => x.MetricsProvider).Returns(metricsProviderMock.Object);
            telemetryProviderMock.Setup(x => x.GetCorrelationId()).Returns("test-correlation-id");
            telemetryProviderMock.Setup(x => x.StartActivity(It.IsAny<string>(), It.IsAny<Type>(), It.IsAny<string>()))
                .Returns(activityMock);

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
            telemetryProviderMock.Verify(x => x.StartActivity("Authorization", typeof(TestAuthorizedRequest), "test-correlation-id"), Times.Once);
            metricsProviderMock.Verify(x => x.RecordHandlerExecution(It.Is<HandlerExecutionMetrics>(m =>
                m.HandlerName == "AuthorizationPipelineBehavior" &&
                m.RequestType == typeof(TestAuthorizedRequest) &&
                m.Success == true)), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_SetActivityTags_WhenAuthorizationSucceeds()
        {
            // Arrange
            var relayOptions = new RelayOptions();
            var options = Options.Create(relayOptions);

            var logger = new Mock<ILogger<AuthorizationPipelineBehavior<TestAuthorizedRequest, string>>>();
            var telemetryProviderMock = new Mock<ITelemetryProvider>();
            var activity = new Activity("test").Start();

            telemetryProviderMock.Setup(x => x.GetCorrelationId()).Returns("test-correlation-id");
            telemetryProviderMock.Setup(x => x.StartActivity(It.IsAny<string>(), It.IsAny<Type>(), It.IsAny<string>()))
                .Returns(activity);

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
            Assert.Contains(activity.Tags, tag => tag.Key == "authorization.result" && tag.Value == "granted");
            Assert.Contains(activity.Tags, tag => tag.Key == "authorization.user");
            activity.Stop();
        }

        [Fact]
        public async Task HandleAsync_Should_SetActivityTags_WhenAuthorizationFails()
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
            var telemetryProviderMock = new Mock<ITelemetryProvider>();
            var activity = new Activity("test").Start();

            telemetryProviderMock.Setup(x => x.GetCorrelationId()).Returns("test-correlation-id");
            telemetryProviderMock.Setup(x => x.StartActivity(It.IsAny<string>(), It.IsAny<Type>(), It.IsAny<string>()))
                .Returns(activity);

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
            Assert.Contains(activity.Tags, tag => tag.Key == "authorization.result" && tag.Value == "denied");
            Assert.Contains(activity.Tags, tag => tag.Key == "authorization.user");
            activity.Stop();
        }

        [Fact]
        public async Task HandleAsync_Should_AddCorrelationIdToContext_WhenTelemetryProviderIsAvailable()
        {
            // Arrange
            var contextProperties = new Dictionary<string, object>();
            _authorizationContextMock.Setup(x => x.Properties).Returns(contextProperties);

            var relayOptions = new RelayOptions();
            var options = Options.Create(relayOptions);

            var logger = new Mock<ILogger<AuthorizationPipelineBehavior<TestAuthorizedRequest, string>>>();
            var telemetryProviderMock = new Mock<ITelemetryProvider>();

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
            Assert.Contains("CorrelationId", contextProperties.Keys);
            Assert.Equal("test-correlation-id", contextProperties["CorrelationId"]);
        }

        #region Test Helper Classes

        [Authorize("Admin")]
        public class TestAuthorizedRequest : IRequest<string> { }

        #endregion
    }
}

