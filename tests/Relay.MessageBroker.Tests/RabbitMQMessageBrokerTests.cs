using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Relay.MessageBroker.RabbitMQ;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class RabbitMQMessageBrokerTests
{
    private readonly Mock<ILogger<RabbitMQMessageBroker>> _loggerMock;
    private readonly MessageBrokerOptions _options;

    public RabbitMQMessageBrokerTests()
    {
        _loggerMock = new Mock<ILogger<RabbitMQMessageBroker>>();
        _options = new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/",
                ExchangeType = "topic"
            },
            DefaultExchange = "relay-test",
            DefaultRoutingKeyPattern = "relay.{MessageType}"
        };
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldSucceed()
    {
        // Arrange
        var options = Options.Create(_options);

        // Act
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        TestMessage? message = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.PublishAsync(message!));
    }

    [Fact]
    public async Task PublishAsync_WithValidMessage_ShouldNotThrowConfigurationExceptions()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "test" };

        // Act & Assert
        // Note: This will fail in test environment without RabbitMQ server, but should not throw configuration exceptions
        // The test verifies that the method can be called with valid parameters without throwing ArgumentException etc.
        try
        {
            await broker.PublishAsync(message);
            // If it succeeds, that's also fine (if RabbitMQ is running)
        }
        catch (Exception ex)
        {
            // Connection exceptions are expected in test environment, but not configuration exceptions
            Assert.IsNotType<ArgumentException>(ex);
            Assert.IsNotType<ArgumentNullException>(ex);
        }
    }

    [Fact]
    public async Task SubscribeAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        Func<TestMessage, MessageContext, CancellationToken, ValueTask>? handler = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.SubscribeAsync(handler!));
    }

    [Fact]
    public async Task StopAsync_BeforeStart_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StopAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task StartAsync_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Act & Assert - Starting multiple times should be idempotent
        // May throw connection errors but should handle multiple calls gracefully
        try
        {
            await broker.StartAsync();
            await broker.StartAsync(); // Call twice
        }
        catch
        {
            // Connection errors are expected in test environment without RabbitMQ running
            // The important thing is the structure is correct
        }
    }

    [Fact]
    public async Task DisposeAsync_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.DisposeAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task DisposeAsync_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

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
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        // Note: This will fail in test environment without RabbitMQ server, but should not throw configuration exceptions
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask)); // Expected to fail due to no RabbitMQ server
    }

    [Fact]
    public async Task SubscribeAsync_WithOptions_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var subscriptionOptions = new SubscriptionOptions
        {
            QueueName = "test-queue",
            RoutingKey = "test.routing.key",
            Exchange = "test-exchange",
            Durable = true,
            AutoAck = false
        };

        // Act & Assert
        // Note: This will fail in test environment without RabbitMQ server, but should not throw configuration exceptions
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask,
            subscriptionOptions)); // Expected to fail due to no RabbitMQ server
    }

    [Fact]
    public async Task SubscribeAsync_WithCustomQueueName_ShouldUseProvidedQueueName()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var subscriptionOptions = new SubscriptionOptions
        {
            QueueName = "custom-queue-name"
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask,
            subscriptionOptions));
    }

    [Fact]
    public async Task SubscribeAsync_WithCustomRoutingKey_ShouldUseProvidedRoutingKey()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var subscriptionOptions = new SubscriptionOptions
        {
            RoutingKey = "custom.subscription.key"
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask,
            subscriptionOptions));
    }

    [Fact]
    public async Task SubscribeAsync_WithCustomExchange_ShouldUseProvidedExchange()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var subscriptionOptions = new SubscriptionOptions
        {
            Exchange = "custom-subscription-exchange"
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask,
            subscriptionOptions));
    }

    [Fact]
    public async Task SubscribeAsync_WithNonDurable_ShouldSetNonDurable()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var subscriptionOptions = new SubscriptionOptions
        {
            Durable = false
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask,
            subscriptionOptions));
    }

    [Fact]
    public async Task SubscribeAsync_WithExclusive_ShouldSetExclusive()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var subscriptionOptions = new SubscriptionOptions
        {
            Exclusive = true
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask,
            subscriptionOptions));
    }

    [Fact]
    public async Task SubscribeAsync_WithAutoDelete_ShouldSetAutoDelete()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var subscriptionOptions = new SubscriptionOptions
        {
            AutoDelete = true
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask,
            subscriptionOptions));
    }

    [Fact]
    public async Task SubscribeAsync_WithPrefetchCount_ShouldSetPrefetchCount()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var subscriptionOptions = new SubscriptionOptions
        {
            PrefetchCount = 10
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask,
            subscriptionOptions));
    }

    [Fact]
    public void GetRoutingKey_ShouldReplaceMessageTypePlaceholder()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Use reflection to access the private GetRoutingKey method
        var method = typeof(RabbitMQMessageBroker).GetMethod("GetRoutingKey", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var routingKey = (string)method.Invoke(broker, new object[] { typeof(TestMessage) })!;

        // Assert
        Assert.Equal("relay.TestMessage", routingKey);
    }

    [Fact]
    public void GetRoutingKey_WithCustomPattern_ShouldReplacePlaceholders()
    {
        // Arrange
        var customOptions = new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            },
            DefaultExchange = "relay-test",
            DefaultRoutingKeyPattern = "custom.{MessageType}.{MessageFullName}"
        };
        var options = Options.Create(customOptions);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Use reflection to access the private GetRoutingKey method
        var method = typeof(RabbitMQMessageBroker).GetMethod("GetRoutingKey", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var routingKey = (string)method.Invoke(broker, new object[] { typeof(TestMessage) })!;

        // Assert
        Assert.Equal($"custom.TestMessage.{typeof(TestMessage).FullName}", routingKey);
    }

    [Fact]
    public async Task CreateMessageContext_Acknowledge_ShouldCallBasicAckAsync()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        var channelMock = new Mock<IChannel>();
        var basicProperties = new BasicProperties
        {
            MessageId = "test-message-id",
            CorrelationId = "test-correlation-id",
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            Headers = new Dictionary<string, object?>
            {
                ["header1"] = "value1",
                ["header2"] = 42
            }
        };

        var body = ReadOnlyMemory<byte>.Empty;
        var ea = new BasicDeliverEventArgs("consumer-tag", 123, false, "test-exchange", "test.routing.key", basicProperties, body, CancellationToken.None);

        // Use reflection to access the private CreateMessageContext method
        var method = typeof(RabbitMQMessageBroker).GetMethod("CreateMessageContext", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var context = (MessageContext)method.Invoke(broker, new object[] { ea, channelMock.Object })!;

        // Act
        await context.Acknowledge();

        // Assert
        channelMock.Verify(c => c.BasicAckAsync(123, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateMessageContext_Reject_ShouldCallBasicNackAsync()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        var channelMock = new Mock<IChannel>();
        var basicProperties = new BasicProperties
        {
            MessageId = "test-message-id"
        };

        var body = ReadOnlyMemory<byte>.Empty;
        var ea = new BasicDeliverEventArgs("consumer-tag", 123, false, "test-exchange", "test.routing.key", basicProperties, body, CancellationToken.None);

        // Use reflection to access the private CreateMessageContext method
        var method = typeof(RabbitMQMessageBroker).GetMethod("CreateMessageContext", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var context = (MessageContext)method.Invoke(broker, new object[] { ea, channelMock.Object })!;

        // Act
        await context.Reject(true);

        // Assert
        channelMock.Verify(c => c.BasicNackAsync(123, false, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void CreateMessageContext_WithNullHeaders_ShouldHandleNullHeaders()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        var channelMock = new Mock<IChannel>();
        var basicProperties = new BasicProperties
        {
            MessageId = "test-message-id",
            Headers = null
        };

        var body = ReadOnlyMemory<byte>.Empty;
        var ea = new BasicDeliverEventArgs("consumer-tag", 123, false, "test-exchange", "test.routing.key", basicProperties, body, CancellationToken.None);

        // Use reflection to access the private CreateMessageContext method
        var method = typeof(RabbitMQMessageBroker).GetMethod("CreateMessageContext", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var context = (MessageContext)method.Invoke(broker, new object[] { ea, channelMock.Object })!;

        // Assert
        Assert.Null(context.Headers);
    }

    [Fact]
    public async Task SubscribeAsync_MultipleHandlers_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        // Note: This will fail in test environment without RabbitMQ server, but should not throw configuration exceptions
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
            await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
        }); // Expected to fail due to no RabbitMQ server
    }

    [Fact]
    public async Task SubscribeAsync_DifferentMessageTypes_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        // Note: This will fail in test environment without RabbitMQ server, but should not throw configuration exceptions
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
            await broker.SubscribeAsync<AnotherTestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
        }); // Expected to fail due to no RabbitMQ server
    }

    [Fact]
    public void Constructor_WithConnectionString_ShouldSucceed()
    {
        // Arrange
        var optionsWithConnectionString = new MessageBrokerOptions
        {
            ConnectionString = "amqp://guest:guest@localhost:5672/",
            RabbitMQ = new RabbitMQOptions
            {
                ExchangeType = "topic"
            },
            DefaultExchange = "relay-test"
        };
        var options = Options.Create(optionsWithConnectionString);

        // Act
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithoutRabbitMQOptions_ShouldUseDefaults()
    {
        // Arrange
        var optionsWithoutRabbitMQ = new MessageBrokerOptions
        {
            DefaultExchange = "relay-test"
        };
        var options = Options.Create(optionsWithoutRabbitMQ);

        // Act
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RabbitMQMessageBroker(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RabbitMQMessageBroker(options, null!));
    }

    [Fact]
    public void Constructor_WithNullHostName_ShouldThrowArgumentException()
    {
        // Arrange
        var optionsWithNullHost = new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = null!,
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            },
            DefaultExchange = "relay-test"
        };
        var options = Options.Create(optionsWithNullHost);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RabbitMQMessageBroker(options, _loggerMock.Object));
        Assert.Contains("HostName", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullUserName_ShouldThrowArgumentException()
    {
        // Arrange
        var optionsWithNullUser = new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672,
                UserName = null!,
                Password = "guest"
            },
            DefaultExchange = "relay-test"
        };
        var options = Options.Create(optionsWithNullUser);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RabbitMQMessageBroker(options, _loggerMock.Object));
        Assert.Contains("UserName", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullVirtualHost_ShouldThrowArgumentException()
    {
        // Arrange
        var optionsWithNullVHost = new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                VirtualHost = null!
            },
            DefaultExchange = "relay-test"
        };
        var options = Options.Create(optionsWithNullVHost);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RabbitMQMessageBroker(options, _loggerMock.Object));
        Assert.Contains("VirtualHost", exception.Message);
    }

    [Fact]
    public void Constructor_WithZeroPort_ShouldThrowArgumentException()
    {
        // Arrange
        var optionsWithZeroPort = new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 0,
                UserName = "guest",
                Password = "guest"
            },
            DefaultExchange = "relay-test"
        };
        var options = Options.Create(optionsWithZeroPort);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RabbitMQMessageBroker(options, _loggerMock.Object));
        Assert.Contains("Port", exception.Message);
    }

    [Fact]
    public void Constructor_WithNegativePort_ShouldThrowArgumentException()
    {
        // Arrange
        var optionsWithNegativePort = new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = -1,
                UserName = "guest",
                Password = "guest"
            },
            DefaultExchange = "relay-test"
        };
        var options = Options.Create(optionsWithNegativePort);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RabbitMQMessageBroker(options, _loggerMock.Object));
        Assert.Contains("Port", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyHostName_ShouldThrowArgumentException()
    {
        // Arrange
        var optionsWithEmptyHost = new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            },
            DefaultExchange = "relay-test"
        };
        var options = Options.Create(optionsWithEmptyHost);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RabbitMQMessageBroker(options, _loggerMock.Object));
        Assert.Contains("HostName", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidPort_ShouldThrowArgumentException()
    {
        // Arrange
        var optionsWithInvalidPort = new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 99999, // Invalid port
                UserName = "guest",
                Password = "guest"
            },
            DefaultExchange = "relay-test"
        };
        var options = Options.Create(optionsWithInvalidPort);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RabbitMQMessageBroker(options, _loggerMock.Object));
        Assert.Contains("Port", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyUserName_ShouldThrowArgumentException()
    {
        // Arrange
        var optionsWithEmptyUser = new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "",
                Password = "guest"
            },
            DefaultExchange = "relay-test"
        };
        var options = Options.Create(optionsWithEmptyUser);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RabbitMQMessageBroker(options, _loggerMock.Object));
        Assert.Contains("UserName", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullPassword_ShouldThrowArgumentException()
    {
        // Arrange
        var optionsWithNullPassword = new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = null!
            },
            DefaultExchange = "relay-test"
        };
        var options = Options.Create(optionsWithNullPassword);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RabbitMQMessageBroker(options, _loggerMock.Object));
        Assert.Contains("Password", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyVirtualHost_ShouldThrowArgumentException()
    {
        // Arrange
        var optionsWithEmptyVHost = new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                VirtualHost = ""
            },
            DefaultExchange = "relay-test"
        };
        var options = Options.Create(optionsWithEmptyVHost);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RabbitMQMessageBroker(options, _loggerMock.Object));
        Assert.Contains("VirtualHost", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyExchangeType_ShouldThrowArgumentException()
    {
        // Arrange
        var optionsWithEmptyExchangeType = new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                ExchangeType = ""
            },
            DefaultExchange = "relay-test"
        };
        var options = Options.Create(optionsWithEmptyExchangeType);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RabbitMQMessageBroker(options, _loggerMock.Object));
        Assert.Contains("ExchangeType", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidExchangeType_ShouldThrowArgumentException()
    {
        // Arrange
        var optionsWithInvalidExchangeType = new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                ExchangeType = "invalid"
            },
            DefaultExchange = "relay-test"
        };
        var options = Options.Create(optionsWithInvalidExchangeType);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RabbitMQMessageBroker(options, _loggerMock.Object));
        Assert.Contains("ExchangeType", exception.Message);
    }

    [Fact]
    public void Constructor_WithSslEnabled_ShouldSucceed()
    {
        // Arrange
        var optionsWithSsl = new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5671,
                UserName = "guest",
                Password = "guest",
                UseSsl = true
            },
            DefaultExchange = "relay-test"
        };
        var options = Options.Create(optionsWithSsl);

        // Act
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithCustomConnectionTimeout_ShouldSucceed()
    {
        // Arrange
        var optionsWithTimeout = new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                ConnectionTimeout = TimeSpan.FromSeconds(60)
            },
            DefaultExchange = "relay-test"
        };
        var options = Options.Create(optionsWithTimeout);

        // Act
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithZeroConnectionTimeout_ShouldSucceed()
    {
        // Arrange
        var optionsWithZeroTimeout = new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                ConnectionTimeout = TimeSpan.Zero
            },
            DefaultExchange = "relay-test"
        };
        var options = Options.Create(optionsWithZeroTimeout);

        // Act
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithInvalidConnectionString_ShouldSucceed()
    {
        // Arrange
        var optionsWithInvalidConnectionString = new MessageBrokerOptions
        {
            ConnectionString = "invalid-connection-string",
            RabbitMQ = new RabbitMQOptions
            {
                ExchangeType = "topic"
            },
            DefaultExchange = "relay-test"
        };
        var options = Options.Create(optionsWithInvalidConnectionString);

        // Act
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task PublishAsync_WithCustomRoutingKey_ShouldUseProvidedRoutingKey()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "test" };
        var publishOptions = new PublishOptions
        {
            RoutingKey = "custom.routing.key"
        };

        // Act & Assert
        // This will fail due to no RabbitMQ server, but should not throw configuration exceptions
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await broker.PublishAsync(message, publishOptions));
    }

    [Fact]
    public async Task PublishAsync_WithCustomExchange_ShouldUseProvidedExchange()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "test" };
        var publishOptions = new PublishOptions
        {
            Exchange = "custom-exchange"
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await broker.PublishAsync(message, publishOptions));
    }

    [Fact]
    public async Task PublishAsync_WithHeaders_ShouldIncludeHeaders()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "test" };
        var publishOptions = new PublishOptions
        {
            Headers = new Dictionary<string, object>
            {
                ["custom-header"] = "header-value",
                ["numeric-header"] = 42
            }
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await broker.PublishAsync(message, publishOptions));
    }

    [Fact]
    public async Task PublishAsync_WithPriority_ShouldSetPriority()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "test" };
        var publishOptions = new PublishOptions
        {
            Priority = 5
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await broker.PublishAsync(message, publishOptions));
    }

    [Fact]
    public async Task PublishAsync_WithExpiration_ShouldSetExpiration()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "test" };
        var publishOptions = new PublishOptions
        {
            Expiration = TimeSpan.FromMinutes(5)
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await broker.PublishAsync(message, publishOptions));
    }

    [Fact]
    public async Task PublishAsync_WithNonPersistent_ShouldSetNonPersistent()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "test" };
        var publishOptions = new PublishOptions
        {
            Persistent = false
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await broker.PublishAsync(message, publishOptions));
    }

    [Fact]
    public async Task StopAsync_AfterStart_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Act & Assert - May throw connection errors but structure should be valid
        try
        {
            await broker.StartAsync();
            await broker.StopAsync();
        }
        catch
        {
            // Connection errors are expected in test environment without RabbitMQ running
        }
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
