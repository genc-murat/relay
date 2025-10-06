using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Relay.Core.EventSourcing;

/// <summary>
/// Design-time factory for EventStoreDbContext.
/// This is used by EF Core tools for migrations.
/// </summary>
public class EventStoreDbContextFactory : IDesignTimeDbContextFactory<EventStoreDbContext>
{
    /// <summary>
    /// Creates a new instance of EventStoreDbContext for design-time operations.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>A new EventStoreDbContext instance.</returns>
    public EventStoreDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EventStoreDbContext>();
        
        // Use a default connection string for migrations
        // This will be overridden in actual application with the real connection string
        optionsBuilder.UseNpgsql("Host=localhost;Database=relay_events;Username=postgres;Password=postgres");

        return new EventStoreDbContext(optionsBuilder.Options);
    }
}
