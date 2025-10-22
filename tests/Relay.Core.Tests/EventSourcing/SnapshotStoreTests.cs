using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Relay.Core.EventSourcing.Core;
using Relay.Core.EventSourcing.Infrastructure;
using Relay.Core.EventSourcing.Stores;
using Xunit;

#pragma warning disable CS8602 // Dereference of a possibly null reference

namespace Relay.Core.Tests.EventSourcing;

public class SnapshotStoreTests
{
    [Fact]
    public async Task InMemorySnapshotStore_ShouldSaveAndRetrieveSnapshot()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId = Guid.NewGuid();
        var snapshot = new TestSnapshot { Name = "Test Snapshot", Value = 42 };

        // Act
        await store.SaveSnapshotAsync(aggregateId, snapshot, 10);
        var result = await store.GetSnapshotAsync<TestSnapshot>(aggregateId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Value.Snapshot);
        Assert.Equal("Test Snapshot", result.Value.Snapshot.Name);
        Assert.Equal(42, result.Value.Snapshot.Value);
        Assert.Equal(10, result.Value.Version);
    }

    [Fact]
    public async Task InMemorySnapshotStore_ShouldReturnLatestSnapshot()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId = Guid.NewGuid();

        // Act
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "Snapshot 1", Value = 10 }, 5);
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "Snapshot 2", Value = 20 }, 10);
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "Snapshot 3", Value = 30 }, 15);

        var result = await store.GetSnapshotAsync<TestSnapshot>(aggregateId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Snapshot 3", result.Value.Snapshot.Name);
        Assert.Equal(15, result.Value.Version);
    }

    [Fact]
    public async Task InMemorySnapshotStore_ShouldDeleteOldSnapshots()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId = Guid.NewGuid();

        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "Snapshot 1", Value = 10 }, 5);
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "Snapshot 2", Value = 20 }, 10);
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "Snapshot 3", Value = 30 }, 15);

        // Act
        await store.DeleteOldSnapshotsAsync(aggregateId, 12);

        var result = await store.GetSnapshotAsync<TestSnapshot>(aggregateId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Snapshot 3", result.Value.Snapshot.Name);
        Assert.Equal(15, result.Value.Version);
    }

    [Fact]
    public async Task InMemorySnapshotStore_ShouldReturnNullWhenNoSnapshot()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId = Guid.NewGuid();

        // Act
        var result = await store.GetSnapshotAsync<TestSnapshot>(aggregateId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task EfCoreSnapshotStore_ShouldSaveAndRetrieveSnapshot()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EventStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new EventStoreDbContext(options);
        var store = new EfCoreSnapshotStore(context);
        var aggregateId = Guid.NewGuid();
        var snapshot = new TestSnapshot { Name = "EF Snapshot", Value = 99 };

        // Act
        await store.SaveSnapshotAsync(aggregateId, snapshot, 5);
        var result = await store.GetSnapshotAsync<TestSnapshot>(aggregateId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("EF Snapshot", result.Value.Snapshot.Name);
        Assert.Equal(99, result.Value.Snapshot.Value);
        Assert.Equal(5, result.Value.Version);
    }

    [Fact]
    public async Task EfCoreSnapshotStore_ShouldDeleteOldSnapshots()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EventStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new EventStoreDbContext(options);
        var store = new EfCoreSnapshotStore(context);
        var aggregateId = Guid.NewGuid();

        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "Snapshot 1", Value = 10 }, 5);
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "Snapshot 2", Value = 20 }, 10);
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "Snapshot 3", Value = 30 }, 15);

        // Act
        await store.DeleteOldSnapshotsAsync(aggregateId, 12);

        var allSnapshots = await context.Snapshots
            .Where(s => s.AggregateId == aggregateId)
            .ToListAsync();

        // Assert
        Assert.Single(allSnapshots);
        Assert.Equal(15, allSnapshots.First().Version);
    }

    public class TestSnapshot
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
