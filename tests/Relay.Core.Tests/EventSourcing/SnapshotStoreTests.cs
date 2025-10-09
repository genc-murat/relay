using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Relay.Core.EventSourcing.Core;
using Relay.Core.EventSourcing.Infrastructure;
using Relay.Core.EventSourcing.Stores;
using Xunit;

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
        result.Should().NotBeNull();
        result!.Value.Snapshot.Should().NotBeNull();
        result.Value.Snapshot!.Name.Should().Be("Test Snapshot");
        result.Value.Snapshot.Value.Should().Be(42);
        result.Value.Version.Should().Be(10);
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
        result.Should().NotBeNull();
        result!.Value.Snapshot!.Name.Should().Be("Snapshot 3");
        result.Value.Version.Should().Be(15);
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
        result.Should().NotBeNull();
        result!.Value.Snapshot!.Name.Should().Be("Snapshot 3");
        result.Value.Version.Should().Be(15);
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
        result.Should().BeNull();
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
        result.Should().NotBeNull();
        result!.Value.Snapshot!.Name.Should().Be("EF Snapshot");
        result.Value.Snapshot.Value.Should().Be(99);
        result.Value.Version.Should().Be(5);
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
        allSnapshots.Should().HaveCount(1);
        allSnapshots.First().Version.Should().Be(15);
    }

    public class TestSnapshot
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
