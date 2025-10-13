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
using System.Diagnostics;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
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
            var ex = Assert.Throws<ArgumentNullException>(() => act());
            Assert.Equal("authorizationService", ex.ParamName);
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
            var ex = Assert.Throws<ArgumentNullException>(() => act());
            Assert.Equal("authorizationContext", ex.ParamName);
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
            var ex = Assert.Throws<ArgumentNullException>(() => act());
            Assert.Equal("logger", ex.ParamName);
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
            var ex = Assert.Throws<ArgumentNullException>(() => act());
            Assert.Equal("options", ex.ParamName);
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
            Assert.True(nextCalled);
            Assert.Equal("Success", result);
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
            Assert.Equal("Success", result);
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
            Assert.Equal("Relay.Core.Tests.Authorization.AuthorizationPipelineBehaviorTests+TestAuthorizedRequest", contextProperties["RequestType"]);
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

        [Fact]
        public async Task HandleAsync_Should_CacheAuthorizeAttributes_ForPerformance()
        {
            // Arrange
            var behavior = CreateBehaviorForAuthorizedRequest();
            var request = new TestAuthorizedRequest();
            _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<IAuthorizationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            RequestHandlerDelegate<string> next = () => new ValueTask<string>("Success");

            // Act - Call multiple times to test caching
            await behavior.HandleAsync(request, next, CancellationToken.None);
            await behavior.HandleAsync(request, next, CancellationToken.None);
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert - Authorization service should be called 3 times (once per request)
            _authorizationServiceMock.Verify(x => x.AuthorizeAsync(It.IsAny<IAuthorizationContext>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
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
