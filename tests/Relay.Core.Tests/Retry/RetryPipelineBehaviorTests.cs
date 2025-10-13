using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Configuration.Options;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Retry;
using Xunit;

namespace Relay.Core.Tests.Retry
{
    public class RetryPipelineBehaviorTests
    {
        private readonly Mock<ILogger<RetryPipelineBehavior<TestRequest, TestResponse>>> _loggerMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<RequestHandlerDelegate<TestResponse>> _nextMock;
        private readonly RelayOptions _relayOptions;
        private RetryPipelineBehavior<TestRequest, TestResponse> _behavior;

        public RetryPipelineBehaviorTests()
        {
            _loggerMock = new Mock<ILogger<RetryPipelineBehavior<TestRequest, TestResponse>>>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _nextMock = new Mock<RequestHandlerDelegate<TestResponse>>();
            _relayOptions = new RelayOptions
            {
                DefaultRetryOptions = new RetryOptions()
            };

            _behavior = new RetryPipelineBehavior<TestRequest, TestResponse>(
                _loggerMock.Object,
                Options.Create(_relayOptions),
                _serviceProviderMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Act
            Action act = () => new RetryPipelineBehavior<TestRequest, TestResponse>(
                null!,
                Options.Create(_relayOptions),
                _serviceProviderMock.Object);

            // Assert
            var ex = Assert.Throws<ArgumentNullException>(act);
            Assert.Equal("logger", ex.ParamName);
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_WhenOptionsIsNull()
        {
            // Act
            Action act = () => new RetryPipelineBehavior<TestRequest, TestResponse>(
                _loggerMock.Object,
                null!,
                _serviceProviderMock.Object);

            // Assert
            var ex = Assert.Throws<ArgumentNullException>(act);
            Assert.Equal("options", ex.ParamName);
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_WhenServiceProviderIsNull()
        {
            // Act
            Action act = () => new RetryPipelineBehavior<TestRequest, TestResponse>(
                _loggerMock.Object,
                Options.Create(_relayOptions),
                null!);

            // Assert
            var ex = Assert.Throws<ArgumentNullException>(act);
            Assert.Equal("serviceProvider", ex.ParamName);
        }

        [Fact]
        public void Constructor_Should_InitializeSuccessfully_WithValidParameters()
        {
            // Act
            var behavior = new RetryPipelineBehavior<TestRequest, TestResponse>(
                _loggerMock.Object,
                Options.Create(_relayOptions),
                _serviceProviderMock.Object);

            // Assert
            Assert.NotNull(behavior);
        }

        #endregion

        #region Retry Disabled Tests

        [Fact]
        public async Task HandleAsync_Should_CallNextDirectly_WhenRetryIsDisabledGloballyAndNoAttribute()
        {
            // Arrange
            _relayOptions.DefaultRetryOptions.EnableAutomaticRetry = false;
            var request = new TestRequest();
            var expectedResponse = new TestResponse();
            _nextMock.Setup(x => x()).ReturnsAsync(expectedResponse);
 
            // Act
            var result = await _behavior.HandleAsync(request, _nextMock.Object, CancellationToken.None);
 
            // Assert
            Assert.Equal(expectedResponse, result);
            _nextMock.Verify(x => x(), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_Retry_WhenRetryIsDisabledGloballyButAttributeExists()
        {
            // Arrange
            _relayOptions.DefaultRetryOptions.EnableAutomaticRetry = false;
            var request = new TestRequestWithAttribute();
            var expectedResponse = new TestResponse();
            var callCount = 0;
            var loggerMock = new Mock<ILogger<RetryPipelineBehavior<TestRequestWithAttribute, TestResponse>>>();

            _nextMock.Setup(x => x()).Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("First attempt failed");
                }
                return new ValueTask<TestResponse>(expectedResponse);
            });

            // Act
            var behavior = new RetryPipelineBehavior<TestRequestWithAttribute, TestResponse>(
                loggerMock.Object,
                Options.Create(_relayOptions),
                _serviceProviderMock.Object);
            var result = await behavior.HandleAsync(request, _nextMock.Object, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, result);
            _nextMock.Verify(x => x(), Times.Exactly(2));
            Assert.Equal(2, callCount);
        }

        #endregion

        #region Retry Enabled Tests

        [Fact]
        public async Task HandleAsync_Should_CallNextDirectly_WhenRetryIsEnabledGlobally()
        {
            // Arrange
            _relayOptions.DefaultRetryOptions.EnableAutomaticRetry = true;
            var request = new TestRequest();
            var expectedResponse = new TestResponse();
            _nextMock.Setup(x => x()).ReturnsAsync(expectedResponse);
 
            // Act
            var result = await _behavior.HandleAsync(request, _nextMock.Object, CancellationToken.None);
 
            // Assert
            Assert.Equal(expectedResponse, result);
            _nextMock.Verify(x => x(), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_Retry_WhenRetryIsEnabledGloballyAndFirstCallFails()
        {
            // Arrange
            _relayOptions.DefaultRetryOptions.EnableAutomaticRetry = true;
            _relayOptions.DefaultRetryOptions.DefaultMaxRetryAttempts = 3;
            _relayOptions.DefaultRetryOptions.DefaultRetryDelayMilliseconds = 10;

            var request = new TestRequest();
            var expectedResponse = new TestResponse();
            var callCount = 0;

            _nextMock.Setup(x => x()).Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("First attempt failed");
                }
                return new ValueTask<TestResponse>(expectedResponse);
            });

            // Act
            var result = await _behavior.HandleAsync(request, _nextMock.Object, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, result);
            _nextMock.Verify(x => x(), Times.Exactly(2));
            Assert.Equal(2, callCount);
        }

        [Fact]
        public async Task HandleAsync_Should_UseAttributeParameters_WhenAttributeExists()
        {
            // Arrange
            _relayOptions.DefaultRetryOptions.EnableAutomaticRetry = false;
            var request = new TestRequestWithAttribute();
            var expectedResponse = new TestResponse();
            var callCount = 0;
            var loggerMock = new Mock<ILogger<RetryPipelineBehavior<TestRequestWithAttribute, TestResponse>>>();

            _nextMock.Setup(x => x()).Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("First attempt failed");
                }
                return new ValueTask<TestResponse>(expectedResponse);
            });

            // Act
            var behavior = new RetryPipelineBehavior<TestRequestWithAttribute, TestResponse>(
                loggerMock.Object,
                Options.Create(_relayOptions),
                _serviceProviderMock.Object);
            var result = await behavior.HandleAsync(request, _nextMock.Object, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, result);
            _nextMock.Verify(x => x(), Times.Exactly(2));
            Assert.Equal(2, callCount);
        }

        #endregion

        #region Retry Strategy Tests

        [Fact]
        public async Task HandleAsync_Should_UseLinearStrategy_WhenConfigured()
        {
            // Arrange
            _relayOptions.DefaultRetryOptions.EnableAutomaticRetry = true;
            _relayOptions.DefaultRetryOptions.DefaultRetryStrategy = "Linear";
            _relayOptions.DefaultRetryOptions.DefaultMaxRetryAttempts = 2;
            _relayOptions.DefaultRetryOptions.DefaultRetryDelayMilliseconds = 50;

            var request = new TestRequest();
            var expectedResponse = new TestResponse();
            var callCount = 0;
            var timestamps = new List<DateTime>();

            _nextMock.Setup(x => x()).Returns(() =>
            {
                timestamps.Add(DateTime.UtcNow);
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("First attempt failed");
                }
                return new ValueTask<TestResponse>(expectedResponse);
            });

            // Act
            var startTime = DateTime.UtcNow;
            var result = await _behavior.HandleAsync(request, _nextMock.Object, CancellationToken.None);
            var endTime = DateTime.UtcNow;

            // Assert
            Assert.Equal(expectedResponse, result);
            _nextMock.Verify(x => x(), Times.Exactly(2));
            Assert.Equal(2, callCount);
 
            // Verify delay was applied (should be at least 50ms between calls)
            var timeDiff = timestamps[1] - timestamps[0];
            Assert.True(timeDiff.TotalMilliseconds >= 40); // Allow some tolerance
        }

        [Fact]
        public async Task HandleAsync_Should_UseExponentialStrategy_WhenConfigured()
        {
            // Arrange
            _relayOptions.DefaultRetryOptions.EnableAutomaticRetry = true;
            _relayOptions.DefaultRetryOptions.DefaultRetryStrategy = "exponential";
            _relayOptions.DefaultRetryOptions.DefaultMaxRetryAttempts = 3;
            _relayOptions.DefaultRetryOptions.DefaultRetryDelayMilliseconds = 10;

            var request = new TestRequest();
            var expectedResponse = new TestResponse();
            var callCount = 0;

            _nextMock.Setup(x => x()).Returns(() =>
            {
                callCount++;
                if (callCount < 3)
                {
                    throw new InvalidOperationException("Attempt failed");
                }
                return new ValueTask<TestResponse>(expectedResponse);
            });

            // Act
            var result = await _behavior.HandleAsync(request, _nextMock.Object, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, result);
            _nextMock.Verify(x => x(), Times.Exactly(3));
            Assert.Equal(3, callCount);
        }

        [Fact]
        public async Task HandleAsync_Should_UseCircuitBreakerStrategy_WhenConfigured()
        {
            // Arrange
            _relayOptions.DefaultRetryOptions.EnableAutomaticRetry = true;
            _relayOptions.DefaultRetryOptions.DefaultRetryStrategy = "circuitbreaker";
            _relayOptions.DefaultRetryOptions.DefaultMaxRetryAttempts = 2;
            _relayOptions.DefaultRetryOptions.DefaultRetryDelayMilliseconds = 10;
 
            var request = new TestRequest();
            var expectedResponse = new TestResponse();
            var callCount = 0;
 
            _nextMock.Setup(x => x()).Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("First attempt failed");
                }
                return new ValueTask<TestResponse>(expectedResponse);
            });
 
            // Act
            var result = await _behavior.HandleAsync(request, _nextMock.Object, CancellationToken.None);
 
            // Assert
            Assert.Equal(expectedResponse, result);
            _nextMock.Verify(x => x(), Times.Exactly(2));
            Assert.Equal(2, callCount);
        }

        [Fact]
        public async Task HandleAsync_Should_UseCustomStrategy_WhenProvidedInAttribute()
        {
            // Arrange
            var customStrategy = new Mock<IRetryStrategy>();
            customStrategy.Setup(x => x.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(true);
            customStrategy.Setup(x => x.GetRetryDelayAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(TimeSpan.FromMilliseconds(10));

            _serviceProviderMock.Setup(x => x.GetService(typeof(CustomRetryStrategy)))
                              .Returns(customStrategy.Object);

            var request = new TestRequestWithCustomStrategy();
            var expectedResponse = new TestResponse();
            var callCount = 0;
            var loggerMock = new Mock<ILogger<RetryPipelineBehavior<TestRequestWithCustomStrategy, TestResponse>>>();

            _nextMock.Setup(x => x()).Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("First attempt failed");
                }
                return new ValueTask<TestResponse>(expectedResponse);
            });

            // Act
            var behavior = new RetryPipelineBehavior<TestRequestWithCustomStrategy, TestResponse>(
                loggerMock.Object,
                Options.Create(_relayOptions),
                _serviceProviderMock.Object);
            var result = await behavior.HandleAsync(request, _nextMock.Object, CancellationToken.None);
 
            // Assert
            Assert.Equal(expectedResponse, result);
            _nextMock.Verify(x => x(), Times.Exactly(2)); // 1 initial + 1 retry (maxRetryAttempts: 2 means 2 total attempts)
            Assert.Equal(2, callCount);
        }

        #endregion

        #region Retry Exhaustion Tests

        [Fact]
        public async Task HandleAsync_Should_ThrowRetryExhaustedException_WhenAllAttemptsFailAndThrowOnExhaustedIsTrue()
        {
            // Arrange
            _relayOptions.DefaultRetryOptions.EnableAutomaticRetry = true;
            _relayOptions.DefaultRetryOptions.DefaultMaxRetryAttempts = 2;
            _relayOptions.DefaultRetryOptions.ThrowOnRetryExhausted = true;

            var request = new TestRequest();
            var exception = new InvalidOperationException("Always fails");

            _nextMock.Setup(x => x()).ThrowsAsync(exception);

            // Act
            Func<Task> act = async () => await _behavior.HandleAsync(request, _nextMock.Object, CancellationToken.None);

            // Assert
            var resultException = await Assert.ThrowsAsync<RetryExhaustedException>(act);
            Assert.Equal(2, resultException.Exceptions.Count);
            Assert.All(resultException.Exceptions, ex => Assert.IsType<InvalidOperationException>(ex));
        }

        [Fact]
        public async Task HandleAsync_Should_ThrowLastException_WhenAllAttemptsFailAndThrowOnExhaustedIsFalse()
        {
            // Arrange
            _relayOptions.DefaultRetryOptions.EnableAutomaticRetry = true;
            _relayOptions.DefaultRetryOptions.DefaultMaxRetryAttempts = 2;
            _relayOptions.DefaultRetryOptions.ThrowOnRetryExhausted = false;

            var request = new TestRequest();
            var exception = new InvalidOperationException("Always fails");

            _nextMock.Setup(x => x()).ThrowsAsync(exception);

            // Act
            Func<Task> act = async () => await _behavior.HandleAsync(request, _nextMock.Object, CancellationToken.None);

            // Assert
            var resultException = await Assert.ThrowsAsync<InvalidOperationException>(act);
            Assert.Equal("Always fails", resultException.Message);
        }

        [Fact]
        public async Task HandleAsync_Should_StopRetrying_WhenStrategyReturnsFalse()
        {
            // Arrange
            var customStrategy = new Mock<IRetryStrategy>();
            customStrategy.Setup(x => x.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(false);

            _serviceProviderMock.Setup(x => x.GetService(typeof(CustomRetryStrategy)))
                              .Returns(customStrategy.Object);

            var request = new TestRequestWithCustomStrategy();
            var exception = new InvalidOperationException("First attempt failed");
            var loggerMock = new Mock<ILogger<RetryPipelineBehavior<TestRequestWithCustomStrategy, TestResponse>>>();
            var options = new RelayOptions
            {
                DefaultRetryOptions = new RetryOptions
                {
                    ThrowOnRetryExhausted = false // Don't throw RetryExhaustedException
                }
            };

            _nextMock.Setup(x => x()).ThrowsAsync(exception);

            // Act
            var behavior = new RetryPipelineBehavior<TestRequestWithCustomStrategy, TestResponse>(
                loggerMock.Object,
                Options.Create(options),
                _serviceProviderMock.Object);
            Func<Task> act = async () => await behavior.HandleAsync(request, _nextMock.Object, CancellationToken.None);

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(act);
            _nextMock.Verify(x => x(), Times.Once); // Only called once, no retries
            customStrategy.Verify(x => x.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Handler Override Tests

        [Fact]
        public async Task HandleAsync_Should_UseHandlerOverride_WhenConfigured()
        {
            // Arrange
            _relayOptions.DefaultRetryOptions.EnableAutomaticRetry = true; // Enable globally first
            _relayOptions.DefaultRetryOptions.DefaultMaxRetryAttempts = 10; // Set high global limit
            
            var handlerOptions = new RetryOptions
            {
                EnableAutomaticRetry = true,
                DefaultMaxRetryAttempts = 5,
                DefaultRetryDelayMilliseconds = 200
            };

            _relayOptions.RetryOverrides["Relay.Core.Tests.Retry.TestRequest"] = handlerOptions;

            var request = new TestRequest();
            var expectedResponse = new TestResponse();
            var callCount = 0;

            _nextMock.Setup(x => x()).Returns(() =>
            {
                callCount++;
                if (callCount <= 4) // Fail first 4 attempts, succeed on 5th
                {
                    throw new InvalidOperationException("Attempt failed");
                }
                return new ValueTask<TestResponse>(expectedResponse);
            });

            // Act
            var result = await _behavior.HandleAsync(request, _nextMock.Object, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, result);
            _nextMock.Verify(x => x(), Times.Exactly(5)); // 1 initial + 4 retries
            Assert.Equal(5, callCount);
        }

        #endregion

        #region Logging Tests



        [Fact]
        public async Task HandleAsync_Should_LogWarning_WhenAttemptFails()
        {
            // Arrange
            _relayOptions.DefaultRetryOptions.EnableAutomaticRetry = true;
            _relayOptions.DefaultRetryOptions.DefaultMaxRetryAttempts = 2;
            _relayOptions.DefaultRetryOptions.ThrowOnRetryExhausted = false;

            var request = new TestRequest();
            var exception = new InvalidOperationException("Test failure");

            _nextMock.Setup(x => x()).ThrowsAsync(exception);

            // Act
            Func<Task> act = async () => await _behavior.HandleAsync(request, _nextMock.Object, CancellationToken.None);

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(act);

            // Verify logging calls
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed on attempt")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task HandleAsync_Should_LogError_WhenAllAttemptsExhausted()
        {
            // Arrange
            _relayOptions.DefaultRetryOptions.EnableAutomaticRetry = true;
            _relayOptions.DefaultRetryOptions.DefaultMaxRetryAttempts = 2;
            _relayOptions.DefaultRetryOptions.ThrowOnRetryExhausted = true;

            var request = new TestRequest();
            var exception = new InvalidOperationException("Always fails");

            _nextMock.Setup(x => x()).ThrowsAsync(exception);

            // Act
            Func<Task> act = async () => await _behavior.HandleAsync(request, _nextMock.Object, CancellationToken.None);

            // Assert
            await Assert.ThrowsAsync<RetryExhaustedException>(act);

            // Verify logging calls
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("All retry attempts exhausted")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Cancellation Tests

        [Fact]
        public async Task HandleAsync_Should_RespectCancellationToken()
        {
            // Arrange
            _relayOptions.DefaultRetryOptions.EnableAutomaticRetry = true;
            _relayOptions.DefaultRetryOptions.DefaultMaxRetryAttempts = 3;
            _relayOptions.DefaultRetryOptions.DefaultRetryDelayMilliseconds = 1000;
            _relayOptions.DefaultRetryOptions.ThrowOnRetryExhausted = false;

            var request = new TestRequest();
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

            _nextMock.Setup(x => x()).ThrowsAsync(new InvalidOperationException("Always fails"));

            // Act
            Func<Task> act = async () => await _behavior.HandleAsync(request, _nextMock.Object, cts.Token);

            // Assert
            await Assert.ThrowsAsync<TaskCanceledException>(act);
        }

        #endregion

        #region Test Classes

        public class TestRequest { }
        public class TestResponse { }

        [Retry(maxRetryAttempts: 2, retryDelayMilliseconds: 50)]
        public class TestRequestWithAttribute { }

        [Retry(typeof(CustomRetryStrategy), maxRetryAttempts: 2)]
        public class TestRequestWithCustomStrategy { }

        public class CustomRetryStrategy : IRetryStrategy
        {
            public ValueTask<bool> ShouldRetryAsync(int attempt, Exception exception, CancellationToken cancellationToken = default)
            {
                return new ValueTask<bool>(attempt < 3);
            }

            public ValueTask<TimeSpan> GetRetryDelayAsync(int attempt, Exception exception, CancellationToken cancellationToken = default)
            {
                return new ValueTask<TimeSpan>(TimeSpan.FromMilliseconds(10));
            }
        }

        #endregion
    }
}