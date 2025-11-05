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

public class RetryPipelineBehaviorCancellationTests
{
    private readonly Mock<ILogger<RetryPipelineBehavior<TestRequest, TestResponse>>> _loggerMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<RequestHandlerDelegate<TestResponse>> _nextMock;
    private readonly RelayOptions _relayOptions;
    private readonly RetryPipelineBehavior<TestRequest, TestResponse> _behavior;

    public RetryPipelineBehaviorCancellationTests()
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
}
