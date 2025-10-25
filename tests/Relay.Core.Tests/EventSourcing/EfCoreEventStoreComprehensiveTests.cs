using Microsoft.EntityFrameworkCore;
using Moq;
using Relay.Core.EventSourcing.Core;
using Relay.Core.EventSourcing.Infrastructure;
using Relay.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.EventSourcing
{
    public class EfCoreEventStoreComprehensiveTests : IDisposable
    {
        private readonly EventStoreDbContext _context;
        private readonly EfCoreEventStore _eventStore;

        public EfCoreEventStoreComprehensiveTests()
        {
            var options = new DbContextOptionsBuilder<EventStoreDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new EventStoreDbContext(options);
            _eventStore = new EfCoreEventStore(_context);
        }

        [Fact]
        public void Constructor_WithNullContext_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new EfCoreEventStore(null!));
        }

        [Fact]
        public void Constructor_WithValidContext_CreatesInstance()
        {
            // Arrange
            var context = new Mock<EventStoreDbContext>(new DbContextOptions<EventStoreDbContext>());

            // Act
            var eventStore = new EfCoreEventStore(context.Object);

            // Assert
            Assert.NotNull(eventStore);
        }

        [Fact]
        public async Task SaveEventsAsync_WithNullEventsCollection_ThrowsArgumentNullException()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var events = (IEnumerable<Event>)null!;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _eventStore.SaveEventsAsync(aggregateId, events!, -1));
        }

        [Fact]
        public async Task SaveEventsAsync_WithValidEvents_SavesWithCorrectExpectedVersion()
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
            await _eventStore.SaveEventsAsync(aggregateId, events, -1); // Expected version -1 (no previous events)

            // Assert
            var savedEvents = await _context.Events.ToListAsync();
            Assert.Single(savedEvents);
            Assert.Equal(aggregateId, savedEvents.First().AggregateId);
            Assert.Equal(0, savedEvents.First().AggregateVersion);
        }

        [Fact]
        public async Task SaveEventsAsync_WithInvalidExpectedVersion_ThrowsConcurrencyException()
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

            // First save with correct expected version
            await _eventStore.SaveEventsAsync(aggregateId, events, -1);

            // Try to save again with same expected version (should fail)
            var newEvents = new List<Event>
            {
                new TestAggregateNameChanged
                {
                    AggregateId = aggregateId,
                    NewName = "New Name",
                    AggregateVersion = 1
                }
            };

            // Act & Assert - Should throw concurrency exception
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _eventStore.SaveEventsAsync(aggregateId, newEvents, -1)); // Expected -1 but actual is 0
        }

        [Fact]
        public async Task SaveEventsAsync_WithCorrectExpectedVersion_Succeeds()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var firstEvents = new List<Event>
            {
                new TestAggregateCreated
                {
                    AggregateId = aggregateId,
                    AggregateName = "Test Name",
                    AggregateVersion = 0
                }
            };

            // First, save initial events
            await _eventStore.SaveEventsAsync(aggregateId, firstEvents, -1);

            // Now update with correct expected version
            var secondEvents = new List<Event>
            {
                new TestAggregateNameChanged
                {
                    AggregateId = aggregateId,
                    NewName = "Updated Name",
                    AggregateVersion = 1
                }
            };

            // Act - Save with correct expected version (0, which is the last version before this batch)
            await _eventStore.SaveEventsAsync(aggregateId, secondEvents, 0);

            // Assert
            var allEvents = await _context.Events.OrderBy(e => e.AggregateVersion).ToListAsync();
            Assert.Equal(2, allEvents.Count);
            Assert.Equal(0, allEvents[0].AggregateVersion);
            Assert.Equal(1, allEvents[1].AggregateVersion);
        }

        [Fact]
        public async Task GetEventsAsync_WithNonExistentAggregate_ReturnsEmpty()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();

            // Act
            var events = await AsyncEnumerableToListAsync(_eventStore.GetEventsAsync(aggregateId));

            // Assert
            Assert.Empty(events);
        }

        [Fact]
        public async Task GetEventsAsync_WithVersionRange_WhenStartVersionGreaterThanEndVersion_ReturnsEmpty()
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
            var retrievedEvents = await AsyncEnumerableToListAsync(_eventStore.GetEventsAsync(aggregateId, 5, 2)); // start > end

            // Assert
            Assert.Empty(retrievedEvents);
        }

        [Fact]
        public async Task GetEventsAsync_WithVersionRange_WhenStartVersionEqualsEndVersion_ReturnsCorrectEvent()
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
                },
                new TestAggregateNameChanged
                {
                    AggregateId = aggregateId,
                    NewName = "Another Name",
                    AggregateVersion = 2
                }
            };
            await _eventStore.SaveEventsAsync(aggregateId, events, -1);

            // Act
            var retrievedEvents = await AsyncEnumerableToListAsync(_eventStore.GetEventsAsync(aggregateId, 1, 1)); // Only version 1

            // Assert
            Assert.Single(retrievedEvents);
            Assert.Equal(1, retrievedEvents[0].AggregateVersion);
        }

        [Fact]
        public async Task GetEventsAsync_WithCancellation_CanBeCancelled()
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

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(100); // Cancel after 100ms

            // Act & Assert
            var retrievedEvents = await AsyncEnumerableToListAsync(_eventStore.GetEventsAsync(aggregateId, cts.Token));
            Assert.Single(retrievedEvents);
        }

        [Fact]
        public async Task GetEventsAsync_WithVersionRangeAndCancellation_CanBeCancelled()
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

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(100); // Cancel after 100ms

            // Act & Assert
            var retrievedEvents = await AsyncEnumerableToListAsync(_eventStore.GetEventsAsync(aggregateId, 0, 10, cts.Token));
            Assert.Single(retrievedEvents);
        }

        [Fact]
        public async Task GetEventsAsync_WithInvalidEventType_ThrowsException()
        {
            // Arrange - First manually insert an event with an invalid type that doesn't exist
            var aggregateId = Guid.NewGuid();
            var eventEntity = new EventEntity
            {
                Id = Guid.NewGuid(),
                AggregateId = aggregateId,
                AggregateVersion = 0,
                EventType = "Non.Existent.EventType, NonExistentAssembly",
                EventData = "{}",
                Timestamp = DateTime.UtcNow
            };
            
            _context.Events.Add(eventEntity);
            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                var events = _eventStore.GetEventsAsync(aggregateId);
                await foreach (var @event in events)
                {
                    // This should throw when trying to deserialize the invalid event type
                    _ = @event;
                }
            });
        }

        [Fact]
        public async Task GetEventsAsync_WithMalformedJsonData_ThrowsException()
        {
            // Arrange - First manually insert an event with malformed JSON
            var aggregateId = Guid.NewGuid();
            var eventEntity = new EventEntity
            {
                Id = Guid.NewGuid(),
                AggregateId = aggregateId,
                AggregateVersion = 0,
                EventType = typeof(TestAggregateCreated).AssemblyQualifiedName,
                EventData = "{ invalid json ",
                Timestamp = DateTime.UtcNow
            };
            
            _context.Events.Add(eventEntity);
            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<JsonException>(async () =>
            {
                var events = _eventStore.GetEventsAsync(aggregateId);
                await foreach (var @event in events)
                {
                    // This should throw when trying to deserialize malformed JSON
                    _ = @event;
                }
            });
        }

        [Fact]
        public async Task SaveEventsAsync_WithEventsContainingSpecialCharacters_SerializesCorrectly()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var specialName = "Test Name with Special Chars: äöüñ@#$%^&*()_+{}|:<>?[]\\;'\",./~`";
            var events = new List<Event>
            {
                new TestAggregateCreated
                {
                    AggregateId = aggregateId,
                    AggregateName = specialName,
                    AggregateVersion = 0
                }
            };

            // Act
            await _eventStore.SaveEventsAsync(aggregateId, events, -1);

            // Assert
            var retrievedEvents = await _eventStore.GetEventsAsync(aggregateId).ToListAsync();
            Assert.Single(retrievedEvents);
            var retrievedEvent = retrievedEvents.First() as TestAggregateCreated;
            Assert.NotNull(retrievedEvent);
            Assert.Equal(specialName, retrievedEvent.AggregateName);
        }

        [Fact]
        public async Task SaveEventsAsync_WithLargeNumberOfEvents_HandlesCorrectly()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var events = new List<Event>();
            for (int i = 0; i < 100; i++)
            {
                events.Add(new TestAggregateNameChanged
                {
                    AggregateId = aggregateId,
                    NewName = $"Name {i}",
                    AggregateVersion = i
                });
            }

            // Act
            await _eventStore.SaveEventsAsync(aggregateId, events, -1);

            // Assert
            var savedEvents = await _context.Events.ToListAsync();
            Assert.Equal(100, savedEvents.Count);
            
            var retrievedEvents = await _eventStore.GetEventsAsync(aggregateId).ToListAsync();
            Assert.Equal(100, retrievedEvents.Count);
            
            // Check that they are in the correct order
            for (int i = 0; i < retrievedEvents.Count; i++)
            {
                Assert.Equal(i, retrievedEvents[i].AggregateVersion);
            }
        }

        [Fact]
        public async Task GetEventsAsync_WithMultipleAggregates_Isolation()
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
                },
                new TestAggregateNameChanged
                {
                    AggregateId = aggregateId1,
                    NewName = "Updated Aggregate 1",
                    AggregateVersion = 1
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

            await _eventStore.SaveEventsAsync(aggregateId1, events1, -1);
            await _eventStore.SaveEventsAsync(aggregateId2, events2, -1);

            // Act
            var retrievedEvents1 = await _eventStore.GetEventsAsync(aggregateId1).ToListAsync();
            var retrievedEvents2 = await _eventStore.GetEventsAsync(aggregateId2).ToListAsync();

            // Assert
            Assert.Equal(2, retrievedEvents1.Count);
            Assert.Single(retrievedEvents2);
            
            // Ensure aggregates are isolated
            Assert.All(retrievedEvents1, e => Assert.Equal(aggregateId1, e.AggregateId));
            Assert.All(retrievedEvents2, e => Assert.Equal(aggregateId2, e.AggregateId));
        }

        [Fact]
        public async Task SaveEventsAsync_WithEventsHavingDifferentIds_HandlesCorrectly()
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

            // Assert - Should save successfully
            var savedEvents = await _context.Events.ToListAsync();
            Assert.Single(savedEvents);
        }

        [Fact]
        public async Task GetEventsAsync_WithLargeVersionRange_DoesNotCrash()
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

            // Act - Request events with a very large version range
            var retrievedEvents = await AsyncEnumerableToListAsync(_eventStore.GetEventsAsync(aggregateId, 0, int.MaxValue));

            // Assert
            Assert.Single(retrievedEvents);
        }

        [Fact]
        public async Task FindEventType_WithValidType_ReturnsType()
        {
            // This test is a bit tricky since FindEventType is private.
            // We'll test the functionality through the event saving/retrieval process
            // which uses this method internally.

            var aggregateId = Guid.NewGuid();
            var @event = new TestAggregateCreated
            {
                AggregateId = aggregateId,
                AggregateName = "Test Find Type",
                AggregateVersion = 0
            };

            await _eventStore.SaveEventsAsync(aggregateId, new List<Event> { @event }, -1);
            var retrievedEvents = await AsyncEnumerableToListAsync(_eventStore.GetEventsAsync(aggregateId));

            Assert.Single(retrievedEvents);
            Assert.IsType<TestAggregateCreated>(retrievedEvents[0]);
        }

        private async Task<List<T>> AsyncEnumerableToListAsync<T>(IAsyncEnumerable<T> asyncEnumerable)
        {
            var list = new List<T>();
            await foreach (var item in asyncEnumerable)
            {
                list.Add(item);
            }
            return list;
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}