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
