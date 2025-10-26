using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker;
using System.Collections.Concurrent;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class MessageBrokerHostedServiceEdgeCaseTests
{
    public class TestableHostedService(
        IMessageBroker messageBroker,
        ILogger logger) : IHostedService
    {
        private readonly IMessageBroker _messageBroker = messageBroker;
        private readonly ILogger _logger = logger;

        public bool StartShouldFail { get; set; }
        public bool StopShouldFail { get; set; }
        public TimeSpan StartDelay { get; set; } = TimeSpan.Zero;
        public TimeSpan StopDelay { get; set; } = TimeSpan.Zero;
        public Exception? StartException { get; set; }
        public Exception? StopException { get; set; }
        public ConcurrentBag<string> LifecycleEvents { get; } = new();

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            LifecycleEvents.Add("StartAsync_Begin");

            if (StartDelay > TimeSpan.Zero)
            {
                await Task.Delay(StartDelay, cancellationToken);
            }

            if (StartShouldFail)
            {
                LifecycleEvents.Add("StartAsync_Failing");
                if (StartException != null)
                    throw StartException;
                throw new InvalidOperationException("Simulated start failure");
            }

            LifecycleEvents.Add("StartAsync_Success");
            await _messageBroker.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            LifecycleEvents.Add("StopAsync_Begin");

            if (StopDelay > TimeSpan.Zero)
            {
                await Task.Delay(StopDelay, cancellationToken);
            }

            if (StopShouldFail)
            {
                LifecycleEvents.Add("StopAsync_Failing");
                if (StopException != null)
                    throw StopException;
                throw new InvalidOperationException("Simulated stop failure");
            }

            LifecycleEvents.Add("StopAsync_Success");
            await _messageBroker.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task HostedService_StartFailure_ShouldHandleGracefully()
    {
        // Arrange
        var messageBroker = new Mock<IMessageBroker>().Object;
        var logger = new Mock<ILogger<TestableHostedService>>().Object;

        var hostedService = new TestableHostedService(messageBroker, logger)
        {
            StartShouldFail = true
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => hostedService.StartAsync(CancellationToken.None));

        Assert.Contains("StartAsync_Begin", hostedService.LifecycleEvents);
        Assert.Contains("StartAsync_Failing", hostedService.LifecycleEvents);
        Assert.DoesNotContain("StartAsync_Success", hostedService.LifecycleEvents);
    }

    [Fact]
    public async Task HostedService_StopFailure_ShouldHandleGracefully()
    {
        // Arrange
        var messageBroker = new Mock<IMessageBroker>().Object;
        var logger = new Mock<ILogger<TestableHostedService>>().Object;

        var hostedService = new TestableHostedService(messageBroker, logger)
        {
            StopShouldFail = true
        };

        // Start successfully first
        await hostedService.StartAsync(CancellationToken.None);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => hostedService.StopAsync(CancellationToken.None));

        Assert.Contains("StopAsync_Begin", hostedService.LifecycleEvents);
        Assert.Contains("StopAsync_Failing", hostedService.LifecycleEvents);
        Assert.DoesNotContain("StopAsync_Success", hostedService.LifecycleEvents);
    }

    [Fact]
    public async Task HostedService_StartTimeout_ShouldCancelOperation()
    {
        // Arrange
        var messageBroker = new Mock<IMessageBroker>().Object;
        var logger = new Mock<ILogger<TestableHostedService>>().Object;

        var hostedService = new TestableHostedService(messageBroker, logger)
        {
            StartDelay = TimeSpan.FromSeconds(5) // Long delay
        };

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100)); // Short timeout

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => hostedService.StartAsync(cts.Token));

        Assert.Contains("StartAsync_Begin", hostedService.LifecycleEvents);
        Assert.DoesNotContain("StartAsync_Success", hostedService.LifecycleEvents);
    }

    [Fact]
    public async Task HostedService_StopTimeout_ShouldCancelOperation()
    {
        // Arrange
        var messageBroker = new Mock<IMessageBroker>().Object;
        var logger = new Mock<ILogger<TestableHostedService>>().Object;

        var hostedService = new TestableHostedService(messageBroker, logger)
        {
            StopDelay = TimeSpan.FromSeconds(5) // Long delay
        };

        // Start successfully first
        await hostedService.StartAsync(CancellationToken.None);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100)); // Short timeout

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => hostedService.StopAsync(cts.Token));

        Assert.Contains("StopAsync_Begin", hostedService.LifecycleEvents);
        Assert.DoesNotContain("StopAsync_Success", hostedService.LifecycleEvents);
    }

    [Fact]
    public async Task HostedService_CustomExceptions_ShouldBePropagated()
    {
        // Arrange
        var messageBroker = new Mock<IMessageBroker>().Object;
        var logger = new Mock<ILogger<TestableHostedService>>().Object;

        var customException = new ArgumentOutOfRangeException("Test parameter");
        var hostedService = new TestableHostedService(messageBroker, logger)
        {
            StartShouldFail = true,
            StartException = customException
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => hostedService.StartAsync(CancellationToken.None));

        Assert.Equal("Test parameter", exception.ParamName);
    }

    [Fact]
    public async Task HostedService_MultipleStartCalls_ShouldHandleCorrectly()
    {
        // Arrange
        var messageBroker = new Mock<IMessageBroker>().Object;
        var logger = new Mock<ILogger<TestableHostedService>>().Object;

        var hostedService = new TestableHostedService(messageBroker, logger);

        // Act - Multiple starts
        await hostedService.StartAsync(CancellationToken.None);
        await hostedService.StartAsync(CancellationToken.None);
        await hostedService.StartAsync(CancellationToken.None);

        // Assert
        var startEvents = hostedService.LifecycleEvents.Count(e => e == "StartAsync_Begin");
        Assert.Equal(3, startEvents); // Each start call should execute

        var successEvents = hostedService.LifecycleEvents.Count(e => e == "StartAsync_Success");
        Assert.Equal(3, successEvents);
    }

    [Fact]
    public async Task HostedService_MultipleStopCalls_ShouldHandleCorrectly()
    {
        // Arrange
        var messageBroker = new Mock<IMessageBroker>().Object;
        var logger = new Mock<ILogger<TestableHostedService>>().Object;

        var hostedService = new TestableHostedService(messageBroker, logger);

        // Start first
        await hostedService.StartAsync(CancellationToken.None);

        // Act - Multiple stops
        await hostedService.StopAsync(CancellationToken.None);
        await hostedService.StopAsync(CancellationToken.None);
        await hostedService.StopAsync(CancellationToken.None);

        // Assert
        var stopEvents = hostedService.LifecycleEvents.Count(e => e == "StopAsync_Begin");
        Assert.Equal(3, stopEvents); // Each stop call should execute

        var successEvents = hostedService.LifecycleEvents.Count(e => e == "StopAsync_Success");
        Assert.Equal(3, successEvents);
    }

    [Fact]
    public async Task HostedService_StopWithoutStart_ShouldHandleGracefully()
    {
        // Arrange
        var messageBroker = new Mock<IMessageBroker>().Object;
        var logger = new Mock<ILogger<TestableHostedService>>().Object;

        var hostedService = new TestableHostedService(messageBroker, logger);

        // Act - Stop without starting
        await hostedService.StopAsync(CancellationToken.None);

        // Assert
        Assert.Contains("StopAsync_Begin", hostedService.LifecycleEvents);
        Assert.Contains("StopAsync_Success", hostedService.LifecycleEvents);
        Assert.DoesNotContain("StartAsync_Begin", hostedService.LifecycleEvents);
    }

    [Fact]
    public async Task HostedService_ConcurrentStartStop_ShouldHandleThreadSafety()
    {
        // Arrange
        var messageBroker = new Mock<IMessageBroker>().Object;
        var logger = new Mock<ILogger<TestableHostedService>>().Object;

        var hostedService = new TestableHostedService(messageBroker, logger);

        const int operationCount = 10;
        var tasks = new List<Task>();

        // Act - Concurrent start/stop operations
        for (int i = 0; i < operationCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await hostedService.StartAsync(CancellationToken.None);
                await hostedService.StopAsync(CancellationToken.None);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var startBegins = hostedService.LifecycleEvents.Count(e => e == "StartAsync_Begin");
        var startSuccesses = hostedService.LifecycleEvents.Count(e => e == "StartAsync_Success");
        var stopBegins = hostedService.LifecycleEvents.Count(e => e == "StopAsync_Begin");
        var stopSuccesses = hostedService.LifecycleEvents.Count(e => e == "StopAsync_Success");

        Assert.Equal(operationCount, startBegins);
        Assert.Equal(operationCount, startSuccesses);
        Assert.Equal(operationCount, stopBegins);
        Assert.Equal(operationCount, stopSuccesses);
    }

    [Fact]
    public async Task HostedService_Stop_ShouldHandleCorrectly()
    {
        // Arrange
        var messageBroker = new Mock<IMessageBroker>().Object;
        var logger = new Mock<ILogger<TestableHostedService>>().Object;

        var hostedService = new TestableHostedService(messageBroker, logger);

        // Act
        await hostedService.StartAsync(CancellationToken.None);
        await hostedService.StopAsync(CancellationToken.None);

        // Assert - Should not throw
        Assert.Contains("StartAsync_Success", hostedService.LifecycleEvents);
        Assert.Contains("StopAsync_Success", hostedService.LifecycleEvents);
    }

    [Fact]
    public async Task HostedService_WithMessageBrokerFailure_ShouldHandleCorrectly()
    {
        // Arrange
        var failingMessageBroker = new Mock<IMessageBroker>();
        failingMessageBroker.Setup(mb => mb.StartAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Message broker failure"));

        var logger = new Mock<ILogger<TestableHostedService>>().Object;

        var hostedService = new TestableHostedService(failingMessageBroker.Object, logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => hostedService.StartAsync(CancellationToken.None));

        failingMessageBroker.Verify(mb => mb.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }


}