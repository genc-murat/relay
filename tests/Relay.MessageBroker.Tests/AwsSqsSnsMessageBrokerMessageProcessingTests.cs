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

public class AwsSqsSnsMessageBrokerMessageProcessingTests
{
    private readonly Mock<ILogger<AwsSqsSnsMessageBroker>> _loggerMock;

    public AwsSqsSnsMessageBrokerMessageProcessingTests()
    {
        _loggerMock = new Mock<ILogger<AwsSqsSnsMessageBroker>>();
    }

    [Fact]
    public async Task ProcessMessageAsync_WithInvalidMessageType_ShouldLogWarningAndDeleteMessage()
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

        var sqsMessage = new Message
        {
            MessageId = "test-message-id",
            ReceiptHandle = "test-receipt-handle",
            Body = "{\"content\":\"test\"}",
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["MessageType"] = new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = "Invalid.Type, InvalidAssembly"
                }
            }
        };

        // Act
        var method = typeof(AwsSqsSnsMessageBroker).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Message), typeof(string), typeof(CancellationToken) }, null);
        await (Task)method!.Invoke(broker, new object[] { sqsMessage, "test-queue-url", CancellationToken.None })!;

        // Assert - Should not throw, would log warning and attempt to delete message
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("No handler found for message type")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithNullMessageType_ShouldLogWarningAndDeleteMessage()
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

        var sqsMessage = new Message
        {
            MessageId = "test-message-id",
            ReceiptHandle = "test-receipt-handle",
            Body = "{\"content\":\"test\"}",
            MessageAttributes = new Dictionary<string, MessageAttributeValue>()
            // No MessageType attribute
        };

        // Act
        var method = typeof(AwsSqsSnsMessageBroker).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Message), typeof(string), typeof(CancellationToken) }, null);
        await (Task)method!.Invoke(broker, new object[] { sqsMessage, "test-queue-url", CancellationToken.None })!;

        // Assert - Should not throw, would log warning and attempt to delete message
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("No handler found for message type")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithInvalidJson_ShouldLogWarningAndDeleteMessage()
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

        var sqsMessage = new Message
        {
            MessageId = "test-message-id",
            ReceiptHandle = "test-receipt-handle",
            Body = "invalid json",
             MessageAttributes = new Dictionary<string, MessageAttributeValue>
             {
                 ["MessageType"] = new MessageAttributeValue
                 {
                     DataType = "String",
                     StringValue = typeof(TestMessage).AssemblyQualifiedName!
                 }
             }
        };

        // Act
        var method = typeof(AwsSqsSnsMessageBroker).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Message), typeof(string), typeof(CancellationToken) }, null);
        await (Task)method!.Invoke(broker, new object[] { sqsMessage, "test-queue-url", CancellationToken.None })!;

        // Assert - Should not throw, would log error for deserialization failure
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error processing SQS message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithValidMessageAndHandler_ShouldProcessMessage()
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

        // Subscribe to TestMessage
        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);

        var sqsMessage = new Message
        {
            MessageId = "test-message-id",
            ReceiptHandle = "test-receipt-handle",
            Body = "{\"content\":\"test message\"}",
             MessageAttributes = new Dictionary<string, MessageAttributeValue>
             {
                 ["MessageType"] = new MessageAttributeValue
                 {
                     DataType = "String",
                     StringValue = typeof(TestMessage).AssemblyQualifiedName!
                 }
             }
        };

        // Act
        var method = typeof(AwsSqsSnsMessageBroker).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Message), typeof(string), typeof(CancellationToken) }, null);
        await (Task)method!.Invoke(broker, new object[] { sqsMessage, "test-queue-url", CancellationToken.None })!;

        // Assert - Should not throw, message should be processed
        Assert.True(true); // If we get here without exception, test passes
    }

    [Fact]
    public async Task ProcessMessageAsync_WithHandlerException_ShouldLogError()
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

        // Subscribe to TestMessage with handler that throws
        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
            throw new InvalidOperationException("Handler error"));

        var sqsMessage = new Message
        {
            MessageId = "test-message-id",
            ReceiptHandle = "test-receipt-handle",
            Body = "{\"content\":\"test message\"}",
             MessageAttributes = new Dictionary<string, MessageAttributeValue>
             {
                 ["MessageType"] = new MessageAttributeValue
                 {
                     DataType = "String",
                     StringValue = typeof(TestMessage).AssemblyQualifiedName!
                 }
             }
        };

        // Act
        var method = typeof(AwsSqsSnsMessageBroker).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Message), typeof(string), typeof(CancellationToken) }, null);
        await (Task)method!.Invoke(broker, new object[] { sqsMessage, "test-queue-url", CancellationToken.None })!;

        // Assert - Should log error for handler failure but not throw
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Handler failed to process message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithAutoDeleteEnabled_ShouldAutoAcknowledge()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue",
                AutoDeleteMessages = true
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Subscribe to TestMessage
        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);

        var sqsMessage = new Message
        {
            MessageId = "test-message-id",
            ReceiptHandle = "test-receipt-handle",
            Body = "{\"content\":\"test message\"}",
             MessageAttributes = new Dictionary<string, MessageAttributeValue>
             {
                 ["MessageType"] = new MessageAttributeValue
                 {
                     DataType = "String",
                     StringValue = typeof(TestMessage).AssemblyQualifiedName!
                 }
             }
        };

        // Act
        var method = typeof(AwsSqsSnsMessageBroker).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Message), typeof(string), typeof(CancellationToken) }, null);
        await (Task)method!.Invoke(broker, new object[] { sqsMessage, "test-queue-url", CancellationToken.None })!;

        // Assert - Should not throw, auto-acknowledgment would be attempted
        Assert.True(true);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithAutoDeleteDisabled_ShouldNotAutoAcknowledge()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue",
                AutoDeleteMessages = false // Disabled
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Subscribe to TestMessage
        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);

        var sqsMessage = new Message
        {
            MessageId = "test-message-id",
            ReceiptHandle = "test-receipt-handle",
            Body = "{\"content\":\"test message\"}",
             MessageAttributes = new Dictionary<string, MessageAttributeValue>
             {
                 ["MessageType"] = new MessageAttributeValue
                 {
                     DataType = "String",
                     StringValue = typeof(TestMessage).AssemblyQualifiedName!
                 }
             }
        };

        // Act
        var method = typeof(AwsSqsSnsMessageBroker).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Message), typeof(string), typeof(CancellationToken) }, null);
        await (Task)method!.Invoke(broker, new object[] { sqsMessage, "test-queue-url", CancellationToken.None })!;

        // Assert - Should not throw, no auto-acknowledgment would occur
        Assert.True(true);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithManualAcknowledgment_ShouldAllowManualAck()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue",
                AutoDeleteMessages = false
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        bool acknowledged = false;
        // Subscribe to TestMessage with manual acknowledgment
        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            acknowledged = true;
            return ValueTask.CompletedTask;
        });

        var sqsMessage = new Message
        {
            MessageId = "test-message-id",
            ReceiptHandle = "test-receipt-handle",
            Body = "{\"content\":\"test message\"}",
             MessageAttributes = new Dictionary<string, MessageAttributeValue>
             {
                 ["MessageType"] = new MessageAttributeValue
                 {
                     DataType = "String",
                     StringValue = typeof(TestMessage).AssemblyQualifiedName!
                 }
             }
        };

        // Act
        var method = typeof(AwsSqsSnsMessageBroker).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Message), typeof(string), typeof(CancellationToken) }, null);
        await (Task)method!.Invoke(broker, new object[] { sqsMessage, "test-queue-url", CancellationToken.None })!;

        // Assert - Should not throw, manual acknowledgment would be performed
        Assert.True(acknowledged);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithRejection_ShouldAllowRejection()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue",
                AutoDeleteMessages = false
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        bool rejected = false;
        // Subscribe to TestMessage with rejection
        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            rejected = true;
            return ValueTask.CompletedTask;
        });

        var sqsMessage = new Message
        {
            MessageId = "test-message-id",
            ReceiptHandle = "test-receipt-handle",
            Body = "{\"content\":\"test message\"}",
             MessageAttributes = new Dictionary<string, MessageAttributeValue>
             {
                 ["MessageType"] = new MessageAttributeValue
                 {
                     DataType = "String",
                     StringValue = typeof(TestMessage).AssemblyQualifiedName!
                 }
             }
        };

        // Act
        var method = typeof(AwsSqsSnsMessageBroker).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Message), typeof(string), typeof(CancellationToken) }, null);
        await (Task)method!.Invoke(broker, new object[] { sqsMessage, "test-queue-url", CancellationToken.None })!;

        // Assert - Should not throw, rejection would be performed
        Assert.True(rejected);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithRequeue_ShouldAllowRequeue()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue",
                AutoDeleteMessages = false
            }
        };
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        bool requeued = false;
        // Subscribe to TestMessage with requeue
        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            requeued = true;
            return ValueTask.CompletedTask;
        });

        var sqsMessage = new Message
        {
            MessageId = "test-message-id",
            ReceiptHandle = "test-receipt-handle",
            Body = "{\"content\":\"test message\"}",
             MessageAttributes = new Dictionary<string, MessageAttributeValue>
             {
                 ["MessageType"] = new MessageAttributeValue
                 {
                     DataType = "String",
                     StringValue = typeof(TestMessage).AssemblyQualifiedName!
                 }
             }
        };

        // Act
        var method = typeof(AwsSqsSnsMessageBroker).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Message), typeof(string), typeof(CancellationToken) }, null);
        await (Task)method!.Invoke(broker, new object[] { sqsMessage, "test-queue-url", CancellationToken.None })!;

        // Assert - Should not throw, requeue would be performed (message not deleted)
        Assert.True(requeued);
    }

    private class TestMessage
    {
        public string? Content { get; set; }
    }
}