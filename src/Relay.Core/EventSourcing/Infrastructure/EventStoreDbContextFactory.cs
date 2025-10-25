using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Relay.Core.EventSourcing.Infrastructure.Database;

namespace Relay.Core.EventSourcing.Infrastructure;

/// <summary>
/// Design-time factory for EventStoreDbContext.
/// This is used by EF Core tools for migrations.
/// Supports multiple database providers (PostgreSQL, SQL Server, SQLite).
/// </summary>
public class EventStoreDbContextFactory : IDesignTimeDbContextFactory<EventStoreDbContext>
{
    /// <summary>
    /// Environment variable name for specifying database provider.
    /// </summary>
    private const string DbProviderEnvVar = "EVENTSTORE_DB_PROVIDER";

    /// <summary>
    /// Environment variable name for database connection string.
    /// </summary>
    private const string ConnectionStringEnvVar = "EVENTSTORE_CONNECTION_STRING";

    /// <summary>
    /// Creates a new instance of EventStoreDbContext for design-time operations.
    /// Reads configuration from command-line arguments or environment variables.
    /// </summary>
    /// <param name="args">
    /// Command line arguments. Supports:
    ///   --provider=&lt;ProviderName&gt;     Database provider (PostgreSQL, SqlServer, Sqlite)
    ///   --connection=&lt;ConnectionString&gt; Database connection string
    /// </param>
    /// <returns>A new EventStoreDbContext instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no valid configuration is available.</exception>
    public EventStoreDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EventStoreDbContext>();

        // Parse command-line arguments
        var provider = ParseArgument(args, "provider");
        var connectionString = ParseArgument(args, "connection");

        // Fall back to environment variables
        provider ??= Environment.GetEnvironmentVariable(DbProviderEnvVar);
        connectionString ??= Environment.GetEnvironmentVariable(ConnectionStringEnvVar);

        // Use defaults if still not set
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = GetDefaultConnectionString();
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            var dbProvider = DbProviderFactory.CreateProviderFromConnectionString(connectionString);
            ConfigureDbContext(optionsBuilder, dbProvider, connectionString);
        }
        else
        {
            var dbProvider = DbProviderFactory.CreateProvider(provider);
            ConfigureDbContext(optionsBuilder, dbProvider, connectionString);
        }

        return new EventStoreDbContext(optionsBuilder.Options);
    }

    /// <summary>
    /// Configures the DbContextOptionsBuilder with the specified provider and connection string.
    /// </summary>
    private static void ConfigureDbContext(
        DbContextOptionsBuilder<EventStoreDbContext> optionsBuilder,
        IDbProvider dbProvider,
        string connectionString)
    {
        dbProvider.Configure(optionsBuilder, connectionString);
    }

    /// <summary>
    /// Parses a command-line argument by name.
    /// </summary>
    private static string? ParseArgument(string[] args, string argumentName)
    {
        if (args == null || args.Length == 0)
        {
            return null;
        }

        var prefix = $"--{argumentName}=";
        var argument = args.FirstOrDefault(a =>
            a.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        return argument?.Substring(prefix.Length);
    }

    /// <summary>
    /// Gets the default connection string for design-time operations.
    /// This uses PostgreSQL with localhost default.
    /// </summary>
    private static string GetDefaultConnectionString()
    {
        // Default to PostgreSQL with localhost
        return "Host=localhost;Database=relay_events;Username=postgres;Password=postgres";
    }
}
