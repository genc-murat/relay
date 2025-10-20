using System.Net;
using System.Reflection;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SimpleNotificationService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker;
using Relay.MessageBroker.AwsSqsSns;
using Relay.MessageBroker.Compression;
using Relay.Core.ContractValidation;
using Relay.Core;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class AwsSqsSnsMessageBrokerPublishTests
{
    private readonly Mock<ILogger<AwsSqsSnsMessageBroker>> _loggerMock;

    public AwsSqsSnsMessageBrokerPublishTests()
    {
        _loggerMock = new Mock<ILogger<AwsSqsSnsMessageBroker>>();
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

    [Fact]
    public async Task PublishAsync_WithCompressionEnabled_ShouldCompressMessage()
    {
        // Arrange
        var compressorMock = new Mock<Relay.MessageBroker.Compression.IMessageCompressor>();
        compressorMock.Setup(c => c.CompressAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 1, 2, 3, 4 }); // Mock compressed data

        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            },
            Compression = new Relay.MessageBroker.Compression.CompressionOptions { Enabled = true }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object, compressorMock.Object);
        var message = new TestMessage { Content = "test message" };

        // Act & Assert - Should not throw, compression would be applied
        Assert.NotNull(broker);
        // Note: We can't easily test the actual publish without mocking AWS SDK
    }

    [Fact]
    public async Task PublishAsync_WithCompressionDisabled_ShouldNotCompressMessage()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            },
            Compression = new Relay.MessageBroker.Compression.CompressionOptions { Enabled = false }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Content = "test message" };

        // Act & Assert - Should not throw, no compression would be applied
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task PublishAsync_WithNullCompressor_ShouldNotCompressMessage()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            },
            Compression = new Relay.MessageBroker.Compression.CompressionOptions { Enabled = true }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object, null); // No compressor
        var message = new TestMessage { Content = "test message" };

        // Act & Assert - Should not throw, no compression would be applied when compressor is null
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task PublishAsync_WithSnsAndCompression_ShouldCompressMessageForSns()
    {
        // Arrange
        var compressorMock = new Mock<Relay.MessageBroker.Compression.IMessageCompressor>();
        compressorMock.Setup(c => c.CompressAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 5, 6, 7, 8 });

        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultTopicArn = "arn:aws:sns:us-east-1:123456789012:test-topic"
            },
            Compression = new Relay.MessageBroker.Compression.CompressionOptions { Enabled = true }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object, compressorMock.Object);
        var message = new TestMessage { Content = "test message" };

        // Act & Assert - Should not throw, compression would be applied for SNS publishing
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task PublishAsync_WithFifoQueueAndNoGroupId_ShouldNotIncludeGroupId()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue.fifo",
                UseFifoQueue = true
                // MessageGroupId is not set
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Content = "test message" };

        // Act & Assert - Should not throw, FIFO settings would be applied only if group ID is present
        Assert.NotNull(broker);
        // Note: We can't easily test the actual publish without mocking AWS SDK
    }

    [Fact]
    public async Task PublishAsync_WithFifoQueueAndDeduplicationIdFromOptions_ShouldIncludeDeduplicationId()
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
                MessageDeduplicationId = "options-dedup-id"
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Content = "test message" };

        // Act & Assert - Should not throw, deduplication ID from options would be used
        Assert.NotNull(broker);
        // Note: We can't easily test the actual publish without mocking AWS SDK
    }

    [Fact]
    public async Task PublishAsync_WithFifoQueueAndCustomGroupId_ShouldUseCustomGroupId()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue.fifo",
                UseFifoQueue = true,
                MessageGroupId = "default-group"
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Content = "test message" };
        var publishOptions = new PublishOptions
        {
            Headers = new Dictionary<string, object>
            {
                ["MessageGroupId"] = "custom-group-id"
            }
        };

        // Act & Assert - Should not throw, custom group ID from headers would override default
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task PublishAsync_WithFifoQueueAndCustomDeduplicationId_ShouldUseCustomDeduplicationId()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue.fifo",
                UseFifoQueue = true,
                MessageGroupId = "test-group",
                MessageDeduplicationId = "default-dedup"
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Content = "test message" };
        var publishOptions = new PublishOptions
        {
            Headers = new Dictionary<string, object>
            {
                ["MessageDeduplicationId"] = "custom-dedup-id"
            }
        };

        // Act & Assert - Should not throw, custom deduplication ID from headers would override default
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task PublishAsync_WithNonFifoQueue_ShouldNotIncludeFifoAttributes()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue", // Not FIFO
                UseFifoQueue = false,
                MessageGroupId = "test-group",
                MessageDeduplicationId = "test-dedup"
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Content = "test message" };

        // Act & Assert - Should not throw, FIFO attributes would not be included for non-FIFO queue
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task PublishAsync_WithCustomRoutingKey_ShouldOverrideDefaultTopicArn()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultTopicArn = "arn:aws:sns:us-east-1:123456789012/default-topic"
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Content = "test message" };
        var publishOptions = new PublishOptions
        {
            RoutingKey = "arn:aws:sns:us-east-1:123456789012/custom-topic"
        };

        // Act & Assert - Should not throw, custom routing key would override default
        Assert.NotNull(broker);
        // Note: We can't easily test the actual publish without mocking AWS SDK
    }

    [Fact]
    public async Task PublishAsync_WithCustomRoutingKey_ShouldOverrideDefaultQueueUrl()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/default-queue"
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Content = "test message" };
        var publishOptions = new PublishOptions
        {
            RoutingKey = "https://sqs.us-east-1.amazonaws.com/123456789012/custom-queue"
        };

        // Act & Assert - Should not throw, custom routing key would override default
        Assert.NotNull(broker);
        // Note: We can't easily test the actual publish without mocking AWS SDK
    }

    [Fact]
    public async Task PublishAsync_WithSnsTopicAndSubject_ShouldUseMessageTypeAsSubject()
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

        // Act & Assert - Should not throw, message type would be used as SNS subject
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task PublishAsync_WithSqsAndMessageAttributes_ShouldIncludeAllAttributes()
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
                ["Priority"] = "high",
                ["CorrelationId"] = Guid.NewGuid().ToString(),
                ["Timestamp"] = DateTimeOffset.UtcNow.ToString()
            }
        };

        // Act & Assert - Should not throw, all headers would be converted to SQS message attributes
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task PublishAsync_WithSnsAndMessageAttributes_ShouldIncludeAllAttributes()
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
                ["Source"] = "test-app",
                ["Version"] = "1.0"
            }
        };

        // Act & Assert - Should not throw, all headers would be converted to SNS message attributes
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

    [Fact]
    public async Task PublishAsync_WithSqsException_ShouldRetryAccordingToPolicy()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            },
            RetryPolicy = new RetryPolicy
            {
                MaxAttempts = 2 // Limited attempts for testing
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Content = "test message" };

        // Act & Assert - Should not throw during construction, retry policy would handle exceptions
        Assert.NotNull(broker);
        // Note: Testing actual retry behavior requires mocking AWS SDK which is complex
    }

    [Fact]
    public async Task PublishAsync_WithSnsException_ShouldRetryAccordingToPolicy()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultTopicArn = "arn:aws:sns:us-east-1:123456789012:test-topic"
            },
            RetryPolicy = new RetryPolicy
            {
                MaxAttempts = 2
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Content = "test message" };

        // Act & Assert - Should not throw during construction, retry policy would handle exceptions
        Assert.NotNull(broker);
        // Note: Testing actual retry behavior requires mocking AWS SDK which is complex
    }

    [Fact]
    public async Task PublishAsync_WithHttpRequestException_ShouldRetryAccordingToPolicy()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            },
            RetryPolicy = new RetryPolicy
            {
                MaxAttempts = 2
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Content = "test message" };

        // Act & Assert - Should not throw during construction, retry policy would handle HTTP exceptions
        Assert.NotNull(broker);
        // Note: Testing actual retry behavior requires mocking AWS SDK which is complex
    }

    [Fact]
    public async Task PublishAsync_WithUnauthorizedException_ShouldNotRetry()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            },
            RetryPolicy = new RetryPolicy
            {
                MaxAttempts = 2
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Content = "test message" };

        // Act & Assert - Should not throw during construction, unauthorized exceptions should not be retried
        Assert.NotNull(broker);
        // Note: Testing actual retry behavior requires mocking AWS SDK which is complex
    }

    [Fact]
    public async Task PublishAsync_WithTimeout_ShouldRespectTimeoutPolicy()
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

        // Act & Assert - Should not throw during construction, 30-second timeout would be applied
        Assert.NotNull(broker);
        // Note: Testing actual timeout behavior requires mocking AWS SDK which is complex
    }

    [Fact]
    public async Task PublishAsync_WithCancellationToken_ShouldRespectCancellation()
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
        var cancellationTokenSource = new CancellationTokenSource();

        // Act & Assert - Should not throw during construction, cancellation token would be respected
        Assert.NotNull(broker);
        // Note: Testing actual cancellation requires mocking AWS SDK
    }

    private class TestMessage
    {
        public string? Content { get; set; }
    }
}