using System.Reflection;
using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.Kafka;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class KafkaMessageBrokerPublishTests
{
    private readonly Mock<ILogger<KafkaMessageBroker>> _loggerMock;
    private readonly MessageBrokerOptions _options;

    public KafkaMessageBrokerPublishTests()
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