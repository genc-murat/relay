using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.Nats;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class NatsMessageBrokerConstructorTests
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
}