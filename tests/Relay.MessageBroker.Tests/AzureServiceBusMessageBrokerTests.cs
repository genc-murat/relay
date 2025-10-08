using FluentAssertions;
using Relay.MessageBroker.AzureServiceBus;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class AzureServiceBusMessageBrokerTests
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new AzureServiceBusMessageBroker(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithoutAzureOptions_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new MessageBrokerOptions();

        // Act
        Action act = () => new AzureServiceBusMessageBroker(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Azure Service Bus options are required.");
    }

    [Fact]
    public void Constructor_WithEmptyConnectionString_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AzureServiceBus = new AzureServiceBusOptions { ConnectionString = "" }
        };

        // Act
        Action act = () => new AzureServiceBusMessageBroker(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Azure Service Bus connection string is required.");
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
        var broker = new AzureServiceBusMessageBroker(options);

        // Assert
        broker.Should().NotBeNull();
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
        var broker = new AzureServiceBusMessageBroker(options);

        // Act
        Func<Task> act = async () => await broker.StopAsync();

        // Assert
        await act.Should().NotThrowAsync();
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
        var broker = new AzureServiceBusMessageBroker(options);

        // Act
        Func<Task> act = async () => await broker.PublishAsync<TestMessage>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
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
        var broker = new AzureServiceBusMessageBroker(options);

        // Act
        Func<Task> act = async () => await broker.PublishBatchAsync<TestMessage>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
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
        var broker = new AzureServiceBusMessageBroker(options);

        // Act
        Func<Task> act = async () => await broker.ScheduleMessageAsync<TestMessage>(null!, DateTime.UtcNow.AddHours(1));

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
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
        var broker = new AzureServiceBusMessageBroker(options);

        // Act
        Func<Task> act = async () => await broker.SubscribeAsync<TestMessage>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
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
        var broker = new AzureServiceBusMessageBroker(options);

        // Act
        Func<Task> act = async () => await broker.PublishInTransactionAsync<TestMessage>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
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
        var broker = new AzureServiceBusMessageBroker(options);

        // Act
        Func<Task> act = async () => await broker.ProcessDeadLetterMessagesAsync<TestMessage>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
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
        var broker = new AzureServiceBusMessageBroker(options);

        // Act
        Func<Task> act = async () => await broker.ExecuteInTransactionAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
