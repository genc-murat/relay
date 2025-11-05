using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Configuration.Options;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Retry;
using Xunit;
using TestRequest = Relay.Core.Tests.Retry.RetryPipelineBehaviorTestUtilities.TestRequest;
using TestResponse = Relay.Core.Tests.Retry.RetryPipelineBehaviorTestUtilities.TestResponse;
using TestRequestWithCustomStrategy = Relay.Core.Tests.Retry.RetryPipelineBehaviorTestUtilities.TestRequestWithCustomStrategy;
using CustomRetryStrategy = Relay.Core.Tests.Retry.RetryPipelineBehaviorTestUtilities.CustomRetryStrategy;
using Relay.Core.Configuration.Options.Core;
using Relay.Core.Configuration.Options.Retry;
using Relay.Core.Retry.Strategies;

namespace Relay.Core.Tests.Retry;

public class RetryPipelineBehaviorStrategyTests
{
    private readonly Mock<ILogger<RetryPipelineBehavior<TestRequest, TestResponse>>> _loggerMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<RequestHandlerDelegate<TestResponse>> _nextMock;
    private readonly RelayOptions _relayOptions;
    private readonly RetryPipelineBehavior<TestRequest, TestResponse> _behavior;

    public RetryPipelineBehaviorStrategyTests()
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
}
