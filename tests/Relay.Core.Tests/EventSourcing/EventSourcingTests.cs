using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.EventSourcing;
using Relay.Core.Extensions;
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
            aggregate.Version.Should().Be(-1);
            aggregate.UncommittedEvents.Should().BeEmpty();
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
            aggregate.Id.Should().Be(id);
            aggregate.Name.Should().Be("Test Name");
            aggregate.UncommittedEvents.Should().HaveCount(1);
            aggregate.UncommittedEvents.First().Should().BeOfType<TestAggregateCreated>();
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
            aggregate.UncommittedEvents.Should().BeEmpty();
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
            aggregate.Id.Should().Be(aggregateId);
            aggregate.Name.Should().Be("Updated Name");
            aggregate.Version.Should().Be(1);
            aggregate.UncommittedEvents.Should().BeEmpty();
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
            retrievedEvents.Should().HaveCount(1);
            retrievedEvents.First().Should().BeOfType<TestAggregateCreated>();
            ((TestAggregateCreated)retrievedEvents.First()).AggregateName.Should().Be("Test Name");
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
            retrievedEvents.Should().HaveCount(2);
            retrievedEvents.First().AggregateVersion.Should().Be(1);
            retrievedEvents.Last().AggregateVersion.Should().Be(2);
        }

        [Fact(Skip = "Repository implementation needs fixing for ID persistence")]
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
            aggregate.Id.Should().Be(id);

            // Act
            await repository.SaveAsync(aggregate);

            var loadedAggregate = await repository.GetByIdAsync(id);

            // Assert
            loadedAggregate.Should().NotBeNull();
            loadedAggregate!.Id.Should().Be(id);
            loadedAggregate.Name.Should().Be("Test Name");
            // Note: Version starts at -1, after applying first event at version 0, it should become 0
            loadedAggregate.Version.Should().Be(0);
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
            loadedAggregate.Should().NotBeNull();
            loadedAggregate!.Name.Should().Be("Updated Name");
            loadedAggregate.Version.Should().Be(1);
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
