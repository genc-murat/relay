using System;
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

namespace Relay.Core.Tests.Retry;

public class RetryPipelineBehaviorExhaustionTests
{
    private readonly Mock<ILogger<RetryPipelineBehavior<TestRequest, TestResponse>>> _loggerMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<RequestHandlerDelegate<TestResponse>> _nextMock;
    private readonly RelayOptions _relayOptions;
    private readonly RetryPipelineBehavior<TestRequest, TestResponse> _behavior;

    public RetryPipelineBehaviorExhaustionTests()
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
}