using Microsoft.EntityFrameworkCore;
using Relay.Core.EventSourcing.Infrastructure;
using System;
using System.IO;
using Xunit;

namespace Relay.Core.Tests.EventSourcing.Infrastructure
{
    public class EventStoreDbContextFactoryTests
    {
        [Fact]
        public void CreateDbContext_WithEmptyArgs_CreatesDbContextWithDefaultConnectionString()
        {
            // Arrange
            var factory = new EventStoreDbContextFactory();
            string[] args = new string[0];

            // Act
            var dbContext = factory.CreateDbContext(args);

            // Assert
            Assert.NotNull(dbContext);
            Assert.IsType<EventStoreDbContext>(dbContext);
            
            // The context should be configured with the default connection string
            // We can't directly access the connection string from the DbContext instance,
            // but we can verify it's properly configured by attempting to get the database name
            var databaseName = dbContext.Database.GetConnectionString();
            Assert.NotNull(databaseName);
        }

        [Fact]
        public void CreateDbContext_WithNullArgs_CreatesDbContext()
        {
            // Arrange
            var factory = new EventStoreDbContextFactory();

            // Act
            var dbContext = factory.CreateDbContext(null);

            // Assert
            Assert.NotNull(dbContext);
            Assert.IsType<EventStoreDbContext>(dbContext);
        }

        [Fact]
        public void CreateDbContext_WithArgsContainingValues_CreatesDbContext()
        {
            // Arrange
            var factory = new EventStoreDbContextFactory();
            string[] args = new string[] { "--connection-string", "Host=testhost;Database=testdb;Username=testuser;Password=testpass" };

            // Act
            var dbContext = factory.CreateDbContext(args);

            // Assert
            Assert.NotNull(dbContext);
            Assert.IsType<EventStoreDbContext>(dbContext);
        }

        [Fact]
        public void CreateDbContext_WithMultipleArgs_CreatesDbContext()
        {
            // Arrange
            var factory = new EventStoreDbContextFactory();
            string[] args = new string[] { 
                "--connection-string", "Host=localhost;Database=relay_events;Username=postgres;Password=postgres",
                "--other-option", "value"
            };

            // Act
            var dbContext = factory.CreateDbContext(args);

            // Assert
            Assert.NotNull(dbContext);
            Assert.IsType<EventStoreDbContext>(dbContext);
        }

        [Fact]
        public void CreateDbContext_Implementation_UsesNpgsqlProvider()
        {
            // Arrange
            var factory = new EventStoreDbContextFactory();
            string[] args = new string[0];

            // Act
            var dbContext = factory.CreateDbContext(args);

            // Assert
            // Check that the database provider is configured as expected
            var providerName = dbContext.Database.ProviderName;
            Assert.Contains("Npgsql", providerName);
        }
    }
}