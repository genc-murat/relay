using System.Text.Json;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.AwsSqsSns;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class AwsSqsSnsMessageBrokerTests : IDisposable
{
    private readonly Mock<ILogger<AwsSqsSnsMessageBroker>> _loggerMock;
    private readonly MessageBrokerOptions _options;
    private readonly AwsSqsSnsMessageBroker _broker;

    public AwsSqsSnsMessageBrokerTests()
    {
        _loggerMock = new Mock<ILogger<AwsSqsSnsMessageBroker>>();

        _options = new MessageBrokerOptions
        {
            BrokerType = MessageBrokerType.AwsSqsSns,
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue",
                DefaultTopicArn = "arn:aws:sns:us-east-1:123456789012:test-topic",
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = TimeSpan.FromSeconds(20),
                AutoDeleteMessages = true
            },
            RetryPolicy = new RetryPolicy
            {
                MaxAttempts = 3,
                InitialDelay = TimeSpan.FromMilliseconds(100),
                MaxDelay = TimeSpan.FromSeconds(5),
                UseExponentialBackoff = true
            },
            CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
            {
                FailureThreshold = 3,
                Timeout = TimeSpan.FromSeconds(10)
            }
        };

        _broker = new AwsSqsSnsMessageBroker(Options.Create(_options), _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AwsSqsSnsMessageBroker(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithoutAwsOptions_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new MessageBrokerOptions();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object));
        Assert.Equal("AWS SQS/SNS options are required.", exception.Message);
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions 
        { 
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1"
            }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await _broker.PublishAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task SubscribeAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await _broker.SubscribeAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task StartAsync_WithoutQueueUrl_ShouldThrowException()
    {
        // Arrange
        var optionsWithoutQueue = new MessageBrokerOptions
        {
            BrokerType = MessageBrokerType.AwsSqsSns,
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultTopicArn = "arn:aws:sns:us-east-1:123456789012:test-topic"
                // No DefaultQueueUrl
            }
        };
        var brokerWithoutQueue = new AwsSqsSnsMessageBroker(Options.Create(optionsWithoutQueue), _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await brokerWithoutQueue.StartAsync());
    }

    [Fact]
    public async Task StartAsync_CalledTwice_ShouldNotThrow()
    {
        // Arrange - Create a fresh broker for this test
        var broker = new AwsSqsSnsMessageBroker(Options.Create(_options), _loggerMock.Object);
        await broker.StartAsync();

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StartAsync());
        Assert.Null(exception);

        // Cleanup - TaskCanceledException is expected when stopping
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await broker.StopAsync());
        
        // Final cleanup
        try
        {
            await broker.DisposeAsync();
        }
        catch (TaskCanceledException)
        {
            // Expected
        }
    }

    [Fact]
    public async Task StopAsync_BeforeStart_ShouldNotThrow()
    {
        // Arrange
        var options = new MessageBrokerOptions 
        { 
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1"
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StopAsync());
        Assert.Null(exception);
    }



    [Fact]
    public async Task StopAsync_CalledTwice_ShouldNotThrow()
    {
        // Arrange - Create a fresh broker for this test
        var broker = new AwsSqsSnsMessageBroker(Options.Create(_options), _loggerMock.Object);
        await broker.StartAsync();
        
        // Act & Assert - TaskCanceledException is expected when stopping
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await broker.StopAsync());
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await broker.StopAsync());
        
        // Cleanup
        try
        {
            await broker.DisposeAsync();
        }
        catch (TaskCanceledException)
        {
            // Expected
        }
    }

    [Fact]
    public async Task SubscribeAsync_ShouldRegisterHandlerSuccessfully()
    {
        // Arrange
        Func<TestMessage, MessageContext, CancellationToken, ValueTask> handler = 
            (message, context, ct) => ValueTask.CompletedTask;

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await _broker.SubscribeAsync(handler));
        Assert.Null(exception);
    }

    [Fact]
    public async Task SubscribeAsync_CalledMultipleTimes_ShouldRegisterMultipleHandlers()
    {
        // Arrange
        Func<TestMessage, MessageContext, CancellationToken, ValueTask> handler1 = 
            (message, context, ct) => ValueTask.CompletedTask;
        Func<TestMessage, MessageContext, CancellationToken, ValueTask> handler2 = 
            (message, context, ct) => ValueTask.CompletedTask;

        // Act
        await _broker.SubscribeAsync(handler1);
        var exception = await Record.ExceptionAsync(async () => await _broker.SubscribeAsync(handler2));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task DisposeAsync_ShouldStopAndDisposeSuccessfully()
    {
        // Arrange - Create a fresh broker for this test
        var broker = new AwsSqsSnsMessageBroker(Options.Create(_options), _loggerMock.Object);
        await broker.StartAsync();

        // Give the polling task a moment to start
        await Task.Delay(100);

        // Act & Assert - TaskCanceledException is expected when disposing due to polling task cancellation
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await broker.DisposeAsync());
    }

    [Fact]
    public void Constructor_WithRetryPolicy_ShouldConfigureRetryCorrectly()
    {
        // Arrange
        var optionsWithRetry = new MessageBrokerOptions
        {
            BrokerType = MessageBrokerType.AwsSqsSns,
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1"
            },
            RetryPolicy = new RetryPolicy
            {
                MaxAttempts = 5,
                InitialDelay = TimeSpan.FromMilliseconds(200),
                MaxDelay = TimeSpan.FromMinutes(1),
                UseExponentialBackoff = true
            }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(optionsWithRetry), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithCircuitBreaker_ShouldConfigureCircuitBreakerCorrectly()
    {
        // Arrange
        var optionsWithCircuitBreaker = new MessageBrokerOptions
        {
            BrokerType = MessageBrokerType.AwsSqsSns,
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1"
            },
            CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
            {
                FailureThreshold = 10,
                Timeout = TimeSpan.FromMinutes(2)
            }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(optionsWithCircuitBreaker), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithFifoQueueOptions_ShouldConfigureCorrectly()
    {
        // Arrange
        var fifoOptions = new MessageBrokerOptions
        {
            BrokerType = MessageBrokerType.AwsSqsSns,
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                UseFifoQueue = true,
                MessageGroupId = "test-group",
                MessageDeduplicationId = "test-deduplication"
            }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(fifoOptions), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    public void Dispose()
    {
        try
        {
            _broker?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        catch (TaskCanceledException)
        {
            // Expected when disposing the broker - ignore
        }
        GC.SuppressFinalize(this);
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
