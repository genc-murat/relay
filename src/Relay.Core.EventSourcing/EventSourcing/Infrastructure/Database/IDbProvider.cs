using Microsoft.EntityFrameworkCore;

namespace Relay.Core.EventSourcing.Infrastructure.Database;

/// <summary>
/// Abstraction for database provider configuration.
/// Allows EventStoreDbContext to support multiple database providers.
/// </summary>
public interface IDbProvider
{
    /// <summary>
    /// Gets the database provider type enumeration.
    /// </summary>
    DatabaseProvider Provider { get; }

    /// <summary>
    /// Gets the name of the database provider (e.g., "PostgreSQL", "SQL Server", "SQLite").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets the provider type (e.g., "Npgsql.EntityFrameworkCore.PostgreSQL").
    /// </summary>
    string ProviderType { get; }

    /// <summary>
    /// Configures the DbContextOptionsBuilder with the appropriate provider.
    /// </summary>
    /// <param name="optionsBuilder">The options builder to configure.</param>
    /// <param name="connectionString">The connection string for the database.</param>
    void Configure(DbContextOptionsBuilder<EventStoreDbContext> optionsBuilder, string connectionString);
}
