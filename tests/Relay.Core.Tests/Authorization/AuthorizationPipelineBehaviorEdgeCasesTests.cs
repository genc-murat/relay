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
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Authorization
{
    public class AuthorizationPipelineBehaviorEdgeCasesTests
    {
        private readonly Mock<IAuthorizationService> _authorizationServiceMock;
        private readonly Mock<IAuthorizationContext> _authorizationContextMock;
        private readonly IOptions<RelayOptions> _options;

        public AuthorizationPipelineBehaviorEdgeCasesTests()
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
        public async Task HandleAsync_Should_ThrowArgumentNullException_WhenRequestIsNull()
        {
            // Arrange
            var behavior = CreateBehavior();

            // Act
            Func<Task> act = async () => await behavior.HandleAsync(null!, () => new ValueTask<string>("Success"), CancellationToken.None);

            // Assert
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(act);
            Assert.Equal("request", ex.ParamName);
        }

        [Fact]
        public async Task HandleAsync_Should_ReturnDefaultTaskResponse_WhenAuthorizationFailsAndThrowIsDisabled()
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

            var logger = new Mock<ILogger<AuthorizationPipelineBehavior<TestAuthorizedRequest, Task<string>>>>();
            var behavior = new AuthorizationPipelineBehavior<TestAuthorizedRequest, Task<string>>(
                _authorizationServiceMock.Object,
                _authorizationContextMock.Object,
                logger.Object,
                options);

            var request = new TestAuthorizedRequest();
            _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<IAuthorizationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var nextCalled = false;
            RequestHandlerDelegate<Task<string>> next = () =>
            {
                nextCalled = true;
                return new ValueTask<Task<string>>(Task.FromResult("Success"));
            };

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.False(nextCalled);
            Assert.NotNull(result);
            var resultValue = await result;
            Assert.Null(resultValue);
        }

        [Fact]
        public async Task HandleAsync_Should_ReturnDefaultValueTaskResponse_WhenAuthorizationFailsAndThrowIsDisabled()
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

            var logger = new Mock<ILogger<AuthorizationPipelineBehavior<TestAuthorizedRequest, ValueTask<string>>>>();
            var behavior = new AuthorizationPipelineBehavior<TestAuthorizedRequest, ValueTask<string>>(
                _authorizationServiceMock.Object,
                _authorizationContextMock.Object,
                logger.Object,
                options);

            var request = new TestAuthorizedRequest();
            _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<IAuthorizationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var nextCalled = false;
            RequestHandlerDelegate<ValueTask<string>> next = () =>
            {
                nextCalled = true;
                return new ValueTask<ValueTask<string>>(new ValueTask<string>("Success"));
            };

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.False(nextCalled);
            Assert.NotNull(result);
            var resultValue = await result;
            Assert.Null(resultValue);
        }

        [Fact]
        public async Task HandleAsync_Should_PropagateNonAuthorizationExceptions_FromAuthorizationService()
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
            var expectedException = new InvalidOperationException("Authorization service error");
            _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<IAuthorizationContext>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            RequestHandlerDelegate<string> next = () => new ValueTask<string>("Success");

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await behavior.HandleAsync(request, next, CancellationToken.None));
            Assert.Equal("Authorization service error", ex.Message);
        }

        [Fact]
        public async Task HandleAsync_Should_LogWarning_WhenMetricsRecordingFails()
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

            telemetryProviderMock.Setup(x => x.MetricsProvider).Returns(metricsProviderMock.Object);
            telemetryProviderMock.Setup(x => x.GetCorrelationId()).Returns("test-correlation-id");

            // Setup metrics provider to throw exception
            metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()))
                .Throws(new InvalidOperationException("Metrics recording failed"));

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
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.Equal("Success", result);
            logger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to record authorization metrics")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private AuthorizationPipelineBehavior<TestRequest, string> CreateBehavior()
        {
            var logger = new Mock<ILogger<AuthorizationPipelineBehavior<TestRequest, string>>>();
            return new AuthorizationPipelineBehavior<TestRequest, string>(
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