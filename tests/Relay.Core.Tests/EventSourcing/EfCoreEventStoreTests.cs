using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Relay.Core.EventSourcing;
using Relay.Core.Extensions;
using Xunit;

namespace Relay.Core.Tests.EventSourcing
{
    public class EfCoreEventStoreTests : IDisposable
    {
        private readonly EventStoreDbContext _context;
        private readonly EfCoreEventStore _eventStore;

        public EfCoreEventStoreTests()
        {
            // Create in-memory database for testing
            var options = new DbContextOptionsBuilder<EventStoreDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new EventStoreDbContext(options);
            _eventStore = new EfCoreEventStore(_context);
        }

        [Fact]
        public async Task SaveEventsAsync_ShouldSaveEventsToDatabase()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var events = new List<Event>
            {
                new TestAggregateCreated
                {
                    AggregateId = aggregateId,
                    AggregateName = "Test Name",
                    AggregateVersion = 0
                }
            };

            // Act
            await _eventStore.SaveEventsAsync(aggregateId, events, -1);

            // Assert
            var savedEvents = await _context.Events.ToListAsync();
            savedEvents.Should().HaveCount(1);
            savedEvents.First().AggregateId.Should().Be(aggregateId);
            savedEvents.First().EventType.Should().Contain("TestAggregateCreated");
        }

        [Fact]
        public async Task SaveEventsAsync_ShouldThrowException_WhenConcurrencyConflictOccurs()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var events = new List<Event>
            {
                new TestAggregateCreated
                {
                    AggregateId = aggregateId,
                    AggregateName = "Test Name",
                    AggregateVersion = 0
                }
            };

            await _eventStore.SaveEventsAsync(aggregateId, events, -1);

            // Act & Assert
            var conflictingEvents = new List<Event>
            {
                new TestAggregateNameChanged
                {
                    AggregateId = aggregateId,
                    NewName = "New Name",
                    AggregateVersion = 1
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _eventStore.SaveEventsAsync(aggregateId, conflictingEvents, -1));
        }

        [Fact]
        public async Task SaveEventsAsync_ShouldSaveMultipleEvents()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var events = new List<Event>
            {
                new TestAggregateCreated
                {
                    AggregateId = aggregateId,
                    AggregateName = "Test Name",
                    AggregateVersion = 0
                },
                new TestAggregateNameChanged
                {
                    AggregateId = aggregateId,
                    NewName = "Updated Name",
                    AggregateVersion = 1
                }
            };

            // Act
            await _eventStore.SaveEventsAsync(aggregateId, events, -1);

            // Assert
            var savedEvents = await _context.Events.ToListAsync();
            savedEvents.Should().HaveCount(2);
            savedEvents.Should().BeInAscendingOrder(e => e.AggregateVersion);
        }

        [Fact]
        public async Task GetEventsAsync_ShouldRetrieveAllEvents()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var events = new List<Event>
            {
                new TestAggregateCreated
                {
                    AggregateId = aggregateId,
                    AggregateName = "Test Name",
                    AggregateVersion = 0
                }
            };

            await _eventStore.SaveEventsAsync(aggregateId, events, -1);

            // Act
            var retrievedEvents = await _eventStore.GetEventsAsync(aggregateId).ToListAsync();

            // Assert
            retrievedEvents.Should().HaveCount(1);
            retrievedEvents.First().Should().BeOfType<TestAggregateCreated>();
            ((TestAggregateCreated)retrievedEvents.First()).AggregateName.Should().Be("Test Name");
        }

        [Fact]
        public async Task GetEventsAsync_ShouldReturnEmptyList_WhenNoEventsExist()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();

            // Act
            var retrievedEvents = await _eventStore.GetEventsAsync(aggregateId).ToListAsync();

            // Assert
            retrievedEvents.Should().BeEmpty();
        }

        [Fact]
        public async Task GetEventsAsync_ShouldRetrieveEventsInOrder()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var events = new List<Event>
            {
                new TestAggregateCreated
                {
                    AggregateId = aggregateId,
                    AggregateName = "Test Name",
                    AggregateVersion = 0
                },
                new TestAggregateNameChanged
                {
                    AggregateId = aggregateId,
                    NewName = "Name 1",
                    AggregateVersion = 1
                },
                new TestAggregateNameChanged
                {
                    AggregateId = aggregateId,
                    NewName = "Name 2",
                    AggregateVersion = 2
                }
            };

            await _eventStore.SaveEventsAsync(aggregateId, events, -1);

            // Act
            var retrievedEvents = await _eventStore.GetEventsAsync(aggregateId).ToListAsync();

            // Assert
            retrievedEvents.Should().HaveCount(3);
            retrievedEvents.Should().BeInAscendingOrder(e => e.AggregateVersion);
            retrievedEvents[0].AggregateVersion.Should().Be(0);
            retrievedEvents[1].AggregateVersion.Should().Be(1);
            retrievedEvents[2].AggregateVersion.Should().Be(2);
        }

        [Fact]
        public async Task GetEventsAsync_WithVersionRange_ShouldRetrieveCorrectEvents()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var events = new List<Event>
            {
                new TestAggregateCreated
                {
                    AggregateId = aggregateId,
                    AggregateName = "Test Name",
                    AggregateVersion = 0
                },
                new TestAggregateNameChanged
                {
                    AggregateId = aggregateId,
                    NewName = "Name 1",
                    AggregateVersion = 1
                },
                new TestAggregateNameChanged
                {
                    AggregateId = aggregateId,
                    NewName = "Name 2",
                    AggregateVersion = 2
                },
                new TestAggregateNameChanged
                {
                    AggregateId = aggregateId,
                    NewName = "Name 3",
                    AggregateVersion = 3
                }
            };

            await _eventStore.SaveEventsAsync(aggregateId, events, -1);

            // Act
            var retrievedEvents = await _eventStore.GetEventsAsync(aggregateId, 1, 2).ToListAsync();

            // Assert
            retrievedEvents.Should().HaveCount(2);
            retrievedEvents.First().AggregateVersion.Should().Be(1);
            retrievedEvents.Last().AggregateVersion.Should().Be(2);
        }

        [Fact]
        public async Task GetEventsAsync_WithVersionRange_ShouldReturnEmpty_WhenNoEventsInRange()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var events = new List<Event>
            {
                new TestAggregateCreated
                {
                    AggregateId = aggregateId,
                    AggregateName = "Test Name",
                    AggregateVersion = 0
                }
            };

            await _eventStore.SaveEventsAsync(aggregateId, events, -1);

            // Act
            var retrievedEvents = await _eventStore.GetEventsAsync(aggregateId, 5, 10).ToListAsync();

            // Assert
            retrievedEvents.Should().BeEmpty();
        }

        [Fact]
        public async Task SaveEventsAsync_ShouldHandleSequentialSaves()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var firstEvent = new List<Event>
            {
                new TestAggregateCreated
                {
                    AggregateId = aggregateId,
                    AggregateName = "Test Name",
                    AggregateVersion = 0
                }
            };

            var secondEvent = new List<Event>
            {
                new TestAggregateNameChanged
                {
                    AggregateId = aggregateId,
                    NewName = "Updated Name",
                    AggregateVersion = 1
                }
            };

            // Act
            await _eventStore.SaveEventsAsync(aggregateId, firstEvent, -1);
            await _eventStore.SaveEventsAsync(aggregateId, secondEvent, 0);

            // Assert
            var retrievedEvents = await _eventStore.GetEventsAsync(aggregateId).ToListAsync();
            retrievedEvents.Should().HaveCount(2);
        }

        [Fact]
        public async Task SaveEventsAsync_ShouldHandleMultipleAggregates()
        {
            // Arrange
            var aggregateId1 = Guid.NewGuid();
            var aggregateId2 = Guid.NewGuid();

            var events1 = new List<Event>
            {
                new TestAggregateCreated
                {
                    AggregateId = aggregateId1,
                    AggregateName = "Aggregate 1",
                    AggregateVersion = 0
                }
            };

            var events2 = new List<Event>
            {
                new TestAggregateCreated
                {
                    AggregateId = aggregateId2,
                    AggregateName = "Aggregate 2",
                    AggregateVersion = 0
                }
            };

            // Act
            await _eventStore.SaveEventsAsync(aggregateId1, events1, -1);
            await _eventStore.SaveEventsAsync(aggregateId2, events2, -1);

            // Assert
            var retrievedEvents1 = await _eventStore.GetEventsAsync(aggregateId1).ToListAsync();
            var retrievedEvents2 = await _eventStore.GetEventsAsync(aggregateId2).ToListAsync();

            retrievedEvents1.Should().HaveCount(1);
            retrievedEvents2.Should().HaveCount(1);
            ((TestAggregateCreated)retrievedEvents1.First()).AggregateName.Should().Be("Aggregate 1");
            ((TestAggregateCreated)retrievedEvents2.First()).AggregateName.Should().Be("Aggregate 2");
        }

        [Fact]
        public async Task SaveEventsAsync_ShouldNotSave_WhenEventsListIsEmpty()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var events = new List<Event>();

            // Act
            await _eventStore.SaveEventsAsync(aggregateId, events, -1);

            // Assert
            var savedEvents = await _context.Events.ToListAsync();
            savedEvents.Should().BeEmpty();
        }

        [Fact]
        public async Task EventSerialization_ShouldPreserveEventData()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var originalEvent = new TestAggregateCreated
            {
                AggregateId = aggregateId,
                AggregateName = "Test Name With Special Chars: äöü!@#$%",
                AggregateVersion = 0
            };

            // Act
            await _eventStore.SaveEventsAsync(aggregateId, new[] { originalEvent }, -1);
            var retrievedEvents = await _eventStore.GetEventsAsync(aggregateId).ToListAsync();

            // Assert
            var retrievedEvent = retrievedEvents.First() as TestAggregateCreated;
            retrievedEvent.Should().NotBeNull();
            retrievedEvent!.AggregateName.Should().Be(originalEvent.AggregateName);
            retrievedEvent.AggregateId.Should().Be(originalEvent.AggregateId);
            retrievedEvent.AggregateVersion.Should().Be(originalEvent.AggregateVersion);
        }

        [Fact]
        public async Task SaveEventsAsync_ShouldPreserveTimestamp()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var @event = new TestAggregateCreated
            {
                AggregateId = aggregateId,
                AggregateName = "Test Name",
                AggregateVersion = 0
            };
            var originalTimestamp = @event.Timestamp;

            // Act
            await _eventStore.SaveEventsAsync(aggregateId, new[] { @event }, -1);
            var retrievedEvents = await _eventStore.GetEventsAsync(aggregateId).ToListAsync();

            // Assert
            var retrievedEvent = retrievedEvents.First();
            retrievedEvent.Timestamp.Should().BeCloseTo(originalTimestamp, TimeSpan.FromSeconds(1));
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
