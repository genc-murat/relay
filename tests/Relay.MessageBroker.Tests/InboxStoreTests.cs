using Relay.MessageBroker.Inbox;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class InMemoryInboxStoreTests
{
    [Fact]
    public async Task ExistsAsync_ShouldReturnFalseForNonExistentMessage()
    {
        // Arrange
        var store = new InMemoryInboxStore();

        // Act
        var exists = await store.ExistsAsync("non-existent-id");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrueForStoredMessage()
    {
        // Arrange
        var store = new InMemoryInboxStore();
        var message = new InboxMessage
        {
            MessageId = "test-id",
            MessageType = "TestMessage"
        };
        await store.StoreAsync(message);

        // Act
        var exists = await store.ExistsAsync("test-id");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_ShouldThrowWhenMessageIdIsNull()
    {
        // Arrange
        var store = new InMemoryInboxStore();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            store.ExistsAsync(null!).AsTask());
    }

    [Fact]
    public async Task ExistsAsync_ShouldThrowWhenMessageIdIsEmpty()
    {
        // Arrange
        var store = new InMemoryInboxStore();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            store.ExistsAsync(string.Empty).AsTask());
    }

    [Fact]
    public async Task StoreAsync_ShouldStoreMessageWithTimestamp()
    {
        // Arrange
        var store = new InMemoryInboxStore();
        var message = new InboxMessage
        {
            MessageId = "test-id",
            MessageType = "TestMessage",
            ConsumerName = "TestConsumer"
        };

        // Act
        await store.StoreAsync(message);

        // Assert
        var all = store.GetAll();
        var stored = all.First();
        Assert.Equal("test-id", stored.MessageId);
        Assert.Equal("TestMessage", stored.MessageType);
        Assert.Equal("TestConsumer", stored.ConsumerName);
        Assert.True(stored.ProcessedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task StoreAsync_ShouldThrowWhenMessageIsNull()
    {
        // Arrange
        var store = new InMemoryInboxStore();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            store.StoreAsync(null!).AsTask());
    }

    [Fact]
    public async Task StoreAsync_ShouldThrowWhenMessageIdIsNull()
    {
        // Arrange
        var store = new InMemoryInboxStore();
        var message = new InboxMessage
        {
            MessageId = null!,
            MessageType = "TestMessage"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            store.StoreAsync(message).AsTask());
    }

    [Fact]
    public async Task StoreAsync_ShouldOverwriteExistingMessage()
    {
        // Arrange
        var store = new InMemoryInboxStore();
        var message1 = new InboxMessage
        {
            MessageId = "test-id",
            MessageType = "TestMessage1"
        };
        var message2 = new InboxMessage
        {
            MessageId = "test-id",
            MessageType = "TestMessage2"
        };

        // Act
        await store.StoreAsync(message1);
        await store.StoreAsync(message2);

        // Assert
        var all = store.GetAll();
        Assert.Single(all);
        Assert.Equal("TestMessage2", all.First().MessageType);
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldRemoveExpiredMessages()
    {
        // Arrange
        var store = new InMemoryInboxStore();
        var oldMessage = new InboxMessage
        {
            MessageId = "old-id",
            MessageType = "OldMessage"
        };
        await store.StoreAsync(oldMessage);
        
        // Manually set old timestamp
        var all = store.GetAll();
        var stored = all.First(m => m.MessageId == "old-id");
        stored.ProcessedAt = DateTimeOffset.UtcNow.AddDays(-8);

        var newMessage = new InboxMessage
        {
            MessageId = "new-id",
            MessageType = "NewMessage"
        };
        await store.StoreAsync(newMessage);

        // Act
        var removedCount = await store.CleanupExpiredAsync(TimeSpan.FromDays(7));

        // Assert
        Assert.Equal(1, removedCount);
        var remaining = store.GetAll();
        Assert.Single(remaining);
        Assert.Equal("new-id", remaining.First().MessageId);
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldReturnZeroWhenNoExpiredMessages()
    {
        // Arrange
        var store = new InMemoryInboxStore();
        var message = new InboxMessage
        {
            MessageId = "test-id",
            MessageType = "TestMessage"
        };
        await store.StoreAsync(message);

        // Act
        var removedCount = await store.CleanupExpiredAsync(TimeSpan.FromDays(7));

        // Assert
        Assert.Equal(0, removedCount);
        Assert.Single(store.GetAll());
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldHandleEmptyStore()
    {
        // Arrange
        var store = new InMemoryInboxStore();

        // Act
        var removedCount = await store.CleanupExpiredAsync(TimeSpan.FromDays(7));

        // Assert
        Assert.Equal(0, removedCount);
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldRemoveMultipleExpiredMessages()
    {
        // Arrange
        var store = new InMemoryInboxStore();
        
        for (int i = 0; i < 5; i++)
        {
            var message = new InboxMessage
            {
                MessageId = $"old-id-{i}",
                MessageType = "OldMessage"
            };
            await store.StoreAsync(message);
            
            var all = store.GetAll();
            var stored = all.First(m => m.MessageId == $"old-id-{i}");
            stored.ProcessedAt = DateTimeOffset.UtcNow.AddDays(-8);
        }

        // Act
        var removedCount = await store.CleanupExpiredAsync(TimeSpan.FromDays(7));

        // Assert
        Assert.Equal(5, removedCount);
        Assert.Empty(store.GetAll());
    }

    [Fact]
    public async Task Clear_ShouldRemoveAllMessages()
    {
        // Arrange
        var store = new InMemoryInboxStore();
        await store.StoreAsync(new InboxMessage { MessageId = "id1", MessageType = "Test1" });
        await store.StoreAsync(new InboxMessage { MessageId = "id2", MessageType = "Test2" });

        // Act
        store.Clear();

        // Assert
        Assert.Empty(store.GetAll());
    }
}
