using System.Reflection;
using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.Kafka;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class KafkaMessageBrokerUtilityTests
{
    private readonly Mock<ILogger<KafkaMessageBroker>> _loggerMock;
    private readonly MessageBrokerOptions _options;

    public KafkaMessageBrokerUtilityTests()
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