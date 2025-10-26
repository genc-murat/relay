using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.AwsSqsSns;
using Relay.MessageBroker.AzureServiceBus;
using Relay.MessageBroker.Kafka;
using Relay.MessageBroker.RabbitMQ;
using Relay.MessageBroker.RedisStreams;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class MessageBrokerConfigurationTests
{
    [Fact]
    public void AwsSqsSns_Constructor_WithDifferentRegions_ShouldHandleCorrectly()
    {
        // Arrange
        var logger = new Mock<ILogger<AwsSqsSnsMessageBroker>>().Object;

        var regions = new[] { "us-east-1", "eu-west-1", "ap-southeast-1", "" };

        foreach (var region in regions)
        {
            var options = new MessageBrokerOptions
            {
                AwsSqsSns = new AwsSqsSnsOptions
                {
                    Region = region,
                    AccessKeyId = "test-key",
                    SecretAccessKey = "test-secret"
                }
            };

            // Act & Assert
            if (string.IsNullOrEmpty(region))
            {
                // Should use default region
                var broker = new AwsSqsSnsMessageBroker(Options.Create(options), logger);
                Assert.NotNull(broker);
            }
            else
            {
                var broker = new AwsSqsSnsMessageBroker(Options.Create(options), logger);
                Assert.NotNull(broker);
            }
        }
    }

    [Fact]
    public void AwsSqsSns_Constructor_WithFifoQueueConfiguration_ShouldHandleCorrectly()
    {
        // Arrange
        var logger = new Mock<ILogger<AwsSqsSnsMessageBroker>>().Object;

        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                AccessKeyId = "test-key",
                SecretAccessKey = "test-secret",
                UseFifo = true,
                MessageGroupId = "test-group",
                MessageDeduplicationId = "test-dedup"
            }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), logger);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void AzureServiceBus_Constructor_WithConnectionString_ShouldHandleCorrectly()
    {
        // Arrange
        var logger = new Mock<ILogger<AzureServiceBusMessageBroker>>().Object;

        var options = new MessageBrokerOptions
        {
            AzureServiceBus = new AzureServiceBusOptions
            {
                ConnectionString = "Endpoint=test;SharedAccessKeyName=test;SharedAccessKey=test"
            }
        };

        // Act
        var broker = new AzureServiceBusMessageBroker(Options.Create(options), logger);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Kafka_Constructor_WithBasicConfiguration_ShouldHandleCorrectly()
    {
        // Arrange
        var logger = new Mock<ILogger<KafkaMessageBroker>>().Object;

        var options = new MessageBrokerOptions
        {
            Kafka = new KafkaOptions
            {
                BootstrapServers = "localhost:9092"
            }
        };

        // Act
        var broker = new KafkaMessageBroker(Options.Create(options), logger);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void RabbitMQ_Constructor_WithBasicConfiguration_ShouldHandleCorrectly()
    {
        // Arrange
        var logger = new Mock<ILogger<RabbitMQMessageBroker>>().Object;

        var options = new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672
            }
        };

        // Act
        var broker = new RabbitMQMessageBroker(Options.Create(options), logger);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void RedisStreams_Constructor_WithBasicConfiguration_ShouldHandleCorrectly()
    {
        // Arrange
        var logger = new Mock<ILogger<RedisStreamsMessageBroker>>().Object;

        var options = new MessageBrokerOptions
        {
            RedisStreams = new RedisStreamsOptions
            {
                ConnectionString = "localhost:6379",
                DefaultStreamName = "test-stream"
            }
        };

        // Act
        var broker = new RedisStreamsMessageBroker(Options.Create(options), logger);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void MessageBrokerOptions_WithCompressionAndBrokerSpecific_ShouldWorkTogether()
    {
        // Arrange
        var logger = new Mock<ILogger<AwsSqsSnsMessageBroker>>().Object;

        var options = new MessageBrokerOptions
        {
            Compression = new Relay.MessageBroker.Compression.CompressionOptions
            {
                Enabled = true,
                Algorithm = Relay.Core.Caching.Compression.CompressionAlgorithm.GZip
            },
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                AccessKeyId = "test-key",
                SecretAccessKey = "test-secret"
            }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), logger);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void MessageBrokerOptions_WithCircuitBreakerAndBrokerSpecific_ShouldWorkTogether()
    {
        // Arrange
        var logger = new Mock<ILogger<AwsSqsSnsMessageBroker>>().Object;

        var options = new MessageBrokerOptions
        {
            CircuitBreaker = new Relay.MessageBroker.CircuitBreaker.CircuitBreakerOptions
            {
                Enabled = true,
                FailureThreshold = 5,
                Timeout = TimeSpan.FromSeconds(30)
            },
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                AccessKeyId = "test-key",
                SecretAccessKey = "test-secret"
            }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), logger);

        // Assert
        Assert.NotNull(broker);
    }
}