using Relay.Core.EventSourcing.Core;
using Relay.Core.EventSourcing.Repositories;
using Relay.Core.EventSourcing.Stores;
using Relay.Core.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.EventSourcing
{
    public class EventSourcingTests
    {
        [Fact]
        public void AggregateRoot_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var aggregate = new TestAggregate();

            // Assert
            Assert.Equal(-1, aggregate.Version);
            Assert.Empty(aggregate.UncommittedEvents);
        }

        [Fact]
        public void AggregateRoot_ShouldApplyEvent_WhenEventIsApplied()
        {
            // Arrange
            var aggregate = new TestAggregate();
            var id = Guid.NewGuid();

            // Act
            aggregate.Create(id, "Test Name");

            // Assert
            Assert.Equal(id, aggregate.Id);
            Assert.Equal("Test Name", aggregate.Name);
            Assert.Single(aggregate.UncommittedEvents);
            Assert.IsType<TestAggregateCreated>(aggregate.UncommittedEvents.First());
        }

        [Fact]
        public void AggregateRoot_ShouldClearUncommittedEvents_WhenClearIsCalled()
        {
            // Arrange
            var aggregate = new TestAggregate();
            aggregate.Create(Guid.NewGuid(), "Test Name");

            // Act
            aggregate.ClearUncommittedEvents();

            // Assert
            Assert.Empty(aggregate.UncommittedEvents);
        }

        [Fact]
        public void AggregateRoot_ShouldLoadFromHistory_WhenEventsAreProvided()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var events = new Event[]
            {
                new TestAggregateCreated { AggregateId = aggregateId, AggregateName = "Test Name", AggregateVersion = 0 },
                new TestAggregateNameChanged { AggregateId = aggregateId, NewName = "Updated Name", AggregateVersion = 1 }
            };

            // Act
            var aggregate = new TestAggregate();
            aggregate.LoadFromHistory(events);

            // Assert
            Assert.Equal(aggregateId, aggregate.Id);
            Assert.Equal("Updated Name", aggregate.Name);
            Assert.Equal(1, aggregate.Version);
            Assert.Empty(aggregate.UncommittedEvents);
        }

        [Fact]
        public async Task InMemoryEventStore_ShouldSaveAndRetrieveEvents()
        {
            // Arrange
            var eventStore = new InMemoryEventStore();
            var aggregateId = Guid.NewGuid();
            var events = new Event[]
            {
                new TestAggregateCreated { AggregateId = aggregateId, AggregateName = "Test Name", AggregateVersion = 0 }
            };

            // Act
            await eventStore.SaveEventsAsync(aggregateId, events, -1);
            var retrievedEvents = await eventStore.GetEventsAsync(aggregateId).ToListAsync();

            // Assert
            Assert.Single(retrievedEvents);
            Assert.IsType<TestAggregateCreated>(retrievedEvents.First());
            Assert.Equal("Test Name", ((TestAggregateCreated)retrievedEvents.First()).AggregateName);
        }

        [Fact]
        public async Task InMemoryEventStore_ShouldThrowException_WhenConcurrencyConflictOccurs()
        {
            // Arrange
            var eventStore = new InMemoryEventStore();
            var aggregateId = Guid.NewGuid();
            var events = new Event[]
            {
                new TestAggregateCreated { AggregateId = aggregateId, AggregateName = "Test Name", AggregateVersion = 0 }
            };

            await eventStore.SaveEventsAsync(aggregateId, events, -1);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await eventStore.SaveEventsAsync(aggregateId, events, -1));
        }

        [Fact]
        public async Task InMemoryEventStore_ShouldRetrieveEventsByVersionRange()
        {
            // Arrange
            var eventStore = new InMemoryEventStore();
            var aggregateId = Guid.NewGuid();
            var events = new Event[]
            {
                new TestAggregateCreated { AggregateId = aggregateId, AggregateName = "Test Name", AggregateVersion = 0 },
                new TestAggregateNameChanged { AggregateId = aggregateId, NewName = "Name 1", AggregateVersion = 1 },
                new TestAggregateNameChanged { AggregateId = aggregateId, NewName = "Name 2", AggregateVersion = 2 },
                new TestAggregateNameChanged { AggregateId = aggregateId, NewName = "Name 3", AggregateVersion = 3 }
            };

            await eventStore.SaveEventsAsync(aggregateId, events, -1);

            // Act
            var retrievedEvents = await eventStore.GetEventsAsync(aggregateId, 1, 2).ToListAsync();

            // Assert
            Assert.Equal(2, retrievedEvents.Count);
            Assert.Equal(1, retrievedEvents.First().AggregateVersion);
            Assert.Equal(2, retrievedEvents.Last().AggregateVersion);
        }

        [Fact()]
        public async Task EventSourcedRepository_ShouldSaveAndLoadAggregate()
        {
            // Arrange
            var eventStore = new InMemoryEventStore();
            var repository = new EventSourcedRepository<TestAggregate, Guid>(eventStore);
            var aggregate = new TestAggregate();
            var id = Guid.NewGuid();
            
            // Create the aggregate with the ID
            aggregate.Create(id, "Test Name");
            
            // Verify the ID was set correctly before saving
            Assert.Equal(id, aggregate.Id);

            // Act
            await repository.SaveAsync(aggregate);

            var loadedAggregate = await repository.GetByIdAsync(id);

            // Assert
            Assert.NotNull(loadedAggregate);
            Assert.Equal(id, loadedAggregate.Id);
            Assert.Equal("Test Name", loadedAggregate.Name);
            // Note: Version starts at -1, after applying first event at version 0, it should become 0
            Assert.Equal(0, loadedAggregate.Version);
        }

        [Fact]
        public async Task EventSourcedRepository_ShouldApplyMultipleEvents()
        {
            // Arrange
            var eventStore = new InMemoryEventStore();
            var repository = new EventSourcedRepository<TestAggregate, Guid>(eventStore);
            var aggregate = new TestAggregate();
            var id = Guid.NewGuid();
            
            aggregate.Create(id, "Test Name");
            await repository.SaveAsync(aggregate);

            // Act
            aggregate.ChangeName("Updated Name");
            await repository.SaveAsync(aggregate);

            var loadedAggregate = await repository.GetByIdAsync(id);

            // Assert
            Assert.NotNull(loadedAggregate);
            Assert.Equal("Updated Name", loadedAggregate.Name);
            Assert.Equal(1, loadedAggregate.Version);
        }
    }

    // Test aggregate implementation
    public class TestAggregate : AggregateRoot<Guid>
    {
        public string Name { get; private set; } = string.Empty;

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
    }

    // Test events
    public class TestAggregateCreated : Event
    {
        public string AggregateName { get; set; } = string.Empty;
    }

    public class TestAggregateNameChanged : Event
    {
        public string NewName { get; set; } = string.Empty;
    }
}
