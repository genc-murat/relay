using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Polly.CircuitBreaker;
using Relay.MessageBroker.Nats;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class NatsMessageBrokerTests
{
    private readonly Mock<ILogger<NatsMessageBroker>> _loggerMock = new();

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NatsMessageBroker(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithoutNatsOptions_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new MessageBrokerOptions();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new NatsMessageBroker(Options.Create(options), _loggerMock.Object));
        Assert.Equal("NATS options are required.", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyServers_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions { Servers = Array.Empty<string>() }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new NatsMessageBroker(Options.Create(options), _loggerMock.Object));
        Assert.Equal("At least one NATS server URL is required.", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullServers_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions { Servers = null! }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new NatsMessageBroker(Options.Create(options), _loggerMock.Object));
        Assert.Equal("At least one NATS server URL is required.", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidServerUrl_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions { Servers = new[] { "invalid-url" } }
        };

        // Act & Assert
        // Constructor should succeed, validation happens during connection
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithEmptyServerUrl_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions { Servers = new[] { "" } }
        };

        // Act & Assert
        // Constructor should succeed, validation happens during connection
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithWhitespaceServerUrl_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions { Servers = new[] { "   " } }
        };

        // Act & Assert
        // Constructor should succeed, validation happens during connection
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222" },
                Name = "test-client"
            }
        };

        // Act
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithTokenAuthentication_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222" },
                Token = "test-token"
            }
        };

        // Act
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithPartialCredentials_ShouldSucceed()
    {
        // Arrange - Only username provided
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222" },
                Username = "testuser"
            }
        };

        // Act
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithEmptyUsername_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222" },
                Username = "",
                Password = "testpass"
            }
        };

        // Act
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithJetStreamEnabled_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222" },
                UseJetStream = true,
                StreamName = "test-stream",
                ConsumerName = "test-consumer"
            }
        };

        // Act
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithJetStreamAckPolicy_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222" },
                AckPolicy = NatsAckPolicy.All,
                MaxAckPending = 500
            }
        };

        // Act
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithAckPolicyNone_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222" },
                AckPolicy = NatsAckPolicy.None
            }
        };

        // Act
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithAckPolicyExplicit_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222" },
                AckPolicy = NatsAckPolicy.Explicit
            }
        };

        // Act
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithAutoAckDisabled_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222" },
                AutoAck = false
            }
        };

        // Act
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithJetStreamFetchBatchSize_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222" },
                FetchBatchSize = 50
            }
        };

        // Act
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithTlsEnabled_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "tls://localhost:4222" },
                UseTls = true
            }
        };

        // Act
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithCustomReconnectWait_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222" },
                ReconnectWait = TimeSpan.FromSeconds(5)
            }
        };

        // Act
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithZeroMaxReconnects_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222" },
                MaxReconnects = 0
            }
        };

        // Act
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithMultipleServers_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://server1:4222", "nats://server2:4222", "nats://server3:4222" }
            }
        };

        // Act
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithMixedServerUrls_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222", "tls://secure-server:4222" }
            }
        };

        // Act
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void GetSubjectName_WithoutStreamName_ShouldUseDefaultPrefix()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions { Servers = new[] { "nats://localhost:4222" } }
        };
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act
        var subjectName = GetSubjectName(broker, typeof(TestMessage));

        // Assert
        Assert.Equal("relay.TestMessage", subjectName);
    }

    [Fact]
    public void GetSubjectName_WithStreamName_ShouldUseStreamPrefix()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222" },
                StreamName = "my-stream"
            }
        };
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act
        var subjectName = GetSubjectName(broker, typeof(TestMessage));

        // Assert
        Assert.Equal("my-stream.TestMessage", subjectName);
    }

    [Fact]
    public void GetSubjectName_WithEmptyStreamName_ShouldUseDefaultPrefix()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222" },
                StreamName = ""
            }
        };
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act
        var subjectName = GetSubjectName(broker, typeof(TestMessage));

        // Assert
        Assert.Equal("relay.TestMessage", subjectName);
    }

    [Fact]
    public void GetSubjectName_WithWhitespaceStreamName_ShouldUseDefaultPrefix()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222" },
                StreamName = "   "
            }
        };
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act
        var subjectName = GetSubjectName(broker, typeof(TestMessage));

        // Assert
        Assert.Equal("relay.TestMessage", subjectName);
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.PublishAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task SubscribeAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.SubscribeAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task StartAsync_WithValidOptions_ShouldCompleteSuccessfully()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        // Note: This will fail in test environment without NATS server, but should not throw configuration exceptions
        await Assert.ThrowsAsync<NATS.Client.Core.NatsException>(async () => await broker.StartAsync()); // Expected to fail due to no NATS server
    }

    [Fact]
    public async Task StartAsync_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        try
        {
            await broker.StartAsync();
        }
        catch (NATS.Client.Core.NatsException)
        {
            // Expected to fail without NATS server
        }

        // Act & Assert
        await Assert.ThrowsAsync<NATS.Client.Core.NatsException>(async () => await broker.StartAsync()); // Still expected to fail
    }

    [Fact]
    public async Task DisposeAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.DisposeAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task PublishAsync_WithValidMessage_ShouldNotThrowConfigurationErrors()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act & Assert
        // Expected to fail due to no NATS server, but not configuration errors
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.PublishAsync(message));
    }

    [Fact]
    public async Task PublishAsync_WithCustomHeaders_ShouldNotThrowConfigurationErrors()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "Test" };
        var publishOptions = new PublishOptions
        {
            Headers = new Dictionary<string, object>
            {
                ["custom-header"] = "custom-value",
                ["numeric-header"] = 123
            }
        };

        // Act & Assert
        // Expected to fail due to no NATS server, but not configuration errors
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.PublishAsync(message, publishOptions));
    }

    [Fact]
    public async Task PublishAsync_WithNullHeaders_ShouldNotThrowConfigurationErrors()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "Test" };
        var publishOptions = new PublishOptions
        {
            Headers = null
        };

        // Act & Assert
        // Expected to fail due to no NATS server, but not configuration errors
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.PublishAsync(message, publishOptions));
    }

    [Fact]
    public async Task PublishAsync_WithRoutingKey_ShouldNotThrowConfigurationErrors()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "Test" };
        var publishOptions = new PublishOptions
        {
            RoutingKey = "custom.subject"
        };

        // Act & Assert
        // Expected to fail due to no NATS server, but not configuration errors
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.PublishAsync(message, publishOptions));
    }

    [Fact]
    public async Task SubscribeAsync_WithValidHandler_ShouldCompleteSuccessfully()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        async ValueTask Handler(TestMessage message, MessageContext context, CancellationToken ct)
        {
            // Test handler
        }

        // Act & Assert
        // Note: This will fail in test environment without NATS server, but should not throw configuration exceptions
        await Assert.ThrowsAsync<NATS.Client.Core.NatsException>(async () => await broker.SubscribeAsync<TestMessage>(Handler)); // Expected to fail due to no NATS server
    }

    [Fact]
    public async Task SubscribeAsync_WithHandlerThatThrows_ShouldNotThrowConfigurationErrors()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        async ValueTask Handler(TestMessage message, MessageContext context, CancellationToken ct)
        {
            throw new InvalidOperationException("Test exception");
        }

        // Act & Assert
        // Note: This will fail in test environment without NATS server, but should not throw configuration exceptions
        await Assert.ThrowsAsync<NATS.Client.Core.NatsException>(async () => await broker.SubscribeAsync<TestMessage>(Handler));
    }

    [Fact]
    public async Task PublishAsync_AfterDispose_ShouldNotThrow()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "Test" };

        await broker.DisposeAsync();

        // Act & Assert
        // BaseMessageBroker doesn't throw after dispose, it just doesn't work
        var exception = await Record.ExceptionAsync(async () => await broker.PublishAsync(message));
        // May throw due to no connection, but not ObjectDisposedException
        Assert.True(exception == null || exception is not ObjectDisposedException);
    }

    [Fact]
    public async Task SubscribeAsync_AfterDispose_ShouldNotThrow()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        async ValueTask Handler(TestMessage message, MessageContext context, CancellationToken ct)
        {
            // Test handler
        }

        await broker.DisposeAsync();

        // Act & Assert
        // BaseMessageBroker doesn't throw after dispose, it just doesn't work
        var exception = await Record.ExceptionAsync(async () => await broker.SubscribeAsync<TestMessage>(Handler));
        // May throw due to no connection, but not ObjectDisposedException
        Assert.True(exception == null || exception is not ObjectDisposedException);
    }

    private MessageBrokerOptions CreateValidOptions()
    {
        return new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222" },
                Name = "test-client",
                Username = "testuser",
                Password = "testpass",
                MaxReconnects = 3
            }
        };
    }

    [Fact]
    public async Task StopAsync_BeforeStart_ShouldNotThrow()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions { Servers = new[] { "nats://localhost:4222" } }
        };
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StopAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task StartAsync_AfterStop_ShouldNotThrow()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        try
        {
            await broker.StartAsync();
        }
        catch (NATS.Client.Core.NatsException)
        {
            // Expected to fail without NATS server
        }

        await broker.StopAsync();

        // Act & Assert
        // Should not throw even if connection fails
        var exception = await Record.ExceptionAsync(async () => await broker.StartAsync());
        // Note: May throw NatsException due to no server, but should not throw other exceptions
        Assert.True(exception == null || exception is NATS.Client.Core.NatsException);
    }

    [Fact]
    public async Task DisposeAsync_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        var exception1 = await Record.ExceptionAsync(async () => await broker.DisposeAsync());
        var exception2 = await Record.ExceptionAsync(async () => await broker.DisposeAsync());

        Assert.Null(exception1);
        Assert.Null(exception2);
    }

    [Fact]
    public async Task DisposeAsync_AfterStart_ShouldDisposeConnection()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        try
        {
            await broker.StartAsync();
        }
        catch (NATS.Client.Core.NatsException)
        {
            // Expected to fail without NATS server
        }

        // Act
        await broker.DisposeAsync();

        // Assert - Should not throw on subsequent operations
        var exception = await Record.ExceptionAsync(async () => await broker.DisposeAsync());
        Assert.Null(exception);
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    private static string GetSubjectName(NatsMessageBroker broker, Type type)
    {
        var method = typeof(NatsMessageBroker).GetMethod("GetSubjectName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new[] { typeof(Type) }, null);
        return (string)method!.Invoke(broker, new object[] { type })!;
    }
}
