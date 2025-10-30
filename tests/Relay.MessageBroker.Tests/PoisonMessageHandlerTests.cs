using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using PoisonMessageClass = Relay.MessageBroker.PoisonMessage.PoisonMessage;
using PoisonMessageHandler = Relay.MessageBroker.PoisonMessage.PoisonMessageHandler;
using InMemoryPoisonMessageStore = Relay.MessageBroker.PoisonMessage.InMemoryPoisonMessageStore;
using PoisonMessageOptions = Relay.MessageBroker.PoisonMessage.PoisonMessageOptions;

namespace Relay.MessageBroker.Tests;

public class PoisonMessageHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldStorePoisonMessage()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var options = Options.Create(new PoisonMessageOptions
        {
            Enabled = true,
            FailureThreshold = 5
        });
        var handler = new PoisonMessageHandler(store, options, NullLogger<PoisonMessageHandler>.Instance);

        var poisonMessage = new PoisonMessageClass
        {
            MessageType = "TestMessage",
            Payload = new byte[] { 1, 2, 3 },
            FailureCount = 5,
            Errors = new List<string> { "Error 1", "Error 2" }
        };

        // Act
        await handler.HandleAsync(poisonMessage);

        // Assert
        var messages = await handler.GetPoisonMessagesAsync();
        Assert.Single(messages);
        Assert.Equal("TestMessage", messages.First().MessageType);
    }

    [Fact]
    public async Task HandleAsync_ShouldGenerateIdIfEmpty()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var options = Options.Create(new PoisonMessageOptions());
        var handler = new PoisonMessageHandler(store, options, NullLogger<PoisonMessageHandler>.Instance);

        var poisonMessage = new PoisonMessageClass
        {
            Id = Guid.Empty,
            MessageType = "TestMessage",
            Payload = new byte[] { 1, 2, 3 }
        };

        // Act
        await handler.HandleAsync(poisonMessage);

        // Assert
        var messages = await handler.GetPoisonMessagesAsync();
        Assert.NotEqual(Guid.Empty, messages.First().Id);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowWhenMessageIsNull()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var options = Options.Create(new PoisonMessageOptions());
        var handler = new PoisonMessageHandler(store, options, NullLogger<PoisonMessageHandler>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await handler.HandleAsync(null!);
        });
    }

    [Fact]
    public async Task GetPoisonMessagesAsync_ShouldReturnAllMessages()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var options = Options.Create(new PoisonMessageOptions());
        var handler = new PoisonMessageHandler(store, options, NullLogger<PoisonMessageHandler>.Instance);

        await handler.HandleAsync(new PoisonMessageClass { MessageType = "Test1", Payload = new byte[] { 1 } });
        await handler.HandleAsync(new PoisonMessageClass { MessageType = "Test2", Payload = new byte[] { 2 } });
        await handler.HandleAsync(new PoisonMessageClass { MessageType = "Test3", Payload = new byte[] { 3 } });

        // Act
        var messages = await handler.GetPoisonMessagesAsync();

        // Assert
        Assert.Equal(3, messages.Count());
    }

    [Fact]
    public async Task ReprocessAsync_ShouldRemoveMessageFromStore()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var options = Options.Create(new PoisonMessageOptions());
        var handler = new PoisonMessageHandler(store, options, NullLogger<PoisonMessageHandler>.Instance);

        var poisonMessage = new PoisonMessageClass
        {
            MessageType = "TestMessage",
            Payload = new byte[] { 1, 2, 3 },
            OriginalMessageId = "msg-123"
        };
        await handler.HandleAsync(poisonMessage);

        var messages = await handler.GetPoisonMessagesAsync();
        var messageId = messages.First().Id;

        // Act
        await handler.ReprocessAsync(messageId);

        // Assert
        var remainingMessages = await handler.GetPoisonMessagesAsync();
        Assert.Empty(remainingMessages);
    }

    [Fact]
    public async Task ReprocessAsync_ShouldHandleNonExistentMessage()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var options = Options.Create(new PoisonMessageOptions());
        var handler = new PoisonMessageHandler(store, options, NullLogger<PoisonMessageHandler>.Instance);

        // Act & Assert (should not throw)
        await handler.ReprocessAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task TrackFailureAsync_ShouldIncrementFailureCount()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var options = Options.Create(new PoisonMessageOptions
        {
            FailureThreshold = 5
        });
        var handler = new PoisonMessageHandler(store, options, NullLogger<PoisonMessageHandler>.Instance);

        var context = new MessageContext
        {
            MessageId = "msg-123",
            CorrelationId = "corr-456"
        };

        // Act
        var isPoisoned1 = await handler.TrackFailureAsync("msg-123", "TestMessage", new byte[] { 1 }, "Error 1", context);
        var isPoisoned2 = await handler.TrackFailureAsync("msg-123", "TestMessage", new byte[] { 1 }, "Error 2", context);
        var isPoisoned3 = await handler.TrackFailureAsync("msg-123", "TestMessage", new byte[] { 1 }, "Error 3", context);

        // Assert
        Assert.False(isPoisoned1);
        Assert.False(isPoisoned2);
        Assert.False(isPoisoned3);
    }

    [Fact]
    public async Task TrackFailureAsync_ShouldMoveToPoisonQueueWhenThresholdReached()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var options = Options.Create(new PoisonMessageOptions
        {
            FailureThreshold = 3
        });
        var handler = new PoisonMessageHandler(store, options, NullLogger<PoisonMessageHandler>.Instance);

        var context = new MessageContext
        {
            MessageId = "msg-123",
            CorrelationId = "corr-456"
        };

        // Act
        await handler.TrackFailureAsync("msg-123", "TestMessage", new byte[] { 1 }, "Error 1", context);
        await handler.TrackFailureAsync("msg-123", "TestMessage", new byte[] { 1 }, "Error 2", context);
        var isPoisoned = await handler.TrackFailureAsync("msg-123", "TestMessage", new byte[] { 1 }, "Error 3", context);

        // Assert
        Assert.True(isPoisoned);
        var messages = await handler.GetPoisonMessagesAsync();
        Assert.Single(messages);
        Assert.Equal("TestMessage", messages.First().MessageType);
        Assert.Equal(3, messages.First().FailureCount);
        Assert.Equal(3, messages.First().Errors.Count);
    }

    [Fact]
    public async Task TrackFailureAsync_ShouldStoreMessageContext()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var options = Options.Create(new PoisonMessageOptions
        {
            FailureThreshold = 1
        });
        var handler = new PoisonMessageHandler(store, options, NullLogger<PoisonMessageHandler>.Instance);

        var context = new MessageContext
        {
            MessageId = "msg-123",
            CorrelationId = "corr-456",
            RoutingKey = "test.route",
            Exchange = "test-exchange",
            Headers = new Dictionary<string, object> { { "key1", "value1" } }
        };

        // Act
        await handler.TrackFailureAsync("msg-123", "TestMessage", new byte[] { 1 }, "Error", context);

        // Assert
        var messages = await handler.GetPoisonMessagesAsync();
        var message = messages.First();
        Assert.Equal("msg-123", message.OriginalMessageId);
        Assert.Equal("corr-456", message.CorrelationId);
        Assert.Equal("test.route", message.RoutingKey);
        Assert.Equal("test-exchange", message.Exchange);
        Assert.NotNull(message.Headers);
        Assert.Equal("value1", message.Headers["key1"]);
    }

    [Fact]
    public async Task TrackFailureAsync_ShouldThrowWhenMessageIdIsNull()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var options = Options.Create(new PoisonMessageOptions());
        var handler = new PoisonMessageHandler(store, options, NullLogger<PoisonMessageHandler>.Instance);

        var context = new MessageContext();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await handler.TrackFailureAsync(null!, "TestMessage", new byte[] { 1 }, "Error", context);
        });
    }

    [Fact]
    public async Task TrackFailureAsync_ShouldThrowWhenMessageTypeIsNull()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var options = Options.Create(new PoisonMessageOptions());
        var handler = new PoisonMessageHandler(store, options, NullLogger<PoisonMessageHandler>.Instance);

        var context = new MessageContext();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await handler.TrackFailureAsync("msg-123", null!, new byte[] { 1 }, "Error", context);
        });
    }

    [Fact]
    public async Task TrackFailureAsync_ShouldThrowWhenPayloadIsNull()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var options = Options.Create(new PoisonMessageOptions());
        var handler = new PoisonMessageHandler(store, options, NullLogger<PoisonMessageHandler>.Instance);

        var context = new MessageContext();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await handler.TrackFailureAsync("msg-123", "TestMessage", null!, "Error", context);
        });
    }

    [Fact]
    public async Task TrackFailureAsync_ShouldThrowWhenErrorIsNull()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var options = Options.Create(new PoisonMessageOptions());
        var handler = new PoisonMessageHandler(store, options, NullLogger<PoisonMessageHandler>.Instance);

        var context = new MessageContext();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await handler.TrackFailureAsync("msg-123", "TestMessage", new byte[] { 1 }, null!, context);
        });
    }

    [Fact]
    public async Task TrackFailureAsync_ShouldClearTrackerAfterMovingToPoisonQueue()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var options = Options.Create(new PoisonMessageOptions
        {
            FailureThreshold = 2
        });
        var handler = new PoisonMessageHandler(store, options, NullLogger<PoisonMessageHandler>.Instance);

        var context = new MessageContext();

        // Act
        await handler.TrackFailureAsync("msg-123", "TestMessage", new byte[] { 1 }, "Error 1", context);
        await handler.TrackFailureAsync("msg-123", "TestMessage", new byte[] { 1 }, "Error 2", context);

        // Now track a new failure for the same message ID (should start fresh)
        var isPoisoned = await handler.TrackFailureAsync("msg-123", "TestMessage", new byte[] { 1 }, "Error 3", context);

        // Assert
        Assert.False(isPoisoned); // Should be false because tracker was cleared
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldRemoveExpiredMessages()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var options = Options.Create(new PoisonMessageOptions
        {
            RetentionPeriod = TimeSpan.FromDays(7)
        });
        var handler = new PoisonMessageHandler(store, options, NullLogger<PoisonMessageHandler>.Instance);

        var oldMessage = new PoisonMessageClass
        {
            MessageType = "OldMessage",
            Payload = new byte[] { 1 },
            LastFailureAt = DateTimeOffset.UtcNow.AddDays(-8)
        };
        await store.StoreAsync(oldMessage);

        var newMessage = new PoisonMessageClass
        {
            MessageType = "NewMessage",
            Payload = new byte[] { 2 },
            LastFailureAt = DateTimeOffset.UtcNow
        };
        await store.StoreAsync(newMessage);

        // Act
        var removedCount = await handler.CleanupExpiredAsync();

        // Assert
        Assert.Equal(1, removedCount);
        var messages = await handler.GetPoisonMessagesAsync();
        Assert.Single(messages);
        Assert.Equal("NewMessage", messages.First().MessageType);
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldReturnZeroWhenNoExpiredMessages()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var options = Options.Create(new PoisonMessageOptions
        {
            RetentionPeriod = TimeSpan.FromDays(7)
        });
        var handler = new PoisonMessageHandler(store, options, NullLogger<PoisonMessageHandler>.Instance);

        var message = new PoisonMessageClass
        {
            MessageType = "TestMessage",
            Payload = new byte[] { 1 },
            LastFailureAt = DateTimeOffset.UtcNow
        };
        await store.StoreAsync(message);

        // Act
        var removedCount = await handler.CleanupExpiredAsync();

        // Assert
        Assert.Equal(0, removedCount);
    }

    [Fact]
    public void Constructor_ShouldThrowWhenStoreIsNull()
    {
        // Arrange
        var options = Options.Create(new PoisonMessageOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            new PoisonMessageHandler(null!, options, NullLogger<PoisonMessageHandler>.Instance);
        });
    }

    [Fact]
    public void Constructor_ShouldThrowWhenOptionsIsNull()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            new PoisonMessageHandler(store, null!, NullLogger<PoisonMessageHandler>.Instance);
        });
    }

    [Fact]
    public void Constructor_ShouldThrowWhenLoggerIsNull()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var options = Options.Create(new PoisonMessageOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            new PoisonMessageHandler(store, options, null!);
        });
    }
}
