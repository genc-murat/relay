using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Security;
using Xunit;

namespace Relay.Core.Tests.Security
{
    public class SecurityPipelineIntegrationTests
    {
        private readonly Mock<ILogger<SecurityPipelineBehavior<IntegrationTestRequest, IntegrationTestResponse>>> _loggerMock;
        private readonly Mock<ISecurityContext> _securityContextMock;
        private readonly Mock<IRequestAuditor> _auditorMock;
        private readonly Mock<IRateLimiter> _rateLimiterMock;

        public SecurityPipelineIntegrationTests()
        {
            _loggerMock = new Mock<ILogger<SecurityPipelineBehavior<IntegrationTestRequest, IntegrationTestResponse>>>();
            _securityContextMock = new Mock<ISecurityContext>();
            _auditorMock = new Mock<IRequestAuditor>();
            _rateLimiterMock = new Mock<IRateLimiter>();
        }

        [Fact]
        public async Task CompleteSecurityPipeline_ShouldWork_WhenAllChecksPass()
        {
            // Arrange
            var userId = "user123";
            var request = new IntegrationTestRequest { Data = "test data" };
            var response = new IntegrationTestResponse { Result = "success" };
            var cancellationToken = CancellationToken.None;
            var requiredPermissions = new[] { "read", "execute" };

            _securityContextMock.Setup(x => x.UserId).Returns(userId);
            _securityContextMock.Setup(x => x.HasPermissions(requiredPermissions)).Returns(true);
            _rateLimiterMock.Setup(x => x.CheckRateLimitAsync($"{userId}:IntegrationTestRequest")).ReturnsAsync(true);

            var nextMock = new Mock<RequestHandlerDelegate<IntegrationTestResponse>>();
            nextMock.Setup(x => x()).Returns(new ValueTask<IntegrationTestResponse>(response));

            var behavior = new SecurityPipelineBehavior<IntegrationTestRequest, IntegrationTestResponse>(
                _loggerMock.Object, _securityContextMock.Object, _auditorMock.Object, _rateLimiterMock.Object);

            // Act
            var result = await behavior.HandleAsync(request, nextMock.Object, cancellationToken);

            // Assert
            result.Should().Be(response);

            // Verify all security steps were called
            _auditorMock.Verify(x => x.LogRequestAsync(userId, "IntegrationTestRequest", request, cancellationToken), Times.Once);
            _rateLimiterMock.Verify(x => x.CheckRateLimitAsync($"{userId}:IntegrationTestRequest"), Times.Once);
            _auditorMock.Verify(x => x.LogSuccessAsync(userId, "IntegrationTestRequest", cancellationToken), Times.Once);
            _auditorMock.Verify(x => x.LogFailureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CompleteSecurityPipeline_ShouldWork_WithAllSecurityFeatures()
        {
            // Arrange
            var userId = "user123";
            var request = new IntegrationTestRequest { Data = "test data" };
            var response = new IntegrationTestResponse { Result = "success" };
            var cancellationToken = CancellationToken.None;

            _securityContextMock.Setup(x => x.UserId).Returns(userId);
            _securityContextMock.Setup(x => x.HasPermissions(It.IsAny<IEnumerable<string>>())).Returns(true);
            _rateLimiterMock.Setup(x => x.CheckRateLimitAsync($"{userId}:IntegrationTestRequest")).ReturnsAsync(true);

            var nextMock = new Mock<RequestHandlerDelegate<IntegrationTestResponse>>();
            nextMock.Setup(x => x()).Returns(new ValueTask<IntegrationTestResponse>(response));

            var behavior = new SecurityPipelineBehavior<IntegrationTestRequest, IntegrationTestResponse>(
                _loggerMock.Object, _securityContextMock.Object, _auditorMock.Object, _rateLimiterMock.Object);

            // Act
            var result = await behavior.HandleAsync(request, nextMock.Object, cancellationToken);

            // Assert
            result.Should().Be(response);

            // Verify all security steps were called
            _auditorMock.Verify(x => x.LogRequestAsync(userId, "IntegrationTestRequest", request, cancellationToken), Times.Once);
            _rateLimiterMock.Verify(x => x.CheckRateLimitAsync($"{userId}:IntegrationTestRequest"), Times.Once);
            _auditorMock.Verify(x => x.LogSuccessAsync(userId, "IntegrationTestRequest", cancellationToken), Times.Once);
            _auditorMock.Verify(x => x.LogFailureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CompleteSecurityPipeline_ShouldFailAtRateLimit_WhenRateLimitExceeded()
        {
            // Arrange
            var userId = "user123";
            var request = new IntegrationTestRequest { Data = "test data" };
            var cancellationToken = CancellationToken.None;

            _securityContextMock.Setup(x => x.UserId).Returns(userId);
            _securityContextMock.Setup(x => x.HasPermissions(It.IsAny<IEnumerable<string>>())).Returns(true);
            _rateLimiterMock.Setup(x => x.CheckRateLimitAsync($"{userId}:IntegrationTestRequest")).ReturnsAsync(false);

            var nextMock = new Mock<RequestHandlerDelegate<IntegrationTestResponse>>();

            var behavior = new SecurityPipelineBehavior<IntegrationTestRequest, IntegrationTestResponse>(
                _loggerMock.Object, _securityContextMock.Object, _auditorMock.Object, _rateLimiterMock.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RateLimitExceededException>(() =>
                behavior.HandleAsync(request, nextMock.Object, cancellationToken).AsTask());

            exception.UserId.Should().Be(userId);
            exception.RequestType.Should().Be("IntegrationTestRequest");

            // Verify pipeline stopped at rate limit check (before audit logging)
            _rateLimiterMock.Verify(x => x.CheckRateLimitAsync($"{userId}:IntegrationTestRequest"), Times.Once);
            nextMock.Verify(x => x(), Times.Never);
            _auditorMock.Verify(x => x.LogRequestAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
            _auditorMock.Verify(x => x.LogSuccessAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CompleteSecurityPipeline_ShouldLogFailure_WhenNextThrowsException()
        {
            // Arrange
            var userId = "user123";
            var request = new IntegrationTestRequest { Data = "test data" };
            var cancellationToken = CancellationToken.None;
            var exception = new InvalidOperationException("Business logic error");

            _securityContextMock.Setup(x => x.UserId).Returns(userId);
            _securityContextMock.Setup(x => x.HasPermissions(It.IsAny<IEnumerable<string>>())).Returns(true);
            _rateLimiterMock.Setup(x => x.CheckRateLimitAsync($"{userId}:IntegrationTestRequest")).ReturnsAsync(true);

            var nextMock = new Mock<RequestHandlerDelegate<IntegrationTestResponse>>();
            nextMock.Setup(x => x()).ThrowsAsync(exception);

            var behavior = new SecurityPipelineBehavior<IntegrationTestRequest, IntegrationTestResponse>(
                _loggerMock.Object, _securityContextMock.Object, _auditorMock.Object, _rateLimiterMock.Object);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                behavior.HandleAsync(request, nextMock.Object, cancellationToken).AsTask());

            thrownException.Should().Be(exception);

            // Verify failure was logged
            _auditorMock.Verify(x => x.LogRequestAsync(userId, "IntegrationTestRequest", request, cancellationToken), Times.Once);
            _rateLimiterMock.Verify(x => x.CheckRateLimitAsync($"{userId}:IntegrationTestRequest"), Times.Once);
            nextMock.Verify(x => x(), Times.Once);
            _auditorMock.Verify(x => x.LogFailureAsync(userId, "IntegrationTestRequest", exception, cancellationToken), Times.Once);
            _auditorMock.Verify(x => x.LogSuccessAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CompleteSecurityPipeline_ShouldWorkWithoutRateLimiter_WhenNotProvided()
        {
            // Arrange
            var userId = "user123";
            var request = new IntegrationTestRequest { Data = "test data" };
            var response = new IntegrationTestResponse { Result = "success" };
            var cancellationToken = CancellationToken.None;

            _securityContextMock.Setup(x => x.UserId).Returns(userId);
            _securityContextMock.Setup(x => x.HasPermissions(It.IsAny<IEnumerable<string>>())).Returns(true);

            var nextMock = new Mock<RequestHandlerDelegate<IntegrationTestResponse>>();
            nextMock.Setup(x => x()).Returns(new ValueTask<IntegrationTestResponse>(response));

            var behavior = new SecurityPipelineBehavior<IntegrationTestRequest, IntegrationTestResponse>(
                _loggerMock.Object, _securityContextMock.Object, _auditorMock.Object, null);

            // Act
            var result = await behavior.HandleAsync(request, nextMock.Object, cancellationToken);

            // Assert
            result.Should().Be(response);

            // Verify rate limiter was not called
            _auditorMock.Verify(x => x.LogRequestAsync(userId, "IntegrationTestRequest", request, cancellationToken), Times.Once);
            _rateLimiterMock.Verify(x => x.CheckRateLimitAsync(It.IsAny<string>()), Times.Never);
            _auditorMock.Verify(x => x.LogSuccessAsync(userId, "IntegrationTestRequest", cancellationToken), Times.Once);
        }

        [Fact]
        public async Task CompleteSecurityPipeline_ShouldHandleMultipleConcurrentRequests()
        {
            // Arrange
            var userId = "user123";
            var requests = Enumerable.Range(0, 10)
                .Select(i => new IntegrationTestRequest { Data = $"test data {i}" })
                .ToArray();
            var responses = Enumerable.Range(0, 10)
                .Select(i => new IntegrationTestResponse { Result = $"success {i}" })
                .ToArray();
            var cancellationToken = CancellationToken.None;

            _securityContextMock.Setup(x => x.UserId).Returns(userId);
            _securityContextMock.Setup(x => x.HasPermissions(It.IsAny<IEnumerable<string>>())).Returns(true);
            _rateLimiterMock.Setup(x => x.CheckRateLimitAsync($"{userId}:IntegrationTestRequest")).ReturnsAsync(true);

            var behavior = new SecurityPipelineBehavior<IntegrationTestRequest, IntegrationTestResponse>(
                _loggerMock.Object, _securityContextMock.Object, _auditorMock.Object, _rateLimiterMock.Object);

            var tasks = requests.Select((request, index) =>
            {
                var nextMock = new Mock<RequestHandlerDelegate<IntegrationTestResponse>>();
                nextMock.Setup(x => x()).Returns(new ValueTask<IntegrationTestResponse>(responses[index]));
                return behavior.HandleAsync(request, nextMock.Object, cancellationToken);
            }).ToArray();

            // Act
            var results = new IntegrationTestResponse[tasks.Length];
            for (int i = 0; i < tasks.Length; i++)
            {
                results[i] = await tasks[i];
            }

            // Assert
            results.Should().HaveCount(10);
            results.Should().BeEquivalentTo(responses);

            // Verify all requests were processed
            _auditorMock.Verify(x => x.LogRequestAsync(userId, "IntegrationTestRequest", It.IsAny<object>(), cancellationToken), Times.Exactly(10));
            _rateLimiterMock.Verify(x => x.CheckRateLimitAsync($"{userId}:IntegrationTestRequest"), Times.Exactly(10));
            _auditorMock.Verify(x => x.LogSuccessAsync(userId, "IntegrationTestRequest", cancellationToken), Times.Exactly(10));
        }

        // Test classes
        public class IntegrationTestRequest : IRequest<IntegrationTestResponse>
        {
            public string? Data { get; set; }
        }

        public class IntegrationTestResponse
        {
            public string? Result { get; set; }
        }


    }
}