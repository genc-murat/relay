using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.EventSourcing;
using Relay.Core.EventSourcing.Core;
using Relay.Core.EventSourcing.Extensions;
using Relay.Core.EventSourcing.Infrastructure;
using Relay.Core.EventSourcing.Infrastructure.Database;
using Xunit;

namespace Relay.Core.EventSourcing.Tests
{
    public class EventSourcingExtensionsTests
    {
        [Fact]
        public void AddEfCoreEventStore_WithConnectionString_ShouldRegisterServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var connectionString = "Host=localhost;Database=test;Username=test;Password=test";

            // Act
            services.AddEfCoreEventStore(connectionString);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var eventStore = serviceProvider.GetService<IEventStore>();
            var dbContext = serviceProvider.GetService<EventStoreDbContext>();

            Assert.NotNull(eventStore);
            Assert.IsType<EfCoreEventStore>(eventStore);
            Assert.NotNull(dbContext);
        }

        [Fact]
        public void AddEfCoreEventStore_WithOptionsAction_ShouldRegisterServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddEfCoreEventStore(options =>
                options.UseInMemoryDatabase("test-db"));
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var eventStore = serviceProvider.GetService<IEventStore>();
            var dbContext = serviceProvider.GetService<EventStoreDbContext>();

            Assert.NotNull(eventStore);
            Assert.IsType<EfCoreEventStore>(eventStore);
            Assert.NotNull(dbContext);
        }

        [Fact]
        public void AddEfCoreEventStore_ShouldRegisterServicesAsScoped()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddEfCoreEventStore(options =>
                options.UseInMemoryDatabase("test-db"));

            // Act
            var serviceProvider = services.BuildServiceProvider();
            var scope1 = serviceProvider.CreateScope();
            var scope2 = serviceProvider.CreateScope();

            var eventStore1 = scope1.ServiceProvider.GetService<IEventStore>();
            var eventStore2 = scope2.ServiceProvider.GetService<IEventStore>();

            // Assert
            Assert.NotSame(eventStore1, eventStore2);
        }

        [Fact]
        public void AddEfCoreEventStore_ShouldAllowMultipleCalls()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddEfCoreEventStore(options =>
                options.UseInMemoryDatabase("test-db-1"));
            services.AddEfCoreEventStore(options =>
                options.UseInMemoryDatabase("test-db-2"));

            // Assert
            // Should not throw exception
            var serviceProvider = services.BuildServiceProvider();
            var eventStore = serviceProvider.GetService<IEventStore>();
            Assert.NotNull(eventStore);
        }

        [Fact]
        public async Task EnsureEventStoreDatabaseAsync_ShouldCreateDatabase()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddEfCoreEventStore(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

            var serviceProvider = services.BuildServiceProvider();

            // Act
            await serviceProvider.EnsureEventStoreDatabaseAsync();

            // Assert
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();
            var canConnect = await context.Database.CanConnectAsync();
            Assert.True(canConnect);
        }

        [Fact]
        public async Task EnsureEventStoreDatabaseAsync_ShouldBeIdempotent()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddEfCoreEventStore(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

            var serviceProvider = services.BuildServiceProvider();

            // Act
            await serviceProvider.EnsureEventStoreDatabaseAsync();
            await serviceProvider.EnsureEventStoreDatabaseAsync(); // Call twice

            // Assert - Should not throw exception
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();
            var canConnect = await context.Database.CanConnectAsync();
            Assert.True(canConnect);
        }

        [Fact]
        public async Task IntegrationTest_ShouldWorkEndToEnd()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddEfCoreEventStore(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

            var serviceProvider = services.BuildServiceProvider();
            await serviceProvider.EnsureEventStoreDatabaseAsync();

            // Act
            using var scope = serviceProvider.CreateScope();
            var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

            var aggregateId = Guid.NewGuid();
            var events = new[]
            {
                new TestAggregateCreated
                {
                    AggregateId = aggregateId,
                    AggregateName = "Test",
                    AggregateVersion = 0
                }
            };

            await eventStore.SaveEventsAsync(aggregateId, events, -1);

            // Assert
            var retrievedEvents = new System.Collections.Generic.List<Event>();
            await foreach (var @event in eventStore.GetEventsAsync(aggregateId))
            {
                retrievedEvents.Add(@event);
            }

            Assert.Single(retrievedEvents);
            Assert.IsType<TestAggregateCreated>(retrievedEvents[0]);
        }

        [Fact]
        public void AddEfCoreEventStore_WithInvalidConnectionString_ShouldStillRegister()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act - Should not throw at registration time
            services.AddEfCoreEventStore("InvalidConnectionString");
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var eventStore = serviceProvider.GetService<IEventStore>();
            Assert.NotNull(eventStore);
        }

        [Fact]
        public async Task EnsureEventStoreDatabaseAsync_WithMultipleScopes_ShouldWork()
        {
            // Arrange
            var databaseName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddEfCoreEventStore(options =>
                options.UseInMemoryDatabase(databaseName));

            var serviceProvider = services.BuildServiceProvider();
            await serviceProvider.EnsureEventStoreDatabaseAsync();

            // Act - Create multiple scopes and verify they all work
            var aggregateId = Guid.NewGuid();
            var events = new[]
            {
                new TestAggregateCreated
                {
                    AggregateId = aggregateId,
                    AggregateName = "Test",
                    AggregateVersion = 0
                }
            };

            // Save in first scope
            using (var scope1 = serviceProvider.CreateScope())
            {
                var eventStore1 = scope1.ServiceProvider.GetRequiredService<IEventStore>();
                await eventStore1.SaveEventsAsync(aggregateId, events, -1);
            }

            // Retrieve in second scope
            using var scope2 = serviceProvider.CreateScope();
            var eventStore2 = scope2.ServiceProvider.GetRequiredService<IEventStore>();

            var retrievedEvents = new System.Collections.Generic.List<Event>();
            await foreach (var @event in eventStore2.GetEventsAsync(aggregateId))
            {
                retrievedEvents.Add(@event);
            }

            // Assert - InMemory database persists across scopes with same database name
            Assert.Single(retrievedEvents);
        }

        [Fact]
        public void AddEfCoreEventStore_WithDatabaseProviderEnum_ShouldRegisterServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var connectionString = "Host=localhost;Database=test;Username=test;Password=test";

            // Act
            services.AddEfCoreEventStore(DatabaseProvider.PostgreSQL, connectionString);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var eventStore = serviceProvider.GetService<IEventStore>();
            var dbContext = serviceProvider.GetService<EventStoreDbContext>();

            Assert.NotNull(eventStore);
            Assert.IsType<EfCoreEventStore>(eventStore);
            Assert.NotNull(dbContext);
        }

        [Fact]
        public void AddEfCoreEventStore_WithProviderString_ShouldRegisterServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var connectionString = "Host=localhost;Database=test;Username=test;Password=test";

            // Act
            services.AddEfCoreEventStore("PostgreSQL", connectionString);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var eventStore = serviceProvider.GetService<IEventStore>();
            var dbContext = serviceProvider.GetService<EventStoreDbContext>();

            Assert.NotNull(eventStore);
            Assert.IsType<EfCoreEventStore>(eventStore);
            Assert.NotNull(dbContext);
        }

        [Fact]
        public void AddEfCoreEventStore_WithInvalidProviderString_ShouldThrowArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var connectionString = "Host=localhost;Database=test;Username=test;Password=test";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                services.AddEfCoreEventStore("InvalidProvider", connectionString));

            Assert.Contains("Unsupported database provider", exception.Message);
        }
    }
}
