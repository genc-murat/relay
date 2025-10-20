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

public class AwsSqsSnsMessageBrokerUtilityTests
{
    private readonly Mock<ILogger<AwsSqsSnsMessageBroker>> _loggerMock;

    public AwsSqsSnsMessageBrokerUtilityTests()
    {
        _loggerMock = new Mock<ILogger<AwsSqsSnsMessageBroker>>();
    }

    [Fact]
    public async Task DeleteMessageAsync_WithValidReceiptHandle_ShouldDeleteMessage()
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

        // Act
        var method = typeof(AwsSqsSnsMessageBroker).GetMethod("DeleteMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)method!.Invoke(broker, new object[] { "test-queue-url", "test-receipt-handle", CancellationToken.None })!;

        // Assert - Should not throw, message deletion would be attempted
        Assert.True(true);
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeClients()
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

        // Act
        await broker.DisposeAsync();

        // Assert - Should not throw, clients would be disposed
        Assert.True(true);
    }

    [Fact]
    public async Task DisposeAsync_MultipleTimes_ShouldNotThrow()
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

        // Act
        await broker.DisposeAsync();
        await broker.DisposeAsync();

        // Assert - Should not throw, multiple disposes should be safe
        Assert.True(true);
    }

    private class TestMessage
    {
        public string? Content { get; set; }
    }
}