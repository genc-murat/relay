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

namespace Relay.Core.Tests.Retry;

public class RetryPipelineBehaviorOverrideTests
{
    private readonly Mock<ILogger<RetryPipelineBehavior<TestRequest, TestResponse>>> _loggerMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<RequestHandlerDelegate<TestResponse>> _nextMock;
    private readonly RelayOptions _relayOptions;
    private readonly RetryPipelineBehavior<TestRequest, TestResponse> _behavior;

    public RetryPipelineBehaviorOverrideTests()
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
}