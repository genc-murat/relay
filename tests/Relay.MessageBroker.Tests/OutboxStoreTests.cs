using Relay.MessageBroker.Outbox;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class InMemoryOutboxStoreTests
{
    [Fact]
    public async Task StoreAsync_ShouldGenerateIdAndSetDefaults()
    {
        // Arrange
        var store = new InMemoryOutboxStore();
        var message = new OutboxMessage
        {
            MessageType = "TestMessage",
            Payload = new byte[] { 1, 2, 3 }
        };

        // Act
        var result = await store.StoreAsync(message);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(OutboxMessageStatus.Pending, result.Status);
        Assert.True(result.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Equal("TestMessage", result.MessageType);
    }

    [Fact]
    public async Task StoreAsync_ShouldThrowWhenMessageIsNull()
    {
        // Arrange
        var store = new InMemoryOutboxStore();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            store.StoreAsync(null!).AsTask());
    }

    [Fact]
    public async Task GetPendingAsync_ShouldReturnPendingMessagesOnly()
    {
        // Arrange
        var store = new InMemoryOutboxStore();
        var message1 = await store.StoreAsync(new OutboxMessage { MessageType = "Test1", Payload = new byte[] { 1 } });
        var message2 = await store.StoreAsync(new OutboxMessage { MessageType = "Test2", Payload = new byte[] { 2 } });
        await store.MarkAsPublishedAsync(message1.Id);

        // Act
        var pending = await store.GetPendingAsync(10);

        // Assert
        Assert.Single(pending);
        Assert.Equal(message2.Id, pending.First().Id);
    }

    [Fact]
    public async Task GetPendingAsync_ShouldRespectBatchSize()
    {
        // Arrange
        var store = new InMemoryOutboxStore();
        for (int i = 0; i < 5; i++)
        {
            await store.StoreAsync(new OutboxMessage { MessageType = $"Test{i}", Payload = new byte[] { (byte)i } });
        }

        // Act
        var pending = await store.GetPendingAsync(3);

        // Assert
        Assert.Equal(3, pending.Count());
    }

    [Fact]
    public async Task GetPendingAsync_ShouldReturnMessagesInCreationOrder()
    {
        // Arrange
        var store = new InMemoryOutboxStore();
        var message1 = await store.StoreAsync(new OutboxMessage { MessageType = "First", Payload = new byte[] { 1 } });
        await Task.Delay(10); // Ensure different timestamps
        var message2 = await store.StoreAsync(new OutboxMessage { MessageType = "Second", Payload = new byte[] { 2 } });

        // Act
        var pending = await store.GetPendingAsync(10);

        // Assert
        var list = pending.ToList();
        Assert.Equal(message1.Id, list[0].Id);
        Assert.Equal(message2.Id, list[1].Id);
    }

    [Fact]
    public async Task MarkAsPublishedAsync_ShouldUpdateStatusAndTimestamp()
    {
        // Arrange
        var store = new InMemoryOutboxStore();
        var message = await store.StoreAsync(new OutboxMessage { MessageType = "Test", Payload = new byte[] { 1 } });

        // Act
        await store.MarkAsPublishedAsync(message.Id);

        // Assert
        var all = store.GetAll();
        var updated = all.First(m => m.Id == message.Id);
        Assert.Equal(OutboxMessageStatus.Published, updated.Status);
        Assert.NotNull(updated.PublishedAt);
        Assert.True(updated.PublishedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task MarkAsPublishedAsync_ShouldHandleNonExistentMessage()
    {
        // Arrange
        var store = new InMemoryOutboxStore();

        // Act & Assert (should not throw)
        await store.MarkAsPublishedAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task MarkAsFailedAsync_ShouldUpdateStatusAndIncrementRetryCount()
    {
        // Arrange
        var store = new InMemoryOutboxStore();
        var message = await store.StoreAsync(new OutboxMessage { MessageType = "Test", Payload = new byte[] { 1 } });

        // Act
        await store.MarkAsFailedAsync(message.Id, "Test error");

        // Assert
        var all = store.GetAll();
        var updated = all.First(m => m.Id == message.Id);
        Assert.Equal(OutboxMessageStatus.Failed, updated.Status);
        Assert.Equal(1, updated.RetryCount);
        Assert.Equal("Test error", updated.LastError);
    }

    [Fact]
    public async Task MarkAsFailedAsync_ShouldIncrementRetryCountOnMultipleCalls()
    {
        // Arrange
        var store = new InMemoryOutboxStore();
        var message = await store.StoreAsync(new OutboxMessage { MessageType = "Test", Payload = new byte[] { 1 } });

        // Act
        await store.MarkAsFailedAsync(message.Id, "Error 1");
        await store.MarkAsFailedAsync(message.Id, "Error 2");
        await store.MarkAsFailedAsync(message.Id, "Error 3");

        // Assert
        var all = store.GetAll();
        var updated = all.First(m => m.Id == message.Id);
        Assert.Equal(3, updated.RetryCount);
        Assert.Equal("Error 3", updated.LastError);
    }

    [Fact]
    public async Task GetFailedAsync_ShouldReturnFailedMessagesOnly()
    {
        // Arrange
        var store = new InMemoryOutboxStore();
        var message1 = await store.StoreAsync(new OutboxMessage { MessageType = "Test1", Payload = new byte[] { 1 } });
        var message2 = await store.StoreAsync(new OutboxMessage { MessageType = "Test2", Payload = new byte[] { 2 } });
        var message3 = await store.StoreAsync(new OutboxMessage { MessageType = "Test3", Payload = new byte[] { 3 } });
        
        await store.MarkAsFailedAsync(message1.Id, "Error");
        await store.MarkAsPublishedAsync(message2.Id);

        // Act
        var failed = await store.GetFailedAsync(10);

        // Assert
        Assert.Single(failed);
        Assert.Equal(message1.Id, failed.First().Id);
    }

    [Fact]
    public async Task GetFailedAsync_ShouldRespectBatchSize()
    {
        // Arrange
        var store = new InMemoryOutboxStore();
        for (int i = 0; i < 5; i++)
        {
            var message = await store.StoreAsync(new OutboxMessage { MessageType = $"Test{i}", Payload = new byte[] { (byte)i } });
            await store.MarkAsFailedAsync(message.Id, "Error");
        }

        // Act
        var failed = await store.GetFailedAsync(3);

        // Assert
        Assert.Equal(3, failed.Count());
    }

    [Fact]
    public async Task Clear_ShouldRemoveAllMessages()
    {
        // Arrange
        var store = new InMemoryOutboxStore();
        await store.StoreAsync(new OutboxMessage { MessageType = "Test1", Payload = new byte[] { 1 } });
        await store.StoreAsync(new OutboxMessage { MessageType = "Test2", Payload = new byte[] { 2 } });

        // Act
        store.Clear();

        // Assert
        Assert.Empty(store.GetAll());
    }
}
