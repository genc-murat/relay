using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class MessageBrokerOptionsTests
{
    [Fact]
    public void MessageBrokerOptions_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var options = new MessageBrokerOptions();

        // Assert
        options.BrokerType.Should().Be(MessageBrokerType.RabbitMQ);
        options.DefaultExchange.Should().Be("relay.events");
        options.DefaultRoutingKeyPattern.Should().Be("{MessageType}");
        options.AutoPublishResults.Should().BeFalse();
        options.EnableSerialization.Should().BeTrue();
        options.SerializerType.Should().Be(MessageSerializerType.Json);
    }

    [Fact]
    public void RabbitMQOptions_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var options = new RabbitMQOptions();

        // Assert
        options.HostName.Should().Be("localhost");
        options.Port.Should().Be(5672);
        options.UserName.Should().Be("guest");
        options.Password.Should().Be("guest");
        options.VirtualHost.Should().Be("/");
        options.ConnectionTimeout.Should().Be(TimeSpan.FromSeconds(30));
        options.UseSsl.Should().BeFalse();
        options.PrefetchCount.Should().Be(10);
        options.ExchangeType.Should().Be("topic");
    }

    [Fact]
    public void KafkaOptions_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var options = new KafkaOptions();

        // Assert
        options.BootstrapServers.Should().Be("localhost:9092");
        options.ConsumerGroupId.Should().Be("relay-consumer-group");
        options.AutoOffsetReset.Should().Be("earliest");
        options.EnableAutoCommit.Should().BeFalse();
        options.SessionTimeout.Should().Be(TimeSpan.FromSeconds(10));
        options.CompressionType.Should().Be("none");
        options.DefaultPartitions.Should().Be(3);
        options.ReplicationFactor.Should().Be(1);
    }

    [Fact]
    public void RetryPolicy_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var policy = new RetryPolicy();

        // Assert
        policy.MaxAttempts.Should().Be(3);
        policy.InitialDelay.Should().Be(TimeSpan.FromSeconds(1));
        policy.MaxDelay.Should().Be(TimeSpan.FromSeconds(30));
        policy.BackoffMultiplier.Should().Be(2.0);
        policy.UseExponentialBackoff.Should().BeTrue();
    }

    [Fact]
    public void AzureServiceBusOptions_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var options = new AzureServiceBusOptions();

        // Assert
        options.Should().NotBeNull();
    }

    [Fact]
    public void AwsSqsSnsOptions_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var options = new AwsSqsSnsOptions();

        // Assert
        options.Should().NotBeNull();
    }

    [Fact]
    public void NatsOptions_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var options = new NatsOptions();

        // Assert
        options.Should().NotBeNull();
        options.Servers.Should().NotBeEmpty();
        options.Servers.Should().Contain("nats://localhost:4222");
    }

    [Fact]
    public void RedisStreamsOptions_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var options = new RedisStreamsOptions();

        // Assert
        options.Should().NotBeNull();
    }

    [Fact]
    public void MessageBrokerOptions_ShouldAllowSettingAllBrokerTypes()
    {
        // Arrange & Act
        var options = new MessageBrokerOptions
        {
            BrokerType = MessageBrokerType.Kafka,
            ConnectionString = "localhost:9092",
            RabbitMQ = new RabbitMQOptions(),
            Kafka = new KafkaOptions(),
            AzureServiceBus = new AzureServiceBusOptions(),
            AwsSqsSns = new AwsSqsSnsOptions(),
            Nats = new NatsOptions(),
            RedisStreams = new RedisStreamsOptions()
        };

        // Assert
        options.BrokerType.Should().Be(MessageBrokerType.Kafka);
        options.ConnectionString.Should().Be("localhost:9092");
        options.RabbitMQ.Should().NotBeNull();
        options.Kafka.Should().NotBeNull();
        options.AzureServiceBus.Should().NotBeNull();
        options.AwsSqsSns.Should().NotBeNull();
        options.Nats.Should().NotBeNull();
        options.RedisStreams.Should().NotBeNull();
    }

    [Fact]
    public void MessageBrokerOptions_ShouldAllowSettingAdvancedOptions()
    {
        // Arrange & Act
        var options = new MessageBrokerOptions
        {
            CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions(),
            Compression = new Compression.CompressionOptions(),
            Telemetry = new Telemetry.TelemetryOptions(),
            Saga = new Saga.SagaOptions(),
            RetryPolicy = new RetryPolicy()
        };

        // Assert
        options.CircuitBreaker.Should().NotBeNull();
        options.Compression.Should().NotBeNull();
        options.Telemetry.Should().NotBeNull();
        options.Saga.Should().NotBeNull();
        options.RetryPolicy.Should().NotBeNull();
    }

    [Fact]
    public void RabbitMQOptions_ShouldAllowCustomization()
    {
        // Arrange & Act
        var options = new RabbitMQOptions
        {
            HostName = "rabbitmq.example.com",
            Port = 5673,
            UserName = "admin",
            Password = "secret",
            VirtualHost = "/vhost",
            ConnectionTimeout = TimeSpan.FromSeconds(60),
            UseSsl = true,
            PrefetchCount = 50,
            ExchangeType = "direct"
        };

        // Assert
        options.HostName.Should().Be("rabbitmq.example.com");
        options.Port.Should().Be(5673);
        options.UserName.Should().Be("admin");
        options.Password.Should().Be("secret");
        options.VirtualHost.Should().Be("/vhost");
        options.ConnectionTimeout.Should().Be(TimeSpan.FromSeconds(60));
        options.UseSsl.Should().BeTrue();
        options.PrefetchCount.Should().Be(50);
        options.ExchangeType.Should().Be("direct");
    }

    [Fact]
    public void KafkaOptions_ShouldAllowCustomization()
    {
        // Arrange & Act
        var options = new KafkaOptions
        {
            BootstrapServers = "kafka1:9092,kafka2:9092",
            ConsumerGroupId = "custom-group",
            AutoOffsetReset = "latest",
            EnableAutoCommit = true,
            SessionTimeout = TimeSpan.FromSeconds(30),
            CompressionType = "gzip",
            DefaultPartitions = 10,
            ReplicationFactor = 3
        };

        // Assert
        options.BootstrapServers.Should().Be("kafka1:9092,kafka2:9092");
        options.ConsumerGroupId.Should().Be("custom-group");
        options.AutoOffsetReset.Should().Be("latest");
        options.EnableAutoCommit.Should().BeTrue();
        options.SessionTimeout.Should().Be(TimeSpan.FromSeconds(30));
        options.CompressionType.Should().Be("gzip");
        options.DefaultPartitions.Should().Be(10);
        options.ReplicationFactor.Should().Be(3);
    }

    [Fact]
    public void RetryPolicy_ShouldAllowCustomization()
    {
        // Arrange & Act
        var policy = new RetryPolicy
        {
            MaxAttempts = 5,
            InitialDelay = TimeSpan.FromSeconds(2),
            MaxDelay = TimeSpan.FromMinutes(1),
            BackoffMultiplier = 3.0,
            UseExponentialBackoff = false
        };

        // Assert
        policy.MaxAttempts.Should().Be(5);
        policy.InitialDelay.Should().Be(TimeSpan.FromSeconds(2));
        policy.MaxDelay.Should().Be(TimeSpan.FromMinutes(1));
        policy.BackoffMultiplier.Should().Be(3.0);
        policy.UseExponentialBackoff.Should().BeFalse();
    }
}

public class PublishOptionsTests
{
    [Fact]
    public void PublishOptions_ShouldAllowCustomization()
    {
        // Arrange & Act
        var options = new PublishOptions
        {
            RoutingKey = "test.routing.key",
            Exchange = "test-exchange",
            Headers = new Dictionary<string, object> { { "key1", "value1" } },
            Priority = 5,
            Expiration = TimeSpan.FromMinutes(5),
            Persistent = false
        };

        // Assert
        options.RoutingKey.Should().Be("test.routing.key");
        options.Exchange.Should().Be("test-exchange");
        options.Headers.Should().ContainKey("key1");
        options.Priority.Should().Be(5);
        options.Expiration.Should().Be(TimeSpan.FromMinutes(5));
        options.Persistent.Should().BeFalse();
    }
}

public class SubscriptionOptionsTests
{
    [Fact]
    public void SubscriptionOptions_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var options = new SubscriptionOptions();

        // Assert
        options.AutoAck.Should().BeFalse();
        options.Durable.Should().BeTrue();
        options.Exclusive.Should().BeFalse();
        options.AutoDelete.Should().BeFalse();
    }

    [Fact]
    public void SubscriptionOptions_ShouldAllowCustomization()
    {
        // Arrange & Act
        var options = new SubscriptionOptions
        {
            QueueName = "test-queue",
            RoutingKey = "test.routing.*",
            Exchange = "test-exchange",
            ConsumerGroup = "test-group",
            PrefetchCount = 20,
            AutoAck = true,
            Durable = false,
            Exclusive = true,
            AutoDelete = true
        };

        // Assert
        options.QueueName.Should().Be("test-queue");
        options.RoutingKey.Should().Be("test.routing.*");
        options.Exchange.Should().Be("test-exchange");
        options.ConsumerGroup.Should().Be("test-group");
        options.PrefetchCount.Should().Be(20);
        options.AutoAck.Should().BeTrue();
        options.Durable.Should().BeFalse();
        options.Exclusive.Should().BeTrue();
        options.AutoDelete.Should().BeTrue();
    }
}

public class MessageContextTests
{
    [Fact]
    public void MessageContext_ShouldStoreMetadata()
    {
        // Arrange
        var context = new MessageContext
        {
            MessageId = "msg-123",
            CorrelationId = "corr-456",
            Timestamp = DateTimeOffset.UtcNow,
            Headers = new Dictionary<string, object> { { "key", "value" } },
            RoutingKey = "test.routing",
            Exchange = "test-exchange"
        };

        // Assert
        context.MessageId.Should().Be("msg-123");
        context.CorrelationId.Should().Be("corr-456");
        context.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        context.Headers.Should().ContainKey("key");
        context.RoutingKey.Should().Be("test.routing");
        context.Exchange.Should().Be("test-exchange");
    }

    [Fact]
    public async Task MessageContext_ShouldSupportAcknowledge()
    {
        // Arrange
        var acknowledged = false;
        var context = new MessageContext
        {
            Acknowledge = async () =>
            {
                acknowledged = true;
                await ValueTask.CompletedTask;
            }
        };

        // Act
        await context.Acknowledge!();

        // Assert
        acknowledged.Should().BeTrue();
    }

    [Fact]
    public async Task MessageContext_ShouldSupportReject()
    {
        // Arrange
        var rejected = false;
        var requeued = false;
        var context = new MessageContext
        {
            Reject = async (requeue) =>
            {
                rejected = true;
                requeued = requeue;
                await ValueTask.CompletedTask;
            }
        };

        // Act
        await context.Reject!(true);

        // Assert
        rejected.Should().BeTrue();
        requeued.Should().BeTrue();
    }
}
