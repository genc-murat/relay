using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Relay.Core.EventSourcing.Caching;
using Relay.Core.EventSourcing.Core;
using Relay.Core.EventSourcing.Stores;
using Relay.Core.Extensions;
using Xunit;

namespace Relay.Core.Tests.EventSourcing
{
    public class CachedEventStoreTests
    {
        [Fact]
        public async Task CachedEventStore_ShouldCacheEvents()
        {
            // Arrange
            var innerStore = new InMemoryEventStore();
            var cache = new InMemoryEventStoreCache(new MemoryCache(new MemoryCacheOptions()));
            var cachedStore = new CachedEventStore(innerStore, cache);

            var aggregateId = Guid.NewGuid();
            var events = new Event[]
            {
                new TestEvent { AggregateId = aggregateId, AggregateVersion = 0, Data = "Event 1" }
            };

            // Act - Save and retrieve first time
            await cachedStore.SaveEventsAsync(aggregateId, events, -1);
            var retrievedEvents1 = await cachedStore.GetEventsAsync(aggregateId).ToListAsync();

            // Act - Retrieve second time (should come from cache)
            var retrievedEvents2 = await cachedStore.GetEventsAsync(aggregateId).ToListAsync();

            // Assert
            Assert.Equal(1, retrievedEvents1.Count);
            Assert.Equal(1, retrievedEvents2.Count);
            Assert.Equal("Event 1", ((TestEvent)retrievedEvents1[0]).Data);
        }

        [Fact]
        public async Task CachedEventStore_ShouldInvalidateCacheOnSave()
        {
            // Arrange
            var innerStore = new InMemoryEventStore();
            var cache = new InMemoryEventStoreCache(new MemoryCache(new MemoryCacheOptions()));
            var cachedStore = new CachedEventStore(innerStore, cache);

            var aggregateId = Guid.NewGuid();
            var events1 = new Event[]
            {
                new TestEvent { AggregateId = aggregateId, AggregateVersion = 0, Data = "Event 1" }
            };

            // Act - Save and retrieve
            await cachedStore.SaveEventsAsync(aggregateId, events1, -1);
            await cachedStore.GetEventsAsync(aggregateId).ToListAsync();

            // Add more events
            var events2 = new Event[]
            {
                new TestEvent { AggregateId = aggregateId, AggregateVersion = 1, Data = "Event 2" }
            };
            await cachedStore.SaveEventsAsync(aggregateId, events2, 0);

            // Retrieve again (cache should be invalidated)
            var allEvents = await cachedStore.GetEventsAsync(aggregateId).ToListAsync();

            // Assert
            Assert.Equal(2, allEvents.Count);
        }

        [Fact]
        public async Task InMemoryEventStoreCache_ShouldStoreAndRetrieveEvents()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var cache = new InMemoryEventStoreCache(memoryCache);
            var aggregateId = Guid.NewGuid();
            var events = new System.Collections.Generic.List<Event>
            {
                new TestEvent { Data = "Test Event" }
            };

            // Act
            await cache.SetEventsAsync(aggregateId, events);
            var retrievedEvents = await cache.GetEventsAsync(aggregateId);

            // Assert
            Assert.NotNull(retrievedEvents);
            Assert.Equal(1, retrievedEvents!.Count);
        }

        [Fact]
        public async Task InMemoryEventStoreCache_ShouldInvalidateCache()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var cache = new InMemoryEventStoreCache(memoryCache);
            var aggregateId = Guid.NewGuid();
            var events = new System.Collections.Generic.List<Event>
            {
                new TestEvent { Data = "Test Event" }
            };

            await cache.SetEventsAsync(aggregateId, events);

            // Act
            await cache.InvalidateAsync(aggregateId);
            var retrievedEvents = await cache.GetEventsAsync(aggregateId);

            // Assert
            Assert.Null(retrievedEvents);
        }

        [Fact]
        public async Task InMemoryEventStoreCache_ShouldReturnNullWhenNotCached()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var cache = new InMemoryEventStoreCache(memoryCache);
            var aggregateId = Guid.NewGuid();

            // Act
            var retrievedEvents = await cache.GetEventsAsync(aggregateId);

            // Assert
            Assert.Null(retrievedEvents);
        }

        public class TestEvent : Event
        {
            public string Data { get; set; } = string.Empty;
        }
    }
}