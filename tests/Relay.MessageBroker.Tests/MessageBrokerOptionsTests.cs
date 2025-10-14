using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.MessageBroker;
using Relay.Core.Telemetry;
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
        Assert.Equal(MessageBrokerType.RabbitMQ, options.BrokerType);
        Assert.Equal("relay.events", options.DefaultExchange);
        Assert.Equal("{MessageType}", options.DefaultRoutingKeyPattern);
        Assert.False(options.AutoPublishResults);
        Assert.True(options.EnableSerialization);
        Assert.Equal(MessageSerializerType.Json, options.SerializerType);
    }

    [Fact]
    public void RabbitMQOptions_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var options = new RabbitMQOptions();

        // Assert
        Assert.Equal("localhost", options.HostName);
        Assert.Equal(5672, options.Port);
        Assert.Equal("guest", options.UserName);
        Assert.Equal("guest", options.Password);
        Assert.Equal("/", options.VirtualHost);
        Assert.Equal(TimeSpan.FromSeconds(30), options.ConnectionTimeout);
        Assert.False(options.UseSsl);
        Assert.Equal(10, options.PrefetchCount);
        Assert.Equal("topic", options.ExchangeType);
    }

    [Fact]
    public void KafkaOptions_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var options = new KafkaOptions();

        // Assert
        Assert.Equal("localhost:9092", options.BootstrapServers);
        Assert.Equal("relay-consumer-group", options.ConsumerGroupId);
        Assert.Equal("earliest", options.AutoOffsetReset);
        Assert.False(options.EnableAutoCommit);
        Assert.Equal(TimeSpan.FromSeconds(10), options.SessionTimeout);
        Assert.Equal("none", options.CompressionType);
        Assert.Equal(3, options.DefaultPartitions);
        Assert.Equal(1, options.ReplicationFactor);
    }

    [Fact]
    public void RetryPolicy_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var policy = new RetryPolicy();

        // Assert
        Assert.Equal(3, policy.MaxAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), policy.InitialDelay);
        Assert.Equal(TimeSpan.FromSeconds(30), policy.MaxDelay);
        Assert.Equal(2.0, policy.BackoffMultiplier);
        Assert.True(policy.UseExponentialBackoff);
    }

    [Fact]
    public void AzureServiceBusOptions_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var options = new AzureServiceBusOptions();

        // Assert
        Assert.NotNull(options);
    }

    [Fact]
    public void AwsSqsSnsOptions_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var options = new AwsSqsSnsOptions();

        // Assert
        Assert.NotNull(options);
    }

    [Fact]
    public void NatsOptions_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var options = new NatsOptions();

        // Assert
        Assert.NotNull(options);
        Assert.NotEmpty(options.Servers);
        Assert.Contains("nats://localhost:4222", options.Servers);
    }

    [Fact]
    public void RedisStreamsOptions_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var options = new RedisStreamsOptions();

        // Assert
        Assert.NotNull(options);
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
        Assert.Equal(MessageBrokerType.Kafka, options.BrokerType);
        Assert.Equal("localhost:9092", options.ConnectionString);
        Assert.NotNull(options.RabbitMQ);
        Assert.NotNull(options.Kafka);
        Assert.NotNull(options.AzureServiceBus);
        Assert.NotNull(options.AwsSqsSns);
        Assert.NotNull(options.Nats);
        Assert.NotNull(options.RedisStreams);
    }

    [Fact]
    public void MessageBrokerOptions_ShouldAllowSettingAdvancedOptions()
    {
        // Arrange & Act
        var options = new MessageBrokerOptions
        {
            CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions(),
            Compression = new Compression.CompressionOptions(),
            Telemetry = new RelayTelemetryOptions(),
            Saga = new Saga.SagaOptions(),
            RetryPolicy = new RetryPolicy()
        };

        // Assert
        Assert.NotNull(options.CircuitBreaker);
        Assert.NotNull(options.Compression);
        Assert.NotNull(options.Telemetry);
        Assert.NotNull(options.Saga);
        Assert.NotNull(options.RetryPolicy);
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
        Assert.Equal("rabbitmq.example.com", options.HostName);
        Assert.Equal(5673, options.Port);
        Assert.Equal("admin", options.UserName);
        Assert.Equal("secret", options.Password);
        Assert.Equal("/vhost", options.VirtualHost);
        Assert.Equal(TimeSpan.FromSeconds(60), options.ConnectionTimeout);
        Assert.True(options.UseSsl);
        Assert.Equal(50, options.PrefetchCount);
        Assert.Equal("direct", options.ExchangeType);
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
        Assert.Equal("kafka1:9092,kafka2:9092", options.BootstrapServers);
        Assert.Equal("custom-group", options.ConsumerGroupId);
        Assert.Equal("latest", options.AutoOffsetReset);
        Assert.True(options.EnableAutoCommit);
        Assert.Equal(TimeSpan.FromSeconds(30), options.SessionTimeout);
        Assert.Equal("gzip", options.CompressionType);
        Assert.Equal(10, options.DefaultPartitions);
        Assert.Equal(3, options.ReplicationFactor);
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
        Assert.Equal(5, policy.MaxAttempts);
        Assert.Equal(TimeSpan.FromSeconds(2), policy.InitialDelay);
        Assert.Equal(TimeSpan.FromMinutes(1), policy.MaxDelay);
        Assert.Equal(3.0, policy.BackoffMultiplier);
        Assert.False(policy.UseExponentialBackoff);
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
        Assert.Equal("test.routing.key", options.RoutingKey);
        Assert.Equal("test-exchange", options.Exchange);
        Assert.True(options.Headers.ContainsKey("key1"));
        Assert.Equal((byte)5, options.Priority);
        Assert.Equal(TimeSpan.FromMinutes(5), options.Expiration);
        Assert.False(options.Persistent);
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
        Assert.False(options.AutoAck);
        Assert.True(options.Durable);
        Assert.False(options.Exclusive);
        Assert.False(options.AutoDelete);
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
        Assert.Equal("test-queue", options.QueueName);
        Assert.Equal("test.routing.*", options.RoutingKey);
        Assert.Equal("test-exchange", options.Exchange);
        Assert.Equal("test-group", options.ConsumerGroup);
        Assert.Equal((ushort)20, options.PrefetchCount);
        Assert.True(options.AutoAck);
        Assert.False(options.Durable);
        Assert.True(options.Exclusive);
        Assert.True(options.AutoDelete);
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
        Assert.Equal("msg-123", context.MessageId);
        Assert.Equal("corr-456", context.CorrelationId);
        Assert.InRange(context.Timestamp, DateTimeOffset.UtcNow.AddSeconds(-1), DateTimeOffset.UtcNow.AddSeconds(1));
        Assert.True(context.Headers.ContainsKey("key"));
        Assert.Equal("test.routing", context.RoutingKey);
        Assert.Equal("test-exchange", context.Exchange);
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
        Assert.True(acknowledged);
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
        Assert.True(rejected);
        Assert.True(requeued);
    }
}
