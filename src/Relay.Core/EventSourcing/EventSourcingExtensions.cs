using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Relay.Core.EventSourcing;

/// <summary>
/// Extension methods for configuring event sourcing with EF Core.
/// </summary>
public static class EventSourcingExtensions
{
    /// <summary>
    /// Adds EF Core event store services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEfCoreEventStore(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<EventStoreDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IEventStore, EfCoreEventStore>();

        return services;
    }

    /// <summary>
    /// Adds EF Core event store services to the service collection with custom options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="optionsAction">Action to configure the DbContext options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEfCoreEventStore(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
    {
        services.AddDbContext<EventStoreDbContext>(optionsAction);
        services.AddScoped<IEventStore, EfCoreEventStore>();

        return services;
    }

    /// <summary>
    /// Ensures the event store database is created and migrated.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task EnsureEventStoreDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();
        
        // Check if using a relational database provider
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            // For non-relational providers (like InMemory), just ensure it's created
            await context.Database.EnsureCreatedAsync();
        }
    }
}
