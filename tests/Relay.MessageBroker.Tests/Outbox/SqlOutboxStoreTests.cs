using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Relay.MessageBroker.Outbox.Tests;

public class SqlOutboxStoreTests
{
    [Fact]
    public void Constructor_WithValidContextFactory_ShouldInitializeSuccessfully()
    {
        // Arrange
        var contextFactory = new MockDbContextFactory();

        // Act
        var store = new SqlOutboxStore(contextFactory);

        // Assert
        Assert.NotNull(store);
    }

    [Fact]
    public void Constructor_WithNullContextFactory_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new SqlOutboxStore(null!));
        Assert.Equal("contextFactory", exception.ParamName);
    }

    [Fact]
    public async Task StoreAsync_WithValidMessage_ShouldStoreInDatabase()
    {
        // Arrange
        var options = CreateDbContextOptions();
        var contextFactory = new InMemoryDbContextFactoryForTesting(options);
        var store = new SqlOutboxStore(contextFactory);

        var message = new OutboxMessage
        {
            MessageType = "TestMessage",
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Test" })
        };

        // Act
        var result = await store.StoreAsync(message);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(OutboxMessageStatus.Pending, result.Status);
        Assert.True(result.CreatedAt <= DateTimeOffset.UtcNow);
        
        // Verify the message was saved in the database using a separate context
        await using var verificationContext = new OutboxDbContext(options);
        var savedMessage = await verificationContext.OutboxMessages.FirstAsync();
        Assert.Equal(result.Id, savedMessage.Id);
        Assert.Equal("TestMessage", savedMessage.MessageType);
        Assert.Equal(OutboxMessageStatus.Pending, savedMessage.Status);
    }

    [Fact]
    public async Task StoreAsync_WithEmptyId_ShouldGenerateNewId()
    {
        // Arrange
        var options = CreateDbContextOptions();
        await using var context = new OutboxDbContext(options);
        var contextFactory = new InMemoryDbContextFactory(context);
        var store = new SqlOutboxStore(contextFactory);

        var message = new OutboxMessage
        {
            Id = Guid.Empty,
            MessageType = "TestMessage",
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Test" })
        };

        // Act
        var result = await store.StoreAsync(message);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(result.Id, message.Id);
    }

    [Fact]
    public async Task StoreAsync_WithExistingId_ShouldUseExistingId()
    {
        // Arrange
        var options = CreateDbContextOptions();
        await using var context = new OutboxDbContext(options);
        var contextFactory = new InMemoryDbContextFactory(context);
        var store = new SqlOutboxStore(contextFactory);

        var existingId = Guid.NewGuid();
        var message = new OutboxMessage
        {
            Id = existingId,
            MessageType = "TestMessage",
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Test" })
        };

        // Act
        var result = await store.StoreAsync(message);

        // Assert
        Assert.Equal(existingId, result.Id);
    }

    [Fact]
    public async Task StoreAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var contextFactory = new MockDbContextFactory();
        var store = new SqlOutboxStore(contextFactory);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.StoreAsync(null!).AsTask());
    }

    [Fact]
    public async Task GetPendingAsync_WithNoPendingMessages_ShouldReturnEmptyCollection()
    {
        // Arrange
        var options = CreateDbContextOptions();
        var contextFactory = new InMemoryDbContextFactoryForTesting(options);
        var store = new SqlOutboxStore(contextFactory);

        // Add a published message (not pending) using a separate context
        await using var addContext = new OutboxDbContext(options);
        var publishedMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "PublishedMessage",
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Published" }),
            Status = OutboxMessageStatus.Published
        };
        addContext.OutboxMessages.Add(publishedMessage);
        await addContext.SaveChangesAsync();

        // Act
        var result = await store.GetPendingAsync(10);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPendingAsync_WithPendingMessages_ShouldReturnPendingMessages()
    {
        // Arrange
        var options = CreateDbContextOptions();
        var contextFactory = new InMemoryDbContextFactoryForTesting(options);
        var store = new SqlOutboxStore(contextFactory);

        // Add pending messages using a separate context
        await using var addContext = new OutboxDbContext(options);
        var pendingMessage1 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "PendingMessage1",
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Pending1" }),
            Status = OutboxMessageStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        
        var pendingMessage2 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "PendingMessage2",
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Pending2" }),
            Status = OutboxMessageStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        var failedMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "FailedMessage",
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Failed" }),
            Status = OutboxMessageStatus.Failed
        };

        addContext.OutboxMessages.AddRange(pendingMessage1, pendingMessage2, failedMessage);
        await addContext.SaveChangesAsync();

        // Act
        var result = await store.GetPendingAsync(10);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.All(resultList, m => Assert.Equal(OutboxMessageStatus.Pending, m.Status));
        Assert.Contains(resultList, m => m.Id == pendingMessage1.Id);
        Assert.Contains(resultList, m => m.Id == pendingMessage2.Id);
        Assert.DoesNotContain(resultList, m => m.Id == failedMessage.Id);
    }

    [Fact]
    public async Task GetPendingAsync_WithBatchSizeLimit_ShouldRespectBatchSize()
    {
        // Arrange
        var options = CreateDbContextOptions();
        var contextFactory = new InMemoryDbContextFactoryForTesting(options);
        var store = new SqlOutboxStore(contextFactory);

        // Add multiple pending messages using a separate context
        await using var addContext = new OutboxDbContext(options);
        for (int i = 0; i < 5; i++)
        {
            addContext.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                MessageType = $"Message{i}",
                Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = $"Content{i}" }),
                Status = OutboxMessageStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-i)
            });
        }
        await addContext.SaveChangesAsync();

        // Act
        var result = await store.GetPendingAsync(3); // Limit to 3

        // Assert
        var resultList = result.ToList();
        Assert.Equal(3, resultList.Count);
    }

    [Fact]
    public async Task GetPendingAsync_WithPendingMessages_ShouldOrderByCreatedAt()
    {
        // Arrange
        var options = CreateDbContextOptions();
        var contextFactory = new InMemoryDbContextFactoryForTesting(options);
        var store = new SqlOutboxStore(contextFactory);

        // Add pending messages with different creation times using a separate context
        await using var addContext = new OutboxDbContext(options);
        var oldMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "OldMessage",
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Old" }),
            Status = OutboxMessageStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
        };
        
        var newMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "NewMessage",
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "New" }),
            Status = OutboxMessageStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        var middleMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "MiddleMessage",
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Middle" }),
            Status = OutboxMessageStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };

        addContext.OutboxMessages.AddRange(oldMessage, newMessage, middleMessage);
        await addContext.SaveChangesAsync();

        // Act
        var result = await store.GetPendingAsync(10);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(3, resultList.Count);
        Assert.Equal(oldMessage.Id, resultList[0].Id); // Oldest first
        Assert.Equal(middleMessage.Id, resultList[1].Id);
        Assert.Equal(newMessage.Id, resultList[2].Id); // Newest last
    }

    [Fact]
    public async Task MarkAsPublishedAsync_WithExistingMessage_ShouldUpdateStatusAndPublishedAt()
    {
        // Arrange
        var options = CreateDbContextOptions();
        var contextFactory = new InMemoryDbContextFactoryForTesting(options);
        var store = new SqlOutboxStore(contextFactory);
        
        // Add message using a separate context instance
        await using var addContext = new OutboxDbContext(options);
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "TestMessage",
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Test" }),
            Status = OutboxMessageStatus.Pending
        };
        addContext.OutboxMessages.Add(message);
        await addContext.SaveChangesAsync();

        // Act
        await store.MarkAsPublishedAsync(message.Id);

        // Assert - use a separate context for verification
        await using var verifyContext = new OutboxDbContext(options);
        var updatedMessage = await verifyContext.OutboxMessages.FirstAsync(m => m.Id == message.Id);
        Assert.Equal(OutboxMessageStatus.Published, updatedMessage.Status);
        Assert.NotNull(updatedMessage.PublishedAt);
        Assert.True(updatedMessage.PublishedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task MarkAsPublishedAsync_WithNonExistingMessage_ShouldNotThrow()
    {
        // Arrange
        var options = CreateDbContextOptions();
        await using var context = new OutboxDbContext(options);
        var contextFactory = new InMemoryDbContextFactory(context);
        var store = new SqlOutboxStore(contextFactory);

        var nonExistingId = Guid.NewGuid();

        // Act & Assert - Should not throw
        var exception = await Record.ExceptionAsync(() => store.MarkAsPublishedAsync(nonExistingId).AsTask());
        Assert.Null(exception); // No exception should be thrown
    }

    [Fact]
    public async Task MarkAsFailedAsync_WithExistingMessage_ShouldUpdateStatusAndIncrementRetryCount()
    {
        // Arrange
        var options = CreateDbContextOptions();
        var contextFactory = new InMemoryDbContextFactoryForTesting(options);
        var store = new SqlOutboxStore(contextFactory);
        
        // Add message using a separate context instance
        await using var addContext = new OutboxDbContext(options);
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "TestMessage",
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Test" }),
            Status = OutboxMessageStatus.Pending,
            RetryCount = 1,
            LastError = null
        };
        addContext.OutboxMessages.Add(message);
        await addContext.SaveChangesAsync();

        var error = "Test error message";

        // Act
        await store.MarkAsFailedAsync(message.Id, error);

        // Assert - use a separate context for verification
        await using var verifyContext = new OutboxDbContext(options);
        var updatedMessage = await verifyContext.OutboxMessages.FirstAsync(m => m.Id == message.Id);
        Assert.Equal(OutboxMessageStatus.Failed, updatedMessage.Status);
        Assert.Equal(2, updatedMessage.RetryCount); // Increased from 1 to 2
        Assert.Equal(error, updatedMessage.LastError);
    }

    [Fact]
    public async Task MarkAsFailedAsync_WithNonExistingMessage_ShouldNotThrow()
    {
        // Arrange
        var options = CreateDbContextOptions();
        await using var context = new OutboxDbContext(options);
        var contextFactory = new InMemoryDbContextFactory(context);
        var store = new SqlOutboxStore(contextFactory);

        var nonExistingId = Guid.NewGuid();
        var error = "Test error message";

        // Act & Assert - Should not throw
        var exception = await Record.ExceptionAsync(() => store.MarkAsFailedAsync(nonExistingId, error).AsTask());
        Assert.Null(exception); // No exception should be thrown
    }

    [Fact]
    public async Task GetFailedAsync_WithNoFailedMessages_ShouldReturnEmptyCollection()
    {
        // Arrange
        var options = CreateDbContextOptions();
        var contextFactory = new InMemoryDbContextFactoryForTesting(options);
        var store = new SqlOutboxStore(contextFactory);

        // Add a pending message (not failed) using a separate context
        await using var addContext = new OutboxDbContext(options);
        var pendingMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "PendingMessage",
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Pending" }),
            Status = OutboxMessageStatus.Pending
        };
        addContext.OutboxMessages.Add(pendingMessage);
        await addContext.SaveChangesAsync();

        // Act
        var result = await store.GetFailedAsync(10);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFailedAsync_WithFailedMessages_ShouldReturnFailedMessages()
    {
        // Arrange
        var options = CreateDbContextOptions();
        var contextFactory = new InMemoryDbContextFactoryForTesting(options);
        var store = new SqlOutboxStore(contextFactory);

        // Add failed and non-failed messages using a separate context
        await using var addContext = new OutboxDbContext(options);
        var failedMessage1 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "FailedMessage1",
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Failed1" }),
            Status = OutboxMessageStatus.Failed
        };
        
        var failedMessage2 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "FailedMessage2",
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Failed2" }),
            Status = OutboxMessageStatus.Failed
        };
        
        var pendingMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "PendingMessage",
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Pending" }),
            Status = OutboxMessageStatus.Pending
        };

        addContext.OutboxMessages.AddRange(failedMessage1, failedMessage2, pendingMessage);
        await addContext.SaveChangesAsync();

        // Act
        var result = await store.GetFailedAsync(10);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.All(resultList, m => Assert.Equal(OutboxMessageStatus.Failed, m.Status));
        Assert.Contains(resultList, m => m.Id == failedMessage1.Id);
        Assert.Contains(resultList, m => m.Id == failedMessage2.Id);
        Assert.DoesNotContain(resultList, m => m.Id == pendingMessage.Id);
    }

    [Fact]
    public async Task GetFailedAsync_WithBatchSizeLimit_ShouldRespectBatchSize()
    {
        // Arrange
        var options = CreateDbContextOptions();
        var contextFactory = new InMemoryDbContextFactoryForTesting(options);
        var store = new SqlOutboxStore(contextFactory);

        // Add multiple failed messages using a separate context
        await using var addContext = new OutboxDbContext(options);
        for (int i = 0; i < 5; i++)
        {
            addContext.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                MessageType = $"FailedMessage{i}",
                Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = $"Failed{i}" }),
                Status = OutboxMessageStatus.Failed,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-i)
            });
        }
        await addContext.SaveChangesAsync();

        // Act
        var result = await store.GetFailedAsync(3); // Limit to 3

        // Assert
        var resultList = result.ToList();
        Assert.Equal(3, resultList.Count);
    }

    [Fact]
    public async Task GetFailedAsync_WithFailedMessages_ShouldOrderByCreatedAt()
    {
        // Arrange
        var options = CreateDbContextOptions();
        var contextFactory = new InMemoryDbContextFactoryForTesting(options);
        var store = new SqlOutboxStore(contextFactory);

        // Add failed messages with different creation times using a separate context  
        await using var addContext = new OutboxDbContext(options);
        var oldMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "OldFailedMessage",
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Old" }),
            Status = OutboxMessageStatus.Failed,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
        };
        
        var newMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "NewFailedMessage",
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "New" }),
            Status = OutboxMessageStatus.Failed,
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        var middleMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "MiddleFailedMessage",
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Middle" }),
            Status = OutboxMessageStatus.Failed,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };

        addContext.OutboxMessages.AddRange(oldMessage, newMessage, middleMessage);
        await addContext.SaveChangesAsync();

        // Act
        var result = await store.GetFailedAsync(10);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(3, resultList.Count);
        Assert.Equal(oldMessage.Id, resultList[0].Id); // Oldest first
        Assert.Equal(middleMessage.Id, resultList[1].Id);
        Assert.Equal(newMessage.Id, resultList[2].Id); // Newest last
    }

    private DbContextOptions<OutboxDbContext> CreateDbContextOptions()
    {
        return new DbContextOptionsBuilder<OutboxDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB name for each test
            .Options;
    }

    // Supporting classes for testing
    private class MockDbContextFactory : IDbContextFactory<OutboxDbContext>
    {
        public OutboxDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<OutboxDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new OutboxDbContext(options);
        }

        public ValueTask<OutboxDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(CreateDbContext());
        }
    }

    private class InMemoryDbContextFactory : IDbContextFactory<OutboxDbContext>
    {
        private readonly OutboxDbContext _context;

        public InMemoryDbContextFactory(OutboxDbContext context)
        {
            _context = context;
        }

        public OutboxDbContext CreateDbContext()
        {
            return _context;
        }

        public ValueTask<OutboxDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(_context);
        }
    }

    private class InMemoryDbContextFactoryForTesting : IDbContextFactory<OutboxDbContext>
    {
        private readonly DbContextOptions<OutboxDbContext> _options;

        public InMemoryDbContextFactoryForTesting(DbContextOptions<OutboxDbContext> options)
        {
            _options = options;
        }

        public OutboxDbContext CreateDbContext()
        {
            return new OutboxDbContext(_options);
        }

        public ValueTask<OutboxDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new OutboxDbContext(_options));
        }
    }
}