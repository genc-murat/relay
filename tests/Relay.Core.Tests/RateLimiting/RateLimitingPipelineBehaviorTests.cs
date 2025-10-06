using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Configuration.Options;
using Relay.Core.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.RateLimiting
{
    public class RateLimitingPipelineBehaviorTests
    {
        public class NonRateLimitedRequest : IRequest<TestResponse> { }

        [RateLimit(10, 60, "UserKey")]
        public class RateLimitedRequest : IRequest<TestResponse>
        {
            public int Id { get; set; }
        }

        [RateLimit(5, 30, "ApiKey")]
        public class StrictRateLimitedRequest : IRequest<TestResponse> { }

        public class TestResponse
        {
            public string? Data { get; set; }
        }

        private readonly Mock<IRateLimiter> _rateLimiterMock;
        private readonly Mock<ILogger<RateLimitingPipelineBehavior<NonRateLimitedRequest, TestResponse>>> _loggerMock;
        private readonly Mock<IOptions<RelayOptions>> _optionsMock;
        private readonly RelayOptions _relayOptions;

        public RateLimitingPipelineBehaviorTests()
        {
            _rateLimiterMock = new Mock<IRateLimiter>();
            _loggerMock = new Mock<ILogger<RateLimitingPipelineBehavior<NonRateLimitedRequest, TestResponse>>>();
            _relayOptions = new RelayOptions
            {
                DefaultRateLimitingOptions = new RateLimitingOptions
                {
                    EnableAutomaticRateLimiting = false,
                    DefaultRequestsPerWindow = 100,
                    DefaultWindowSeconds = 60,
                    DefaultKey = "Global",
                    ThrowOnRateLimitExceeded = true
                }
            };
            _optionsMock = new Mock<IOptions<RelayOptions>>();
            _optionsMock.Setup(o => o.Value).Returns(_relayOptions);
        }

        private RateLimitingPipelineBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>()
            where TRequest : IRequest<TResponse>
        {
            var logger = new Mock<ILogger<RateLimitingPipelineBehavior<TRequest, TResponse>>>();
            return new RateLimitingPipelineBehavior<TRequest, TResponse>(
                _rateLimiterMock.Object,
                logger.Object,
                _optionsMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullRateLimiter_ShouldThrowArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new RateLimitingPipelineBehavior<NonRateLimitedRequest, TestResponse>(
                null!,
                _loggerMock.Object,
                _optionsMock.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("rateLimiter");
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new RateLimitingPipelineBehavior<NonRateLimitedRequest, TestResponse>(
                _rateLimiterMock.Object,
                null!,
                _optionsMock.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new RateLimitingPipelineBehavior<NonRateLimitedRequest, TestResponse>(
                _rateLimiterMock.Object,
                _loggerMock.Object,
                null!);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("options");
        }

        #endregion

        #region Rate Limiting Disabled Tests

        [Fact]
        public async Task HandleAsync_WhenRateLimitingDisabledGlobally_ShouldBypassRateLimiting()
        {
            // Arrange
            _relayOptions.DefaultRateLimitingOptions.EnableAutomaticRateLimiting = false;
            var behavior = CreateBehavior<NonRateLimitedRequest, TestResponse>();
            var request = new NonRateLimitedRequest();
            var nextCalled = false;
            var next = new RequestHandlerDelegate<TestResponse>(() =>
            {
                nextCalled = true;
                return new ValueTask<TestResponse>(new TestResponse { Data = "success" });
            });

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            nextCalled.Should().BeTrue();
            result.Data.Should().Be("success");
            _rateLimiterMock.Verify(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WithRateLimitAttribute_ShouldEnableRateLimiting()
        {
            // Arrange
            _relayOptions.DefaultRateLimitingOptions.EnableAutomaticRateLimiting = false;
            var behavior = CreateBehavior<RateLimitedRequest, TestResponse>();
            var request = new RateLimitedRequest { Id = 1 };

            _rateLimiterMock.Setup(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var next = new RequestHandlerDelegate<TestResponse>(() =>
                new ValueTask<TestResponse>(new TestResponse { Data = "success" }));

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            result.Data.Should().Be("success");
            _rateLimiterMock.Verify(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Rate Limiting Enabled Tests

        [Fact]
        public async Task HandleAsync_WhenRateLimitingEnabled_ShouldCheckRateLimit()
        {
            // Arrange
            _relayOptions.DefaultRateLimitingOptions.EnableAutomaticRateLimiting = true;
            var behavior = CreateBehavior<NonRateLimitedRequest, TestResponse>();
            var request = new NonRateLimitedRequest();

            _rateLimiterMock.Setup(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var next = new RequestHandlerDelegate<TestResponse>(() =>
                new ValueTask<TestResponse>(new TestResponse { Data = "success" }));

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            result.Data.Should().Be("success");
            _rateLimiterMock.Verify(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WhenRateLimitAllowed_ShouldCallNext()
        {
            // Arrange
            _relayOptions.DefaultRateLimitingOptions.EnableAutomaticRateLimiting = true;
            var behavior = CreateBehavior<NonRateLimitedRequest, TestResponse>();
            var request = new NonRateLimitedRequest();
            var nextCalled = false;

            _rateLimiterMock.Setup(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var next = new RequestHandlerDelegate<TestResponse>(() =>
            {
                nextCalled = true;
                return new ValueTask<TestResponse>(new TestResponse { Data = "success" });
            });

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            nextCalled.Should().BeTrue();
        }

        [Fact]
        public async Task HandleAsync_WhenRateLimitExceededAndThrowEnabled_ShouldThrowException()
        {
            // Arrange
            _relayOptions.DefaultRateLimitingOptions.EnableAutomaticRateLimiting = true;
            _relayOptions.DefaultRateLimitingOptions.ThrowOnRateLimitExceeded = true;
            var behavior = CreateBehavior<NonRateLimitedRequest, TestResponse>();
            var request = new NonRateLimitedRequest();
            var retryAfter = TimeSpan.FromSeconds(30);

            _rateLimiterMock.Setup(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _rateLimiterMock.Setup(r => r.GetRetryAfterAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(retryAfter);

            var next = new RequestHandlerDelegate<TestResponse>(() =>
                new ValueTask<TestResponse>(new TestResponse { Data = "success" }));

            // Act
            Func<Task> act = async () => await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<RateLimitExceededException>()
                .Where(e => e.RetryAfter == retryAfter);
        }

        [Fact]
        public async Task HandleAsync_WhenRateLimitExceededAndThrowDisabled_ShouldContinue()
        {
            // Arrange
            _relayOptions.DefaultRateLimitingOptions.EnableAutomaticRateLimiting = true;
            _relayOptions.DefaultRateLimitingOptions.ThrowOnRateLimitExceeded = false;
            var behavior = CreateBehavior<NonRateLimitedRequest, TestResponse>();
            var request = new NonRateLimitedRequest();
            var nextCalled = false;

            _rateLimiterMock.Setup(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var next = new RequestHandlerDelegate<TestResponse>(() =>
            {
                nextCalled = true;
                return new ValueTask<TestResponse>(new TestResponse { Data = "success" });
            });

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            nextCalled.Should().BeTrue();
            result.Data.Should().Be("success");
        }

        #endregion

        #region Rate Limit Key Generation Tests

        [Fact]
        public async Task HandleAsync_WithGlobalKey_ShouldGenerateGlobalKey()
        {
            // Arrange
            _relayOptions.DefaultRateLimitingOptions.EnableAutomaticRateLimiting = true;
            _relayOptions.DefaultRateLimitingOptions.DefaultKey = "Global";
            var behavior = CreateBehavior<NonRateLimitedRequest, TestResponse>();
            var request = new NonRateLimitedRequest();
            string? capturedKey = null;

            _rateLimiterMock.Setup(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((key, ct) => capturedKey = key)
                .ReturnsAsync(true);

            var next = new RequestHandlerDelegate<TestResponse>(() =>
                new ValueTask<TestResponse>(new TestResponse()));

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            capturedKey.Should().Be("Global");
        }

        [Fact]
        public async Task HandleAsync_WithTypeKey_ShouldGenerateTypeBasedKey()
        {
            // Arrange
            _relayOptions.DefaultRateLimitingOptions.EnableAutomaticRateLimiting = true;
            _relayOptions.DefaultRateLimitingOptions.DefaultKey = "Type";
            var behavior = CreateBehavior<NonRateLimitedRequest, TestResponse>();
            var request = new NonRateLimitedRequest();
            string? capturedKey = null;

            _rateLimiterMock.Setup(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((key, ct) => capturedKey = key)
                .ReturnsAsync(true);

            var next = new RequestHandlerDelegate<TestResponse>(() =>
                new ValueTask<TestResponse>(new TestResponse()));

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            capturedKey.Should().Be(typeof(NonRateLimitedRequest).FullName ?? typeof(NonRateLimitedRequest).Name);
        }

        [Fact]
        public async Task HandleAsync_WithCustomKey_ShouldGenerateCustomKey()
        {
            // Arrange
            _relayOptions.DefaultRateLimitingOptions.EnableAutomaticRateLimiting = true;
            _relayOptions.DefaultRateLimitingOptions.DefaultKey = "Custom";
            var behavior = CreateBehavior<NonRateLimitedRequest, TestResponse>();
            var request = new NonRateLimitedRequest();
            string? capturedKey = null;

            _rateLimiterMock.Setup(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((key, ct) => capturedKey = key)
                .ReturnsAsync(true);

            var next = new RequestHandlerDelegate<TestResponse>(() =>
                new ValueTask<TestResponse>(new TestResponse()));

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            capturedKey.Should().Be($"Custom:{typeof(NonRateLimitedRequest).FullName ?? typeof(NonRateLimitedRequest).Name}");
        }

        [Fact]
        public async Task HandleAsync_WithAttributeKey_ShouldUseAttributeKey()
        {
            // Arrange
            var behavior = CreateBehavior<RateLimitedRequest, TestResponse>();
            var request = new RateLimitedRequest { Id = 1 };
            string? capturedKey = null;

            _rateLimiterMock.Setup(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((key, ct) => capturedKey = key)
                .ReturnsAsync(true);

            var next = new RequestHandlerDelegate<TestResponse>(() =>
                new ValueTask<TestResponse>(new TestResponse()));

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            capturedKey.Should().Be($"UserKey:{typeof(RateLimitedRequest).FullName ?? typeof(RateLimitedRequest).Name}");
        }

        #endregion

        #region Handler-Specific Overrides Tests

        [Fact]
        public async Task HandleAsync_WithHandlerOverride_ShouldUseOverrideOptions()
        {
            // Arrange
            var handlerKey = typeof(NonRateLimitedRequest).FullName ?? typeof(NonRateLimitedRequest).Name;
            _relayOptions.RateLimitingOverrides[handlerKey] = new RateLimitingOptions
            {
                EnableAutomaticRateLimiting = true,
                DefaultRequestsPerWindow = 50,
                DefaultWindowSeconds = 30,
                ThrowOnRateLimitExceeded = true
            };

            var behavior = CreateBehavior<NonRateLimitedRequest, TestResponse>();
            var request = new NonRateLimitedRequest();

            _rateLimiterMock.Setup(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var next = new RequestHandlerDelegate<TestResponse>(() =>
                new ValueTask<TestResponse>(new TestResponse { Data = "success" }));

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            result.Data.Should().Be("success");
            _rateLimiterMock.Verify(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Cancellation Token Tests

        [Fact]
        public async Task HandleAsync_ShouldPassCancellationToken()
        {
            // Arrange
            _relayOptions.DefaultRateLimitingOptions.EnableAutomaticRateLimiting = true;
            var behavior = CreateBehavior<NonRateLimitedRequest, TestResponse>();
            var request = new NonRateLimitedRequest();
            var cts = new CancellationTokenSource();
            CancellationToken capturedToken = default;

            _rateLimiterMock.Setup(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((key, ct) => capturedToken = ct)
                .ReturnsAsync(true);

            var next = new RequestHandlerDelegate<TestResponse>(() =>
                new ValueTask<TestResponse>(new TestResponse()));

            // Act
            await behavior.HandleAsync(request, next, cts.Token);

            // Assert
            capturedToken.Should().Be(cts.Token);
        }

        [Fact]
        public async Task HandleAsync_WhenRateLimitExceeded_ShouldPassCancellationTokenToGetRetryAfter()
        {
            // Arrange
            _relayOptions.DefaultRateLimitingOptions.EnableAutomaticRateLimiting = true;
            _relayOptions.DefaultRateLimitingOptions.ThrowOnRateLimitExceeded = true;
            var behavior = CreateBehavior<NonRateLimitedRequest, TestResponse>();
            var request = new NonRateLimitedRequest();
            var cts = new CancellationTokenSource();
            CancellationToken capturedToken = default;

            _rateLimiterMock.Setup(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _rateLimiterMock.Setup(r => r.GetRetryAfterAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((key, ct) => capturedToken = ct)
                .ReturnsAsync(TimeSpan.FromSeconds(30));

            var next = new RequestHandlerDelegate<TestResponse>(() =>
                new ValueTask<TestResponse>(new TestResponse()));

            // Act
            try
            {
                await behavior.HandleAsync(request, next, cts.Token);
            }
            catch (RateLimitExceededException)
            {
                // Expected
            }

            // Assert
            capturedToken.Should().Be(cts.Token);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task HandleAsync_MultipleRequests_ShouldCheckRateLimitForEach()
        {
            // Arrange
            _relayOptions.DefaultRateLimitingOptions.EnableAutomaticRateLimiting = true;
            var behavior = CreateBehavior<NonRateLimitedRequest, TestResponse>();
            var request = new NonRateLimitedRequest();
            var callCount = 0;

            _rateLimiterMock.Setup(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback(() => callCount++)
                .ReturnsAsync(true);

            var next = new RequestHandlerDelegate<TestResponse>(() =>
                new ValueTask<TestResponse>(new TestResponse { Data = "success" }));

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);
            await behavior.HandleAsync(request, next, CancellationToken.None);
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            callCount.Should().Be(3);
        }

        [Fact]
        public async Task HandleAsync_DifferentRequestTypes_ShouldUseDifferentKeys()
        {
            // Arrange
            _relayOptions.DefaultRateLimitingOptions.EnableAutomaticRateLimiting = true;
            _relayOptions.DefaultRateLimitingOptions.DefaultKey = "Type";

            var behavior1 = CreateBehavior<NonRateLimitedRequest, TestResponse>();
            var behavior2 = CreateBehavior<RateLimitedRequest, TestResponse>();
            var request1 = new NonRateLimitedRequest();
            var request2 = new RateLimitedRequest { Id = 1 };

            var capturedKeys = new System.Collections.Generic.List<string>();

            _rateLimiterMock.Setup(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((key, ct) => capturedKeys.Add(key))
                .ReturnsAsync(true);

            var next1 = new RequestHandlerDelegate<TestResponse>(() =>
                new ValueTask<TestResponse>(new TestResponse()));
            var next2 = new RequestHandlerDelegate<TestResponse>(() =>
                new ValueTask<TestResponse>(new TestResponse()));

            // Act
            await behavior1.HandleAsync(request1, next1, CancellationToken.None);
            await behavior2.HandleAsync(request2, next2, CancellationToken.None);

            // Assert
            capturedKeys.Should().HaveCount(2);
            capturedKeys[0].Should().Be(typeof(NonRateLimitedRequest).FullName ?? typeof(NonRateLimitedRequest).Name);
            // RateLimitedRequest has a RateLimit attribute with "UserKey", so it should use that
            capturedKeys[1].Should().Contain("UserKey");
        }

        #endregion

        #region Logging Tests

        [Fact]
        public async Task HandleAsync_WhenRateLimitExceeded_ShouldLogWarning()
        {
            // Arrange
            _relayOptions.DefaultRateLimitingOptions.EnableAutomaticRateLimiting = true;
            _relayOptions.DefaultRateLimitingOptions.ThrowOnRateLimitExceeded = false;
            var logger = new Mock<ILogger<RateLimitingPipelineBehavior<NonRateLimitedRequest, TestResponse>>>();
            var behavior = new RateLimitingPipelineBehavior<NonRateLimitedRequest, TestResponse>(
                _rateLimiterMock.Object,
                logger.Object,
                _optionsMock.Object);
            var request = new NonRateLimitedRequest();

            _rateLimiterMock.Setup(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var next = new RequestHandlerDelegate<TestResponse>(() =>
                new ValueTask<TestResponse>(new TestResponse()));

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            logger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Rate limit exceeded")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion
    }
}
