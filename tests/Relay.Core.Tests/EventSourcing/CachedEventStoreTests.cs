using System;
using System.Collections.Generic;
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
            Assert.Single(retrievedEvents1);
            Assert.Single(retrievedEvents2);
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
            Assert.Single(retrievedEvents!);
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

        [Fact]
        public void InMemoryEventStoreCache_Constructor_ShouldThrowWhenCacheIsNull()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new InMemoryEventStoreCache(null!));
        }

        [Fact]
        public void InMemoryEventStoreCache_Constructor_ShouldAcceptCustomExpirationMinutes()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var customExpiration = 60;

            // Act
            var cache = new InMemoryEventStoreCache(memoryCache, customExpiration);

            // Assert
            Assert.NotNull(cache);
        }

        [Fact]
        public async Task InMemoryEventStoreCache_SetEventsAsync_ShouldThrowWhenEventsIsNull()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var cache = new InMemoryEventStoreCache(memoryCache);
            var aggregateId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await cache.SetEventsAsync(aggregateId, null!));
        }

        [Fact]
        public async Task InMemoryEventStoreCache_ShouldHandleEmptyEventList()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var cache = new InMemoryEventStoreCache(memoryCache);
            var aggregateId = Guid.NewGuid();
            var events = new List<Event>();

            // Act
            await cache.SetEventsAsync(aggregateId, events);
            var retrievedEvents = await cache.GetEventsAsync(aggregateId);

            // Assert
            Assert.NotNull(retrievedEvents);
            Assert.Empty(retrievedEvents);
        }

        [Fact]
        public async Task InMemoryEventStoreCache_ClearAsync_ShouldRemoveAllEntries()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var cache = new InMemoryEventStoreCache(memoryCache);
            var aggregateId1 = Guid.NewGuid();
            var aggregateId2 = Guid.NewGuid();
            var events1 = new List<Event> { new TestEvent { Data = "Event 1" } };
            var events2 = new List<Event> { new TestEvent { Data = "Event 2" } };

            await cache.SetEventsAsync(aggregateId1, events1);
            await cache.SetEventsAsync(aggregateId2, events2);

            // Verify both are cached
            var retrieved1 = await cache.GetEventsAsync(aggregateId1);
            var retrieved2 = await cache.GetEventsAsync(aggregateId2);
            Assert.NotNull(retrieved1);
            Assert.NotNull(retrieved2);

            // Act
            await cache.ClearAsync();

            // Assert
            var cleared1 = await cache.GetEventsAsync(aggregateId1);
            var cleared2 = await cache.GetEventsAsync(aggregateId2);
            Assert.Null(cleared1);
            Assert.Null(cleared2);
        }

        public class TestEvent : Event
        {
            public string Data { get; set; } = string.Empty;
        }
    }
}