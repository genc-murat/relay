using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.EventSourcing.Core;
using Relay.Core.EventSourcing.Infrastructure;
using Relay.Core.EventSourcing.Infrastructure.Database;

namespace Relay.Core.EventSourcing.Extensions;

/// <summary>
/// Extension methods for configuring event sourcing with EF Core.
/// Supports multiple database providers (PostgreSQL, SQL Server, SQLite).
/// </summary>
public static class EventSourcingExtensions
{
    /// <summary>
    /// Adds EF Core event store services to the service collection with automatic provider detection.
    /// Detects the database provider based on the connection string format.
    /// Falls back to PostgreSQL if provider cannot be detected from the connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEfCoreEventStore(
        this IServiceCollection services,
        string connectionString)
    {
        IDbProvider provider;
        try
        {
            provider = DbProviderFactory.CreateProviderFromConnectionString(connectionString);
        }
        catch (ArgumentException)
        {
            // If provider cannot be detected from connection string, fall back to PostgreSQL
            provider = DbProviderFactory.CreateProvider(DatabaseProvider.PostgreSQL);
        }

        return AddEfCoreEventStore(services, provider, connectionString);
    }

    /// <summary>
    /// Adds EF Core event store services to the service collection with explicit provider specification (enum).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="provider">The database provider enumeration (DatabaseProvider.PostgreSQL, etc.).</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEfCoreEventStore(
        this IServiceCollection services,
        DatabaseProvider provider,
        string connectionString)
    {
        var dbProvider = DbProviderFactory.CreateProvider(provider);
        return AddEfCoreEventStore(services, dbProvider, connectionString);
    }

    /// <summary>
    /// Adds EF Core event store services to the service collection with explicit provider specification (string).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="providerType">The database provider type (PostgreSQL, SqlServer, Sqlite, MySQL, MariaDB).</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when provider type is not supported.</exception>
    public static IServiceCollection AddEfCoreEventStore(
        this IServiceCollection services,
        string providerType,
        string connectionString)
    {
        var provider = DbProviderFactory.CreateProvider(providerType);
        return AddEfCoreEventStore(services, provider, connectionString);
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
    /// Adds EF Core event store services with a specific database provider.
    /// </summary>
    private static IServiceCollection AddEfCoreEventStore(
        this IServiceCollection services,
        IDbProvider provider,
        string connectionString)
    {
        services.AddDbContext<EventStoreDbContext>(options =>
            provider.Configure((DbContextOptionsBuilder<EventStoreDbContext>)(object)options, connectionString));

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
