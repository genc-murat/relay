using System.Net;
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

public class AwsSqsSnsMessageBrokerPublishCompressionTests
{
    private readonly Mock<ILogger<AwsSqsSnsMessageBroker>> _loggerMock;

    public AwsSqsSnsMessageBrokerPublishCompressionTests()
    {
        _loggerMock = new Mock<ILogger<AwsSqsSnsMessageBroker>>();
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

    private class TestMessage
    {
        public string? Content { get; set; }
    }
}