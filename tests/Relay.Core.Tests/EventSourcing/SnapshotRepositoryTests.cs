using System;
using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.EventSourcing.Core;
using Relay.Core.EventSourcing.Repositories;
using Relay.Core.EventSourcing.Stores;
using Xunit;

namespace Relay.Core.Tests.EventSourcing;

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
        loadedAggregate.Should().NotBeNull();
        loadedAggregate!.Id.Should().Be(id);
        loadedAggregate.Name.Should().Be("Name 9");
        loadedAggregate.Version.Should().Be(9);
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
        loadedAggregate.Should().NotBeNull();
        loadedAggregate!.Name.Should().Be("After Snapshot 2");
        loadedAggregate.Version.Should().Be(11);
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
        loadedAggregate.Should().BeNull();
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
        snapshot1.Should().NotBeNull();
        snapshot1!.Value.Version.Should().Be(10);
    }

    // Test Aggregate with Snapshot Support
    public class TestSnapshotAggregate : AggregateRoot<Guid>, ISnapshotable<TestAggregateSnapshot>
    {
        public string Name { get; private set; } = string.Empty;

        public int SnapshotFrequency => 10;

        public void Create(Guid id, string name)
        {
            Apply(new TestAggregateCreated
            {
                AggregateId = id,
                AggregateName = name,
                AggregateVersion = 0
            });
        }

        public void ChangeName(string newName)
        {
            Apply(new TestAggregateNameChanged
            {
                AggregateId = Id,
                NewName = newName,
                AggregateVersion = Version + 1
            });
        }

        public void When(TestAggregateCreated @event)
        {
            Id = @event.AggregateId;
            Name = @event.AggregateName;
        }

        public void When(TestAggregateNameChanged @event)
        {
            Name = @event.NewName;
        }

        public TestAggregateSnapshot CreateSnapshot()
        {
            return new TestAggregateSnapshot
            {
                Id = Id,
                Name = Name,
                Version = Version
            };
        }

        public void RestoreFromSnapshot(TestAggregateSnapshot snapshot)
        {
            Id = snapshot.Id;
            Name = snapshot.Name;

            // Restore version using reflection
            var versionProperty = typeof(AggregateRoot<Guid>).GetProperty(nameof(Version));
            if (versionProperty != null && versionProperty.CanWrite)
            {
                versionProperty.SetValue(this, snapshot.Version);
            }
        }
    }

    public class TestAggregateSnapshot
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Version { get; set; }
    }

    public class TestAggregateCreated : Event
    {
        public string AggregateName { get; set; } = string.Empty;
    }

    public class TestAggregateNameChanged : Event
    {
        public string NewName { get; set; } = string.Empty;
    }
}
