using System;
using System.Reflection;
using System.Threading.Tasks;
using Relay.Core.EventSourcing.Core;
using Relay.Core.EventSourcing.Repositories;
using Relay.Core.EventSourcing.Stores;
using Relay.Core.Extensions;
using Xunit;

namespace Relay.Core.EventSourcing.Tests;

public class SnapshotRepositoryTests
{
    [Fact]
    public async Task SnapshotRepository_ShouldSaveAndLoadAggregateWithSnapshot()
    {
        // Arrange
        var eventStore = new InMemoryEventStore();
        var snapshotStore = new InMemorySnapshotStore();
        var repository = new SnapshotRepository<TestSnapshotAggregate, Guid, TestAggregateSnapshot>(
            eventStore, snapshotStore);

        var aggregate = new TestSnapshotAggregate();
        var id = Guid.NewGuid();

        // Act - Create aggregate and trigger multiple events to reach snapshot frequency
        aggregate.Create(id, "Initial Name");
        await repository.SaveAsync(aggregate);

        for (int i = 0; i < 9; i++)
        {
            aggregate.ChangeName($"Name {i + 1}");
        }
        await repository.SaveAsync(aggregate); // This should trigger a snapshot at version 9

        // Load aggregate
        var loadedAggregate = await repository.GetByIdAsync(id);

        // Assert
        Assert.NotNull(loadedAggregate);
        Assert.Equal(id, loadedAggregate.Id);
        Assert.Equal("Name 9", loadedAggregate.Name);
        Assert.Equal(9, loadedAggregate.Version);
    }

    [Fact]
    public async Task SnapshotRepository_ShouldLoadFromSnapshotAndReplayEvents()
    {
        // Arrange
        var eventStore = new InMemoryEventStore();
        var snapshotStore = new InMemorySnapshotStore();
        var repository = new SnapshotRepository<TestSnapshotAggregate, Guid, TestAggregateSnapshot>(
            eventStore, snapshotStore);

        var aggregate = new TestSnapshotAggregate();
        var id = Guid.NewGuid();

        // Create and save aggregate
        aggregate.Create(id, "Initial");
        await repository.SaveAsync(aggregate);

        // Add events to trigger snapshot
        for (int i = 0; i < 9; i++)
        {
            aggregate.ChangeName($"Snapshot Name {i}");
        }
        await repository.SaveAsync(aggregate); // Snapshot at version 9

        // Add more events after snapshot
        aggregate.ChangeName("After Snapshot 1");
        aggregate.ChangeName("After Snapshot 2");
        await repository.SaveAsync(aggregate);

        // Act - Load aggregate (should load from snapshot + replay events after snapshot)
        var loadedAggregate = await repository.GetByIdAsync(id);

        // Assert
        Assert.NotNull(loadedAggregate);
        Assert.Equal("After Snapshot 2", loadedAggregate.Name);
        Assert.Equal(11, loadedAggregate.Version);
    }

    [Fact]
    public async Task SnapshotRepository_ShouldReturnNullWhenAggregateDoesNotExist()
    {
        // Arrange
        var eventStore = new InMemoryEventStore();
        var snapshotStore = new InMemorySnapshotStore();
        var repository = new SnapshotRepository<TestSnapshotAggregate, Guid, TestAggregateSnapshot>(
            eventStore, snapshotStore);

        // Act
        var loadedAggregate = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(loadedAggregate);
    }

    [Fact]
    public async Task SnapshotRepository_ShouldCreateSnapshotAtCorrectFrequency()
    {
        // Arrange
        var eventStore = new InMemoryEventStore();
        var snapshotStore = new InMemorySnapshotStore();
        var repository = new SnapshotRepository<TestSnapshotAggregate, Guid, TestAggregateSnapshot>(
            eventStore, snapshotStore);

        var aggregate = new TestSnapshotAggregate();
        var id = Guid.NewGuid();

        // Act
        aggregate.Create(id, "Initial");
        await repository.SaveAsync(aggregate);

        // Add events one by one
        for (int i = 0; i < 14; i++)
        {
            aggregate.ChangeName($"Name {i}");
            await repository.SaveAsync(aggregate);
        }

        // Check snapshots
        var snapshot1 = await snapshotStore.GetSnapshotAsync<TestAggregateSnapshot>(id);

        // Assert - Should have snapshot at version 10 (since frequency is 10)
        Assert.NotNull(snapshot1);
        Assert.Equal(10, snapshot1.Value.Version);
    }

    [Fact]
    public void SnapshotRepository_Constructor_ShouldThrowWhenEventStoreIsNull()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SnapshotRepository<TestSnapshotAggregate, Guid, TestAggregateSnapshot>(null!, new InMemorySnapshotStore()));
    }

    [Fact]
    public void SnapshotRepository_Constructor_ShouldThrowWhenSnapshotStoreIsNull()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SnapshotRepository<TestSnapshotAggregate, Guid, TestAggregateSnapshot>(new InMemoryEventStore(), null!));
    }

    [Fact]
    public async Task SnapshotRepository_SaveAsync_ShouldReturnEarlyWhenNoUncommittedEvents()
    {
        // Arrange
        var eventStore = new InMemoryEventStore();
        var snapshotStore = new InMemorySnapshotStore();
        var repository = new SnapshotRepository<TestSnapshotAggregate, Guid, TestAggregateSnapshot>(
            eventStore, snapshotStore);

        var aggregate = new TestSnapshotAggregate();
        var id = Guid.NewGuid();
        aggregate.Create(id, "Test");

        // Clear uncommitted events
        aggregate.ClearUncommittedEvents();

        // Act - Should not throw and should return early
        await repository.SaveAsync(aggregate);

        // Assert - No events should be saved
        var events = await eventStore.GetEventsAsync(id).ToListAsync();
        Assert.Empty(events);
    }

    [Fact]
    public async Task SnapshotRepository_GetByIdAsync_ShouldLoadFromSnapshotOnly()
    {
        // Arrange
        var eventStore = new InMemoryEventStore();
        var snapshotStore = new InMemorySnapshotStore();
        var repository = new SnapshotRepository<TestSnapshotAggregate, Guid, TestAggregateSnapshot>(
            eventStore, snapshotStore);

        var aggregate = new TestSnapshotAggregate();
        var id = Guid.NewGuid();

        // Create aggregate and save snapshot directly
        aggregate.Create(id, "Snapshot Only");
        var snapshot = aggregate.CreateSnapshot();
        await snapshotStore.SaveSnapshotAsync(id, snapshot, 1);

        // Act
        var loadedAggregate = await repository.GetByIdAsync(id);

        // Assert
        Assert.NotNull(loadedAggregate);
        Assert.Equal(id, loadedAggregate.Id);
        Assert.Equal("Snapshot Only", loadedAggregate.Name);
        Assert.Equal(1, loadedAggregate.Version);
    }

    [Fact]
    public async Task SnapshotRepository_GetByIdAsync_ShouldLoadFromEventsOnly()
    {
        // Arrange
        var eventStore = new InMemoryEventStore();
        var snapshotStore = new InMemorySnapshotStore();
        var repository = new SnapshotRepository<TestSnapshotAggregate, Guid, TestAggregateSnapshot>(
            eventStore, snapshotStore);

        var aggregate = new TestSnapshotAggregate();
        var id = Guid.NewGuid();

        // Create and save aggregate without snapshot
        aggregate.Create(id, "Events Only");
        aggregate.ChangeName("Updated Name");
        await repository.SaveAsync(aggregate);

        // Act
        var loadedAggregate = await repository.GetByIdAsync(id);

        // Assert
        Assert.NotNull(loadedAggregate);
        Assert.Equal(id, loadedAggregate.Id);
        Assert.Equal("Updated Name", loadedAggregate.Name);
        Assert.Equal(1, loadedAggregate.Version);
    }

    [Fact]
    public async Task SnapshotRepository_GetAggregateGuid_ShouldThrowForNonGuidId()
    {
        // Arrange
        var eventStore = new InMemoryEventStore();
        var snapshotStore = new InMemorySnapshotStore();
        var repository = new SnapshotRepository<TestSnapshotAggregateWithStringId, string, TestAggregateSnapshot>(
            eventStore, snapshotStore);

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await repository.GetByIdAsync("test-id"));
    }
}
