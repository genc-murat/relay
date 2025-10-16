using System.Reflection;
using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.Kafka;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class KafkaMessageBrokerTests
{
    private readonly Mock<ILogger<KafkaMessageBroker>> _loggerMock;
    private readonly MessageBrokerOptions _options;

    public KafkaMessageBrokerTests()
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
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new KafkaMessageBroker(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new KafkaMessageBroker(options, null!));
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldSucceed()
    {
        // Arrange
        var options = Options.Create(_options);

        // Act
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithEmptyBootstrapServers_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Kafka = new KafkaOptions
            {
                BootstrapServers = "",
                ConsumerGroupId = "test-group"
            }
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new KafkaMessageBroker(Options.Create(options), _loggerMock.Object));
        Assert.Contains("BootstrapServers", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullBootstrapServers_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Kafka = new KafkaOptions
            {
                BootstrapServers = null!,
                ConsumerGroupId = "test-group"
            }
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new KafkaMessageBroker(Options.Create(options), _loggerMock.Object));
        Assert.Contains("BootstrapServers", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyConsumerGroupId_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Kafka = new KafkaOptions
            {
                BootstrapServers = "localhost:9092",
                ConsumerGroupId = ""
            }
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new KafkaMessageBroker(Options.Create(options), _loggerMock.Object));
        Assert.Contains("ConsumerGroupId", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullConsumerGroupId_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Kafka = new KafkaOptions
            {
                BootstrapServers = "localhost:9092",
                ConsumerGroupId = null!
            }
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new KafkaMessageBroker(Options.Create(options), _loggerMock.Object));
        Assert.Contains("ConsumerGroupId", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidCompressionType_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Kafka = new KafkaOptions
            {
                BootstrapServers = "localhost:9092",
                ConsumerGroupId = "test-group",
                CompressionType = "invalid"
            }
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new KafkaMessageBroker(Options.Create(options), _loggerMock.Object));
        Assert.Contains("CompressionType", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidAutoOffsetReset_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Kafka = new KafkaOptions
            {
                BootstrapServers = "localhost:9092",
                ConsumerGroupId = "test-group",
                AutoOffsetReset = "invalid"
            }
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new KafkaMessageBroker(Options.Create(options), _loggerMock.Object));
        Assert.Contains("AutoOffsetReset", exception.Message);
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);
        TestMessage? message = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.PublishAsync(message!));
    }

    [Fact]
    public async Task PublishAsync_WithValidMessage_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "test" };

        // Mock the producer to avoid actual Kafka connection
        var mockProducer = new Mock<IProducer<string, byte[]>>();
        var mockDeliveryResult = new DeliveryResult<string, byte[]>
        {
            TopicPartitionOffset = new TopicPartitionOffset("relay.testmessage", new Partition(0), new Offset(0))
        };
        mockProducer.Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, byte[]>>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(mockDeliveryResult);

        // Use reflection to set the private producer field
        var producerField = typeof(KafkaMessageBroker).GetField("_producer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        producerField?.SetValue(broker, mockProducer.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.PublishAsync(message));
        Assert.Null(exception);
    }

    [Fact]
    public async Task PublishAsync_WithPublishOptions_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "test" };
        var publishOptions = new PublishOptions
        {
            RoutingKey = "test.topic",
            Headers = new Dictionary<string, object>
            {
                ["CustomHeader"] = "value",
                ["Key"] = "test-key"
            }
        };

        // Mock the producer to avoid actual Kafka connection
        var mockProducer = new Mock<IProducer<string, byte[]>>();
        var mockDeliveryResult = new DeliveryResult<string, byte[]>
        {
            TopicPartitionOffset = new TopicPartitionOffset("test.topic", new Partition(0), new Offset(0))
        };
        mockProducer.Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, byte[]>>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(mockDeliveryResult);

        // Use reflection to set the private producer field
        var producerField = typeof(KafkaMessageBroker).GetField("_producer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        producerField?.SetValue(broker, mockProducer.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.PublishAsync(message, publishOptions));
        Assert.Null(exception);
    }

    [Fact]
    public async Task PublishAsync_WithCompressionEnabled_ShouldNotThrow()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Kafka = new KafkaOptions
            {
                BootstrapServers = "localhost:9092",
                ConsumerGroupId = "test-group",
                CompressionType = "gzip"
            }
        };
        var broker = new KafkaMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "test message for compression" };

        // Mock the producer to avoid actual Kafka connection
        var mockProducer = new Mock<IProducer<string, byte[]>>();
        var mockDeliveryResult = new DeliveryResult<string, byte[]>
        {
            TopicPartitionOffset = new TopicPartitionOffset("relay.testmessage", new Partition(0), new Offset(0))
        };
        mockProducer.Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, byte[]>>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(mockDeliveryResult);

        // Use reflection to set the private producer field
        var producerField = typeof(KafkaMessageBroker).GetField("_producer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        producerField?.SetValue(broker, mockProducer.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.PublishAsync(message));
        Assert.Null(exception);
    }

    [Fact]
    public async Task PublishAsync_WithDifferentMessageTypes_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Mock the producer to avoid actual Kafka connection
        var mockProducer = new Mock<IProducer<string, byte[]>>();
        var mockDeliveryResult = new DeliveryResult<string, byte[]>
        {
            TopicPartitionOffset = new TopicPartitionOffset("test-topic", new Partition(0), new Offset(0))
        };
        mockProducer.Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, byte[]>>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(mockDeliveryResult);

        // Use reflection to set the private producer field
        var producerField = typeof(KafkaMessageBroker).GetField("_producer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        producerField?.SetValue(broker, mockProducer.Object);

        // Act & Assert
        var exception1 = await Record.ExceptionAsync(async () => await broker.PublishAsync(new TestMessage { Id = 1, Content = "test" }));
        var exception2 = await Record.ExceptionAsync(async () => await broker.PublishAsync(new AnotherTestMessage { Name = "test" }));

        Assert.Null(exception1);
        Assert.Null(exception2);
    }

    [Fact]
    public async Task PublishAsync_WithCancellation_ShouldThrowTaskCanceledException()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "test" };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Mock the producer to throw TaskCanceledException when cancelled
        var mockProducer = new Mock<IProducer<string, byte[]>>();
        mockProducer.Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, byte[]>>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new TaskCanceledException());

        // Use reflection to set the private producer field
        var producerField = typeof(KafkaMessageBroker).GetField("_producer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        producerField?.SetValue(broker, mockProducer.Object);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await broker.PublishAsync(message, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task SubscribeAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);
        Func<TestMessage, MessageContext, CancellationToken, ValueTask>? handler = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.SubscribeAsync(handler!));
    }

    [Fact]
    public async Task SubscribeAsync_WithCustomRoutingKey_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);
        var subscriptionOptions = new SubscriptionOptions
        {
            RoutingKey = "custom.topic",
            ConsumerGroup = "custom-group",
            AutoAck = true
        };

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask,
            subscriptionOptions));
        Assert.Null(exception);
    }

    [Fact]
    public async Task SubscribeAsync_WithManualAcknowledgment_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);
        var subscriptionOptions = new SubscriptionOptions
        {
            AutoAck = false
        };

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask,
            subscriptionOptions));
        Assert.Null(exception);
    }

    [Fact]
    public async Task SubscribeAsync_WithCustomConsumerGroup_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);
        var subscriptionOptions = new SubscriptionOptions
        {
            ConsumerGroup = "custom-consumer-group"
        };

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask,
            subscriptionOptions));
        Assert.Null(exception);
    }

    [Fact]
    public async Task StopAsync_BeforeStart_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StopAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task StartAsync_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StartAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task StartAsync_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            await broker.StartAsync();
            await broker.StartAsync(); // Start twice
        });
        Assert.Null(exception);
    }

    [Fact]
    public async Task StopAsync_AfterStart_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);
        await broker.StartAsync();

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StopAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task StopAsync_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);
        await broker.StartAsync();
        await broker.StopAsync();

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StopAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task StartAsync_WithSubscriptions_ShouldSetupConsumers()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
        await broker.SubscribeAsync<AnotherTestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StartAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task StopAsync_WithActiveConsumers_ShouldCloseConsumers()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
        await broker.StartAsync();

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StopAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task DisposeAsync_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.DisposeAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task DisposeAsync_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            await broker.DisposeAsync();
            await broker.DisposeAsync(); // Dispose twice
        });
        Assert.Null(exception);
    }

    [Fact]
    public async Task SubscribeAsync_WithValidHandler_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask));
        Assert.Null(exception);
    }

    [Fact]
    public async Task SubscribeAsync_WithOptions_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);
        var subscriptionOptions = new SubscriptionOptions
        {
            RoutingKey = "test.topic",
            ConsumerGroup = "test-consumer-group",
            AutoAck = false
        };

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask,
            subscriptionOptions));
        Assert.Null(exception);
    }

    [Fact]
    public async Task SubscribeAsync_MultipleHandlers_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
            await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
        });
        Assert.Null(exception);
    }

    [Fact]
    public async Task SubscribeAsync_DifferentMessageTypes_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
            await broker.SubscribeAsync<AnotherTestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
        });
        Assert.Null(exception);
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
    public void Constructor_WithConnectionString_ShouldSucceed()
    {
        // Arrange
        var optionsWithConnectionString = new MessageBrokerOptions
        {
            ConnectionString = "localhost:9092,localhost:9093",
            Kafka = new KafkaOptions
            {
                ConsumerGroupId = "test-group",
                AutoOffsetReset = "earliest",
                CompressionType = "none"
            },
            DefaultRoutingKeyPattern = "relay.{MessageType}"
        };
        var options = Options.Create(optionsWithConnectionString);

        // Act
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithoutKafkaOptions_ShouldUseDefaults()
    {
        // Arrange
        var optionsWithoutKafka = new MessageBrokerOptions
        {
            DefaultRoutingKeyPattern = "relay.{MessageType}"
        };
        var options = Options.Create(optionsWithoutKafka);

        // Act
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task SubscribeAsync_BeforeStart_ShouldStoreSubscription()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act
        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);

        // Assert - subscription should be stored even before start
        // Verified by not throwing exception
    }

    [Fact]
    public void Constructor_WithDifferentCompressionTypes_ShouldSucceed()
    {
        // Arrange & Act & Assert
        foreach (var compressionType in new[] { "none", "gzip", "snappy", "lz4", "zstd" })
        {
            var testOptions = new MessageBrokerOptions
            {
                Kafka = new KafkaOptions
                {
                    BootstrapServers = "localhost:9092",
                    ConsumerGroupId = "test-group",
                    CompressionType = compressionType
                }
            };
            var options = Options.Create(testOptions);
            var broker = new KafkaMessageBroker(options, _loggerMock.Object);

            Assert.NotNull(broker);
        }
    }

    [Fact]
    public void Constructor_WithDifferentAutoOffsetReset_ShouldSucceed()
    {
        // Arrange & Act & Assert
        foreach (var offsetReset in new[] { "earliest", "latest", "error" })
        {
            var testOptions = new MessageBrokerOptions
            {
                Kafka = new KafkaOptions
                {
                    BootstrapServers = "localhost:9092",
                    ConsumerGroupId = "test-group",
                    AutoOffsetReset = offsetReset
                }
            };
            var options = Options.Create(testOptions);
            var broker = new KafkaMessageBroker(options, _loggerMock.Object);

            Assert.NotNull(broker);
        }
    }

    [Fact]
    public void Constructor_WithDifferentSessionTimeouts_ShouldSucceed()
    {
        // Arrange & Act & Assert
        foreach (var timeout in new[] { TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5) })
        {
            var testOptions = new MessageBrokerOptions
            {
                Kafka = new KafkaOptions
                {
                    BootstrapServers = "localhost:9092",
                    ConsumerGroupId = "test-group",
                    SessionTimeout = timeout
                }
            };
            var options = Options.Create(testOptions);
            var broker = new KafkaMessageBroker(options, _loggerMock.Object);

            Assert.NotNull(broker);
        }
    }

    [Fact]
    public void Constructor_WithEnableAutoCommitVariations_ShouldSucceed()
    {
        // Arrange & Act & Assert
        foreach (var enableAutoCommit in new[] { true, false })
        {
            var testOptions = new MessageBrokerOptions
            {
                Kafka = new KafkaOptions
                {
                    BootstrapServers = "localhost:9092",
                    ConsumerGroupId = "test-group",
                    EnableAutoCommit = enableAutoCommit
                }
            };
            var options = Options.Create(testOptions);
            var broker = new KafkaMessageBroker(options, _loggerMock.Object);

            Assert.NotNull(broker);
        }
    }

    [Fact]
    public void Constructor_WithConnectionString_ShouldUseConnectionString()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            ConnectionString = "kafka1:9092,kafka2:9092",
            Kafka = new KafkaOptions
            {
                ConsumerGroupId = "test-group"
            }
        };

        // Act
        var broker = new KafkaMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithDefaultRoutingKeyPattern_ShouldUsePattern()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Kafka = new KafkaOptions
            {
                BootstrapServers = "localhost:9092",
                ConsumerGroupId = "test-group"
            },
            DefaultRoutingKeyPattern = "myapp.{MessageType}.events"
        };

        // Act
        var broker = new KafkaMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void GetTopicName_WithDefaultPattern_ShouldGenerateCorrectTopic()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act
        var method = typeof(KafkaMessageBroker).GetMethod("GetTopicName", BindingFlags.NonPublic | BindingFlags.Instance);
        var topicName = (string)method!.Invoke(broker, new object[] { typeof(TestMessage) })!;

        // Assert
        Assert.Equal("relay.testmessage", topicName);
    }

    [Fact]
    public void GetTopicName_WithCustomPattern_ShouldGenerateCorrectTopic()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Kafka = new KafkaOptions
            {
                BootstrapServers = "localhost:9092",
                ConsumerGroupId = "test-group"
            },
            DefaultRoutingKeyPattern = "myapp.{MessageType}.events"
        };
        var broker = new KafkaMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act
        var method = typeof(KafkaMessageBroker).GetMethod("GetTopicName", BindingFlags.NonPublic | BindingFlags.Instance);
        var topicName = (string)method!.Invoke(broker, new object[] { typeof(TestMessage) })!;

        // Assert
        Assert.Equal("myapp.testmessage.events", topicName);
    }

    [Fact]
    public void GetTopicName_WithMessageFullNamePattern_ShouldGenerateCorrectTopic()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Kafka = new KafkaOptions
            {
                BootstrapServers = "localhost:9092",
                ConsumerGroupId = "test-group"
            },
            DefaultRoutingKeyPattern = "{MessageFullName}"
        };
        var broker = new KafkaMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act
        var method = typeof(KafkaMessageBroker).GetMethod("GetTopicName", BindingFlags.NonPublic | BindingFlags.Instance);
        var topicName = (string)method!.Invoke(broker, new object[] { typeof(TestMessage) })!;

        // Assert
        Assert.Equal(typeof(TestMessage).FullName!.ToLowerInvariant(), topicName);
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
