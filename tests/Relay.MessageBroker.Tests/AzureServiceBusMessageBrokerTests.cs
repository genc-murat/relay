using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.AzureServiceBus;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class AzureServiceBusMessageBrokerTests
{
    private readonly Mock<ILogger<AzureServiceBusMessageBroker>> _loggerMock;

    public AzureServiceBusMessageBrokerTests()
    {
        _loggerMock = new Mock<ILogger<AzureServiceBusMessageBroker>>();
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AzureServiceBusMessageBroker(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithoutAzureOptions_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new MessageBrokerOptions();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new AzureServiceBusMessageBroker(Options.Create(options), _loggerMock.Object));
        Assert.Equal("Azure Service Bus options are required.", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyConnectionString_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AzureServiceBus = new AzureServiceBusOptions { ConnectionString = "" }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new AzureServiceBusMessageBroker(Options.Create(options), _loggerMock.Object));
        Assert.Equal("Azure Service Bus connection string is required.", exception.Message);
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions 
        { 
            AzureServiceBus = new AzureServiceBusOptions
            {
                ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test",
                EntityType = AzureEntityType.Topic
            }
        };

        // Act
        var broker = new AzureServiceBusMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task StopAsync_BeforeStart_ShouldNotThrow()
    {
        // Arrange
        var options = new MessageBrokerOptions 
        { 
            AzureServiceBus = new AzureServiceBusOptions
            {
                ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test"
            }
        };
        var broker = new AzureServiceBusMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StopAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new MessageBrokerOptions 
        { 
            AzureServiceBus = new AzureServiceBusOptions
            {
                ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test"
            }
        };
        var broker = new AzureServiceBusMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.PublishAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task PublishBatchAsync_WithNullMessages_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new MessageBrokerOptions 
        { 
            AzureServiceBus = new AzureServiceBusOptions
            {
                ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test"
            }
        };
        var broker = new AzureServiceBusMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.PublishBatchAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task ScheduleMessageAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new MessageBrokerOptions 
        { 
            AzureServiceBus = new AzureServiceBusOptions
            {
                ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test"
            }
        };
        var broker = new AzureServiceBusMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.ScheduleMessageAsync<TestMessage>(null!, DateTime.UtcNow.AddHours(1)));
    }

    [Fact]
    public async Task SubscribeAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new MessageBrokerOptions 
        { 
            AzureServiceBus = new AzureServiceBusOptions
            {
                ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test"
            }
        };
        var broker = new AzureServiceBusMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.SubscribeAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task PublishInTransactionAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new MessageBrokerOptions 
        { 
            AzureServiceBus = new AzureServiceBusOptions
            {
                ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test"
            }
        };
        var broker = new AzureServiceBusMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.PublishInTransactionAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task ProcessDeadLetterMessagesAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new MessageBrokerOptions 
        { 
            AzureServiceBus = new AzureServiceBusOptions
            {
                ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test"
            }
        };
        var broker = new AzureServiceBusMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.ProcessDeadLetterMessagesAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithNullOperation_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new MessageBrokerOptions 
        { 
            AzureServiceBus = new AzureServiceBusOptions
            {
                ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test"
            }
        };
        var broker = new AzureServiceBusMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.ExecuteInTransactionAsync(null!));
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
