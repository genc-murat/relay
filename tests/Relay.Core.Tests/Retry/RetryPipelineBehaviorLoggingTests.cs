using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Configuration.Options;
using Relay.Core.Configuration.Options.Core;
using Relay.Core.Configuration.Options.Retry;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Retry;
using Xunit;
using TestRequest = Relay.Core.Tests.Retry.RetryPipelineBehaviorTestUtilities.TestRequest;
using TestResponse = Relay.Core.Tests.Retry.RetryPipelineBehaviorTestUtilities.TestResponse;

namespace Relay.Core.Tests.Retry;

public class RetryPipelineBehaviorLoggingTests
{
    private readonly Mock<ILogger<RetryPipelineBehavior<TestRequest, TestResponse>>> _loggerMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<RequestHandlerDelegate<TestResponse>> _nextMock;
    private readonly RelayOptions _relayOptions;
    private readonly RetryPipelineBehavior<TestRequest, TestResponse> _behavior;

    public RetryPipelineBehaviorLoggingTests()
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
}