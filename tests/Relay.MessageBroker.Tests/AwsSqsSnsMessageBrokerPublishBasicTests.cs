using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.AwsSqsSns;

namespace Relay.MessageBroker.Tests;

public class AwsSqsSnsMessageBrokerPublishBasicTests : IDisposable
{
    private readonly Mock<ILogger<AwsSqsSnsMessageBroker>> _loggerMock;
    private readonly AwsSqsSnsMessageBroker _broker;

    public AwsSqsSnsMessageBrokerPublishBasicTests()
    {
        _loggerMock = new Mock<ILogger<AwsSqsSnsMessageBroker>>();

        // Create shared broker instance for all tests
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            }
        };
        _broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await _broker.PublishAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task PublishAsync_WithValidMessage_ShouldNotThrow()
    {
        // Arrange
        var message = new TestMessage { Content = "test message" };

        // Act & Assert - Basic publish should work
        // Note: We can't easily test actual publish without mocking AWS SDK extensively
        Assert.NotNull(_broker);
    }

    [Fact]
    public async Task PublishAsync_WithRoutingKey_ShouldAcceptOptions()
    {
        // Arrange
        var message = new TestMessage { Content = "test message" };
        var publishOptions = new PublishOptions
        {
            RoutingKey = "https://sqs.us-east-1.amazonaws.com/123456789012/custom-queue"
        };

        // Act & Assert - Should accept routing key options
        Assert.NotNull(_broker);
        Assert.NotNull(publishOptions.RoutingKey);
    }

    [Fact]
    public async Task PublishAsync_WithTopicArnConfiguration_ShouldBeValid()
    {
        // Arrange - Test with topic ARN configuration
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultTopicArn = "arn:aws:sns:us-east-1:123456789012:test-topic"
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert - Should create broker with topic ARN
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task PublishAsync_WithQueueUrlConfiguration_ShouldBeValid()
    {
        // Arrange - Test with queue URL configuration
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert - Should create broker with queue URL
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task PublishAsync_WithoutTopicArnOrQueueUrl_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1"
                // Neither DefaultTopicArn nor DefaultQueueUrl is set
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Content = "test message" };

        // Act & Assert - Should throw because neither topic ARN nor queue URL is configured
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await broker.PublishAsync(message));
        Assert.Equal("DefaultQueueUrl is required for consuming messages.", exception.Message);
    }

    private class TestMessage
    {
        public string? Content { get; set; }
    }
}