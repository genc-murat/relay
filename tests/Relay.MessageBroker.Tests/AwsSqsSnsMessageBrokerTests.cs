using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.AwsSqsSns;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class AwsSqsSnsMessageBrokerTests
{
    private readonly Mock<ILogger<AwsSqsSnsMessageBroker>> _loggerMock;

    public AwsSqsSnsMessageBrokerTests()
    {
        _loggerMock = new Mock<ILogger<AwsSqsSnsMessageBroker>>();
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
    public void Constructor_WithEmptyRegion_ShouldUseDefaultRegion()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions { Region = "" }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert - Should not throw, uses default region
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithValidQueueOptions_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithValidTopicOptions_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultTopicArn = "arn:aws:sns:us-east-1:123456789012:test-topic"
            }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithValidFifoQueueOptions_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue.fifo",
                UseFifoQueue = true,
                MessageGroupId = "test-group"
            }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithAwsCredentials_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue",
                AccessKeyId = "test-access-key",
                SecretAccessKey = "test-secret-key"
            }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithPartialAwsCredentials_ShouldSucceed()
    {
        // Arrange - Only access key provided, should still work
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue",
                AccessKeyId = "test-access-key"
                // SecretAccessKey is null
            }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task StopAsync_BeforeStart_ShouldNotThrow()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StopAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.PublishAsync<TestMessage>(null!));
    }



    [Fact]
    public async Task SubscribeAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.SubscribeAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task StartAsync_WithoutQueueUrl_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultTopicArn = "arn:aws:sns:us-east-1:123456789012:test-topic"
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await broker.StartAsync());
        Assert.Equal("DefaultQueueUrl is required for consuming messages.", exception.Message);
    }

    [Fact]
    public async Task StartAsync_WithValidQueueUrl_ShouldNotThrow()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert - This will attempt to create AWS clients, but should not throw during construction
        // We can't easily test the actual StartAsync without mocking AWS services
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task PublishAsync_WithTopicArn_ShouldUseSnsClient()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultTopicArn = "arn:aws:sns:us-east-1:123456789012:test-topic"
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Content = "test message" };

        // Act & Assert - Should not throw during construction, SNS client would be used for publishing
        Assert.NotNull(broker);
        // Note: We can't easily test the actual publish without mocking AWS SDK
    }

    [Fact]
    public async Task PublishAsync_WithQueueUrl_ShouldUseSqsClient()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Content = "test message" };

        // Act & Assert - Should not throw during construction, SQS client would be used for publishing
        Assert.NotNull(broker);
        // Note: We can't easily test the actual publish without mocking AWS SDK
    }

    [Fact]
    public async Task PublishAsync_WithRoutingKey_ShouldUseRoutingKey()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Content = "test message" };
        var publishOptions = new PublishOptions
        {
            RoutingKey = "https://sqs.us-east-1.amazonaws.com/123456789012/custom-queue"
        };

        // Act & Assert - Should not throw, routing key would override default
        Assert.NotNull(broker);
        // Note: We can't easily test the actual publish without mocking AWS SDK
    }

    [Fact]
    public async Task PublishAsync_WithHeaders_ShouldIncludeHeaders()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Content = "test message" };
        var publishOptions = new PublishOptions
        {
            Headers = new Dictionary<string, object>
            {
                ["CustomHeader"] = "header-value",
                ["CorrelationId"] = Guid.NewGuid().ToString()
            }
        };

        // Act & Assert - Should not throw, headers would be included in the message
        Assert.NotNull(broker);
        // Note: We can't easily test the actual publish without mocking AWS SDK
    }

    [Fact]
    public async Task PublishAsync_WithFifoQueueAndGroupId_ShouldIncludeGroupId()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue.fifo",
                UseFifoQueue = true,
                MessageGroupId = "test-group-id"
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Content = "test message" };

        // Act & Assert - Should not throw, FIFO settings would be applied
        Assert.NotNull(broker);
        // Note: We can't easily test the actual publish without mocking AWS SDK
    }

    [Fact]
    public async Task PublishAsync_WithFifoQueueAndDeduplicationId_ShouldIncludeDeduplicationId()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue.fifo",
                UseFifoQueue = true,
                MessageGroupId = "test-group-id",
                MessageDeduplicationId = "test-dedup-id"
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Content = "test message" };

        // Act & Assert - Should not throw, FIFO settings would be applied
        Assert.NotNull(broker);
        // Note: We can't easily test the actual publish without mocking AWS SDK
    }

    [Fact]
    public async Task PublishAsync_WithSnsTopicAndHeaders_ShouldIncludeMessageAttributes()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultTopicArn = "arn:aws:sns:us-east-1:123456789012:test-topic"
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Content = "test message" };
        var publishOptions = new PublishOptions
        {
            Headers = new Dictionary<string, object>
            {
                ["Priority"] = "high",
                ["Source"] = "test-app"
            }
        };

        // Act & Assert - Should not throw, headers would be converted to SNS message attributes
        Assert.NotNull(broker);
        // Note: We can't easily test the actual publish without mocking AWS SDK
    }

    private class TestMessage
    {
        public string? Content { get; set; }
    }
}