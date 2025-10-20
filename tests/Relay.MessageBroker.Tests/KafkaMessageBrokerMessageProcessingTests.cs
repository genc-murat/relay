using System.Reflection;
using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.Kafka;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class KafkaMessageBrokerMessageProcessingTests
{
    private readonly Mock<ILogger<KafkaMessageBroker>> _loggerMock;
    private readonly MessageBrokerOptions _options;

    public KafkaMessageBrokerMessageProcessingTests()
    {
        _loggerMock = new Mock<ILogger<KafkaMessageBroker>>();
        _options = new MessageBrokerOptions
        {
            Kafka = new KafkaOptions
            {
                BootstrapServers = "localhost:9092",
                ConsumerGroupId = "test-group",
                AutoOffsetReset = "earliest",
                EnableAutoCommit = false,
                CompressionType = "gzip"
            },
            DefaultRoutingKeyPattern = "relay.{MessageType}"
        };
    }

    [Fact]
    public async Task ProcessMessageAsync_WithValidMessage_ShouldProcessMessage()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        bool processed = false;
        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            processed = true;
            return ValueTask.CompletedTask;
        });

        var messageBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new TestMessage { Id = 1, Content = "test" });
        var consumeResult = new ConsumeResult<string, byte[]>
        {
            Message = new Message<string, byte[]>
            {
                Key = "test-key",
                Value = messageBytes,
                Headers = new Headers
                {
                    { "MessageType", System.Text.Encoding.UTF8.GetBytes(typeof(TestMessage).AssemblyQualifiedName!) },
                    { "MessageId", System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()) },
                    { "Timestamp", System.Text.Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")) }
                },
                Timestamp = new Timestamp(DateTimeOffset.UtcNow)
            },
            Topic = "relay.testmessage",
            Partition = new Partition(0),
            Offset = new Offset(0)
        };

        var subscription = new SubscriptionInfo
        {
            MessageType = typeof(TestMessage),
            Handler = (msg, ctx, ct) => ValueTask.CompletedTask,
            Options = new SubscriptionOptions()
        };

        var consumerMock = new Mock<IConsumer<string, byte[]>>();
        consumerMock.Setup(c => c.Commit(It.IsAny<ConsumeResult<string, byte[]>>()));

        // Act
        var method = typeof(KafkaMessageBroker).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance, null,
            new[] { typeof(ConsumeResult<string, byte[]>), typeof(SubscriptionInfo), typeof(IConsumer<string, byte[]>) }, null);
        await (ValueTask)method!.Invoke(broker, new object[] { consumeResult, subscription, consumerMock.Object })!;

        // Assert
        Assert.True(processed);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithManualAcknowledgment_ShouldAllowManualAck()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Kafka = new KafkaOptions
            {
                BootstrapServers = "localhost:9092",
                ConsumerGroupId = "test-group",
                EnableAutoCommit = false
            }
        };
        var broker = new KafkaMessageBroker(Options.Create(options), _loggerMock.Object);

        bool acknowledged = false;
        var subscriptionOptions = new SubscriptionOptions { AutoAck = false };
        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            acknowledged = true;
            return ValueTask.CompletedTask;
        }, subscriptionOptions);

        var messageBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new TestMessage { Id = 1, Content = "test" });
        var consumeResult = new ConsumeResult<string, byte[]>
        {
            Message = new Message<string, byte[]>
            {
                Key = "test-key",
                Value = messageBytes,
                Headers = new Headers
                {
                    { "MessageType", System.Text.Encoding.UTF8.GetBytes(typeof(TestMessage).AssemblyQualifiedName!) },
                    { "MessageId", System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()) },
                    { "Timestamp", System.Text.Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")) }
                },
                Timestamp = new Timestamp(DateTimeOffset.UtcNow)
            },
            Topic = "relay.testmessage",
            Partition = new Partition(0),
            Offset = new Offset(0)
        };

        var subscription = new SubscriptionInfo
        {
            MessageType = typeof(TestMessage),
            Handler = (msg, ctx, ct) => ValueTask.CompletedTask,
            Options = subscriptionOptions
        };

        var consumerMock = new Mock<IConsumer<string, byte[]>>();
        consumerMock.Setup(c => c.Commit(It.IsAny<ConsumeResult<string, byte[]>>()));

        // Act
        var method = typeof(KafkaMessageBroker).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance, null,
            new[] { typeof(ConsumeResult<string, byte[]>), typeof(SubscriptionInfo), typeof(IConsumer<string, byte[]>) }, null);
        await (ValueTask)method!.Invoke(broker, new object[] { consumeResult, subscription, consumerMock.Object })!;

        // Assert
        Assert.True(acknowledged);
        consumerMock.Verify(c => c.Commit(It.IsAny<ConsumeResult<string, byte[]>>()), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithRejection_ShouldAllowRejection()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Kafka = new KafkaOptions
            {
                BootstrapServers = "localhost:9092",
                ConsumerGroupId = "test-group",
                EnableAutoCommit = false
            }
        };
        var broker = new KafkaMessageBroker(Options.Create(options), _loggerMock.Object);

        bool rejected = false;
        var subscriptionOptions = new SubscriptionOptions { AutoAck = false };
        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            rejected = true;
            return ValueTask.CompletedTask;
        }, subscriptionOptions);

        var messageBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new TestMessage { Id = 1, Content = "test" });
        var consumeResult = new ConsumeResult<string, byte[]>
        {
            Message = new Message<string, byte[]>
            {
                Key = "test-key",
                Value = messageBytes,
                Headers = new Headers
                {
                    { "MessageType", System.Text.Encoding.UTF8.GetBytes(typeof(TestMessage).AssemblyQualifiedName!) },
                    { "MessageId", System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()) },
                    { "Timestamp", System.Text.Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")) }
                },
                Timestamp = new Timestamp(DateTimeOffset.UtcNow)
            },
            Topic = "relay.testmessage",
            Partition = new Partition(0),
            Offset = new Offset(0)
        };

        var subscription = new SubscriptionInfo
        {
            MessageType = typeof(TestMessage),
            Handler = (msg, ctx, ct) => ValueTask.CompletedTask,
            Options = subscriptionOptions
        };

        var consumerMock = new Mock<IConsumer<string, byte[]>>();
        consumerMock.Setup(c => c.Commit(It.IsAny<ConsumeResult<string, byte[]>>()));

        // Act
        var method = typeof(KafkaMessageBroker).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance, null,
            new[] { typeof(ConsumeResult<string, byte[]>), typeof(SubscriptionInfo), typeof(IConsumer<string, byte[]>) }, null);
        await (ValueTask)method!.Invoke(broker, new object[] { consumeResult, subscription, consumerMock.Object })!;

        // Assert
        Assert.True(rejected);
        consumerMock.Verify(c => c.Commit(It.IsAny<ConsumeResult<string, byte[]>>()), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithRequeue_ShouldAllowRequeue()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Kafka = new KafkaOptions
            {
                BootstrapServers = "localhost:9092",
                ConsumerGroupId = "test-group",
                EnableAutoCommit = false
            }
        };
        var broker = new KafkaMessageBroker(Options.Create(options), _loggerMock.Object);

        bool requeued = false;
        var subscriptionOptions = new SubscriptionOptions { AutoAck = false };
        await broker.SubscribeAsync<TestMessage>(async (msg, ctx, ct) =>
        {
            requeued = true;
            await ctx.Reject(true);
        }, subscriptionOptions);

        var messageBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new TestMessage { Id = 1, Content = "test" });
        var consumeResult = new ConsumeResult<string, byte[]>
        {
            Message = new Message<string, byte[]>
            {
                Key = "test-key",
                Value = messageBytes,
                Headers = new Headers
                {
                    { "MessageType", System.Text.Encoding.UTF8.GetBytes(typeof(TestMessage).AssemblyQualifiedName!) },
                    { "MessageId", System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()) },
                    { "Timestamp", System.Text.Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")) }
                },
                Timestamp = new Timestamp(DateTimeOffset.UtcNow)
            },
            Topic = "relay.testmessage",
            Partition = new Partition(0),
            Offset = new Offset(0)
        };

        var subscription = new SubscriptionInfo
        {
            MessageType = typeof(TestMessage),
            Handler = (msg, ctx, ct) => ValueTask.CompletedTask,
            Options = subscriptionOptions
        };

        var consumerMock = new Mock<IConsumer<string, byte[]>>();
        consumerMock.Setup(c => c.Commit(It.IsAny<ConsumeResult<string, byte[]>>()));

        // Act
        var method = typeof(KafkaMessageBroker).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance, null,
            new[] { typeof(ConsumeResult<string, byte[]>), typeof(SubscriptionInfo), typeof(IConsumer<string, byte[]>) }, null);
        await (ValueTask)method!.Invoke(broker, new object[] { consumeResult, subscription, consumerMock.Object })!;

        // Assert
        Assert.True(requeued);
        consumerMock.Verify(c => c.Commit(It.IsAny<ConsumeResult<string, byte[]>>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithHandlerException_ShouldLogError()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            throw new InvalidOperationException("Test exception");
        });

        var messageBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new TestMessage { Id = 1, Content = "test" });
        var consumeResult = new ConsumeResult<string, byte[]>
        {
            Message = new Message<string, byte[]>
            {
                Key = "test-key",
                Value = messageBytes,
                Headers = new Headers
                {
                    { "MessageType", System.Text.Encoding.UTF8.GetBytes(typeof(TestMessage).AssemblyQualifiedName!) },
                    { "MessageId", System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()) },
                    { "Timestamp", System.Text.Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")) }
                },
                Timestamp = new Timestamp(DateTimeOffset.UtcNow)
            },
            Topic = "relay.testmessage",
            Partition = new Partition(0),
            Offset = new Offset(0)
        };

        var subscription = new SubscriptionInfo
        {
            MessageType = typeof(TestMessage),
            Handler = (msg, ctx, ct) => throw new InvalidOperationException("Test exception"),
            Options = new SubscriptionOptions()
        };

        var consumerMock = new Mock<IConsumer<string, byte[]>>();

        // Act
        var method = typeof(KafkaMessageBroker).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance, null,
            new[] { typeof(ConsumeResult<string, byte[]>), typeof(SubscriptionInfo), typeof(IConsumer<string, byte[]>) }, null);
        await (ValueTask)method!.Invoke(broker, new object[] { consumeResult, subscription, consumerMock.Object })!;

        // Assert - Exception should be logged, not re-thrown
        // Verify logging was called
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithInvalidJson_ShouldLogError()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);

        var consumeResult = new ConsumeResult<string, byte[]>
        {
            Message = new Message<string, byte[]>
            {
                Key = "test-key",
                Value = System.Text.Encoding.UTF8.GetBytes("invalid json"),
                Headers = new Headers
                {
                    { "MessageType", System.Text.Encoding.UTF8.GetBytes(typeof(TestMessage).AssemblyQualifiedName!) },
                    { "MessageId", System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()) },
                    { "Timestamp", System.Text.Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")) }
                },
                Timestamp = new Timestamp(DateTimeOffset.UtcNow)
            },
            Topic = "relay.testmessage",
            Partition = new Partition(0),
            Offset = new Offset(0)
        };

        var subscription = new SubscriptionInfo
        {
            MessageType = typeof(TestMessage),
            Handler = (msg, ctx, ct) => ValueTask.CompletedTask,
            Options = new SubscriptionOptions()
        };

        var consumerMock = new Mock<IConsumer<string, byte[]>>();

        // Act
        var method = typeof(KafkaMessageBroker).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance, null,
            new[] { typeof(ConsumeResult<string, byte[]>), typeof(SubscriptionInfo), typeof(IConsumer<string, byte[]>) }, null);
        await (ValueTask)method!.Invoke(broker, new object[] { consumeResult, subscription, consumerMock.Object })!;

        // Assert - Exception should be logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithNullMessage_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        var consumeResult = new ConsumeResult<string, byte[]>
        {
            Message = null!,
            Topic = "relay.testmessage",
            Partition = new Partition(0),
            Offset = new Offset(0)
        };

        var subscription = new SubscriptionInfo
        {
            MessageType = typeof(TestMessage),
            Handler = (msg, ctx, ct) => ValueTask.CompletedTask,
            Options = new SubscriptionOptions()
        };

        var consumerMock = new Mock<IConsumer<string, byte[]>>();

        // Act
        var method = typeof(KafkaMessageBroker).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance, null,
            new[] { typeof(ConsumeResult<string, byte[]>), typeof(SubscriptionInfo), typeof(IConsumer<string, byte[]>) }, null);
        await (ValueTask)method!.Invoke(broker, new object[] { consumeResult, subscription, consumerMock.Object })!;

        // Assert - Should not throw
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    private class AnotherTestMessage
    {
        public string Name { get; set; } = string.Empty;
    }
}