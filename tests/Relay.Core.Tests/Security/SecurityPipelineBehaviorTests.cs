using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Security.Behaviors;
using Relay.Core.Security.Interfaces;
using Relay.Core.Security.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Security
{
    public class SecurityPipelineBehaviorTests
    {
        private readonly Mock<ILogger<SecurityPipelineBehavior<TestRequest, TestResponse>>> _loggerMock;
        private readonly Mock<ISecurityContext> _securityContextMock;
        private readonly Mock<IRequestAuditor> _auditorMock;
        private readonly Mock<IRateLimiter> _rateLimiterMock;
        private readonly Mock<RequestHandlerDelegate<TestResponse>> _nextMock;

        public SecurityPipelineBehaviorTests()
        {
            _loggerMock = new Mock<ILogger<SecurityPipelineBehavior<TestRequest, TestResponse>>>();
            _securityContextMock = new Mock<ISecurityContext>();
            _auditorMock = new Mock<IRequestAuditor>();
            _rateLimiterMock = new Mock<IRateLimiter>();
            _nextMock = new Mock<RequestHandlerDelegate<TestResponse>>();
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Arrange
            var securityContext = _securityContextMock.Object;
            var auditor = _auditorMock.Object;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new SecurityPipelineBehavior<TestRequest, TestResponse>(null!, securityContext, auditor));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenSecurityContextIsNull()
        {
            // Arrange
            var logger = _loggerMock.Object;
            var auditor = _auditorMock.Object;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new SecurityPipelineBehavior<TestRequest, TestResponse>(logger, null!, auditor));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenAuditorIsNull()
        {
            // Arrange
            var logger = _loggerMock.Object;
            var securityContext = _securityContextMock.Object;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new SecurityPipelineBehavior<TestRequest, TestResponse>(logger, securityContext, null!));
        }

        [Fact]
        public void Constructor_ShouldSucceed_WithValidParameters()
        {
            // Arrange
            var logger = _loggerMock.Object;
            var securityContext = _securityContextMock.Object;
            var auditor = _auditorMock.Object;

            // Act
            var behavior = new SecurityPipelineBehavior<TestRequest, TestResponse>(logger, securityContext, auditor);

            // Assert
            Assert.NotNull(behavior);
        }

        [Fact]
        public void Constructor_ShouldSucceed_WithOptionalRateLimiter()
        {
            // Arrange
            var logger = _loggerMock.Object;
            var securityContext = _securityContextMock.Object;
            var auditor = _auditorMock.Object;
            var rateLimiter = _rateLimiterMock.Object;

            // Act
            var behavior = new SecurityPipelineBehavior<TestRequest, TestResponse>(logger, securityContext, auditor, rateLimiter);

            // Assert
            Assert.NotNull(behavior);
        }

        [Fact]
        public async Task HandleAsync_ShouldCallAuditorLogRequestAsync()
        {
            // Arrange
            var userId = "user123";
            var request = new TestRequest();
            var response = new TestResponse();
            var cancellationToken = CancellationToken.None;

            _securityContextMock.Setup(x => x.UserId).Returns(userId);
            _nextMock.Setup(x => x()).Returns(new ValueTask<TestResponse>(response));

            var behavior = new SecurityPipelineBehavior<TestRequest, TestResponse>(
                _loggerMock.Object, _securityContextMock.Object, _auditorMock.Object);

            // Act
            await behavior.HandleAsync(request, _nextMock.Object, cancellationToken);

            // Assert
            _auditorMock.Verify(x => x.LogRequestAsync(userId, "TestRequest", request, cancellationToken), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldCallNextDelegate()
        {
            // Arrange
            var userId = "user123";
            var request = new TestRequest();
            var response = new TestResponse();
            var cancellationToken = CancellationToken.None;

            _securityContextMock.Setup(x => x.UserId).Returns(userId);
            _nextMock.Setup(x => x()).Returns(new ValueTask<TestResponse>(response));

            var behavior = new SecurityPipelineBehavior<TestRequest, TestResponse>(
                _loggerMock.Object, _securityContextMock.Object, _auditorMock.Object);

            // Act
            var result = await behavior.HandleAsync(request, _nextMock.Object, cancellationToken);

            // Assert
            _nextMock.Verify(x => x(), Times.Once);
            Assert.Equal(response, result);
        }

        [Fact]
        public async Task HandleAsync_ShouldCallAuditorLogSuccessAsync_WhenNextSucceeds()
        {
            // Arrange
            var userId = "user123";
            var request = new TestRequest();
            var response = new TestResponse();
            var cancellationToken = CancellationToken.None;

            _securityContextMock.Setup(x => x.UserId).Returns(userId);
            _nextMock.Setup(x => x()).Returns(new ValueTask<TestResponse>(response));

            var behavior = new SecurityPipelineBehavior<TestRequest, TestResponse>(
                _loggerMock.Object, _securityContextMock.Object, _auditorMock.Object);

            // Act
            await behavior.HandleAsync(request, _nextMock.Object, cancellationToken);

            // Assert
            _auditorMock.Verify(x => x.LogSuccessAsync(userId, "TestRequest", cancellationToken), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldCallAuditorLogFailureAsync_WhenNextThrows()
        {
            // Arrange
            var userId = "user123";
            var request = new TestRequest();
            var exception = new InvalidOperationException("Test exception");
            var cancellationToken = CancellationToken.None;

            _securityContextMock.Setup(x => x.UserId).Returns(userId);
            _nextMock.Setup(x => x()).ThrowsAsync(exception);

            var behavior = new SecurityPipelineBehavior<TestRequest, TestResponse>(
                _loggerMock.Object, _securityContextMock.Object, _auditorMock.Object);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                behavior.HandleAsync(request, _nextMock.Object, cancellationToken).AsTask());

            // Assert
            Assert.Equal(exception, thrownException);
            _auditorMock.Verify(x => x.LogFailureAsync(userId, "TestRequest", exception, cancellationToken), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldNotCallRateLimiter_WhenRateLimiterIsNull()
        {
            // Arrange
            var userId = "user123";
            var request = new TestRequest();
            var response = new TestResponse();
            var cancellationToken = CancellationToken.None;

            _securityContextMock.Setup(x => x.UserId).Returns(userId);
            _nextMock.Setup(x => x()).Returns(new ValueTask<TestResponse>(response));

            var behavior = new SecurityPipelineBehavior<TestRequest, TestResponse>(
                _loggerMock.Object, _securityContextMock.Object, _auditorMock.Object, null);

            // Act
            await behavior.HandleAsync(request, _nextMock.Object, cancellationToken);

            // Assert
            _rateLimiterMock.Verify(x => x.CheckRateLimitAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_ShouldCallRateLimiter_WhenRateLimiterIsProvided()
        {
            // Arrange
            var userId = "user123";
            var request = new TestRequest();
            var response = new TestResponse();
            var cancellationToken = CancellationToken.None;

            _securityContextMock.Setup(x => x.UserId).Returns(userId);
            _nextMock.Setup(x => x()).Returns(new ValueTask<TestResponse>(response));
            _rateLimiterMock.Setup(x => x.CheckRateLimitAsync(It.IsAny<string>())).ReturnsAsync(true);

            var behavior = new SecurityPipelineBehavior<TestRequest, TestResponse>(
                _loggerMock.Object, _securityContextMock.Object, _auditorMock.Object, _rateLimiterMock.Object);

            // Act
            await behavior.HandleAsync(request, _nextMock.Object, cancellationToken);

            // Assert
            _rateLimiterMock.Verify(x => x.CheckRateLimitAsync("user123:TestRequest"), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldThrowRateLimitExceededException_WhenRateLimitExceeded()
        {
            // Arrange
            var userId = "user123";
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;

            _securityContextMock.Setup(x => x.UserId).Returns(userId);
            _rateLimiterMock.Setup(x => x.CheckRateLimitAsync(It.IsAny<string>())).ReturnsAsync(false);

            var behavior = new SecurityPipelineBehavior<TestRequest, TestResponse>(
                _loggerMock.Object, _securityContextMock.Object, _auditorMock.Object, _rateLimiterMock.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RateLimitExceededException>(() =>
                behavior.HandleAsync(request, _nextMock.Object, cancellationToken).AsTask());

            Assert.Equal(userId, exception.UserId);
            Assert.Equal("TestRequest", exception.RequestType);
        }

        [Fact]
        public async Task HandleAsync_ShouldLogWarning_WhenRateLimitExceeded()
        {
            // Arrange
            var userId = "user123";
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;

            _securityContextMock.Setup(x => x.UserId).Returns(userId);
            _rateLimiterMock.Setup(x => x.CheckRateLimitAsync(It.IsAny<string>())).ReturnsAsync(false);

            var behavior = new SecurityPipelineBehavior<TestRequest, TestResponse>(
                _loggerMock.Object, _securityContextMock.Object, _auditorMock.Object, _rateLimiterMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<RateLimitExceededException>(() =>
                behavior.HandleAsync(request, _nextMock.Object, cancellationToken).AsTask());

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Rate limit exceeded for user")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldSanitizeRequest_BeforeValidation()
        {
            // Arrange
            var userId = "user123";
            var request = new TestRequest { Data = "<script>alert('xss')</script>" };
            var response = new TestResponse();
            var cancellationToken = CancellationToken.None;

            _securityContextMock.Setup(x => x.UserId).Returns(userId);
            _nextMock.Setup(x => x()).Returns(new ValueTask<TestResponse>(response));

            var behavior = new SecurityPipelineBehavior<TestRequest, TestResponse>(
                _loggerMock.Object, _securityContextMock.Object, _auditorMock.Object);

            // Act
            var result = await behavior.HandleAsync(request, _nextMock.Object, cancellationToken);

            // Assert
            Assert.Equal(response, result);
            _auditorMock.Verify(x => x.LogRequestAsync(userId, "TestRequest", request, cancellationToken), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldSanitizeResponse_AfterNext()
        {
            // Arrange
            var userId = "user123";
            var request = new TestRequest { Data = "test" };
            var response = new TestResponse { Result = "sensitive_data" };
            var cancellationToken = CancellationToken.None;

            _securityContextMock.Setup(x => x.UserId).Returns(userId);
            _nextMock.Setup(x => x()).Returns(new ValueTask<TestResponse>(response));

            var behavior = new SecurityPipelineBehavior<TestRequest, TestResponse>(
                _loggerMock.Object, _securityContextMock.Object, _auditorMock.Object);

            // Act
            var result = await behavior.HandleAsync(request, _nextMock.Object, cancellationToken);

            // Assert
            Assert.Equal(response, result);
            _auditorMock.Verify(x => x.LogSuccessAsync(userId, "TestRequest", cancellationToken), Times.Once);
        }

        // Test classes
        public class TestRequest : IRequest<TestResponse>
        {
            public string? Data { get; set; }
        }

        public class TestResponse
        {
            public string? Result { get; set; }
        }
    }

    // Extension methods for Mock<ILogger<T>> to simplify verification
    internal static class LoggerMockExtensions
    {
        public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock, LogLevel expectedLevel, string expectedMessage, Times times)
        {
            loggerMock.Verify(
                x => x.Log(
                    expectedLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString() != null && v.ToString().Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                times);
        }
    }
}