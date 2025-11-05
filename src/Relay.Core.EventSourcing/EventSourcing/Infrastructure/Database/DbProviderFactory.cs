using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.EventSourcing.Infrastructure.Database;

/// <summary>
/// Factory for creating database provider instances.
/// </summary>
public static class DbProviderFactory
{
    /// <summary>
    /// Creates a database provider instance based on the provider enum.
    /// </summary>
    /// <param name="provider">The database provider enumeration.</param>
    /// <returns>An IDbProvider instance.</returns>
    public static IDbProvider CreateProvider(DatabaseProvider provider)
    {
        return provider switch
        {
            DatabaseProvider.PostgreSQL => new PostgreSqlProvider(),
            DatabaseProvider.MySQL => new MySqlProvider(),
            DatabaseProvider.MariaDB => new MariaDbProvider(),
            DatabaseProvider.SqlServer => new SqlServerProvider(),
            DatabaseProvider.Sqlite => new SqliteProvider(),
            _ => throw new ArgumentException($"Unsupported database provider: {provider}", nameof(provider))
        };
    }

    /// <summary>
    /// Creates a database provider instance based on the provider type string.
    /// </summary>
    /// <param name="providerType">The provider type (e.g., "PostgreSQL", "SqlServer", "Sqlite").</param>
    /// <returns>An IDbProvider instance.</returns>
    /// <exception cref="ArgumentException">Thrown when provider type is not supported.</exception>
    public static IDbProvider CreateProvider(string providerType)
    {
        return providerType?.ToLowerInvariant() switch
        {
            "postgresql" or "postgres" or "npgsql" or "npgsql.entityframeworkcore.postgresql" => new PostgreSqlProvider(),
            "sqlserver" or "mssql" or "sql server" or "microsoft.entityframeworkcore.sqlserver" => new SqlServerProvider(),
            "sqlite" or "microsoft.entityframeworkcore.sqlite" => new SqliteProvider(),
            "mysql" or "mariadb" or "pomelo.entityframeworkcore.mysql" => new MySqlProvider(),
            _ => throw new ArgumentException($"Unsupported database provider: '{providerType}'", nameof(providerType))
        };
    }

    /// <summary>
    /// Creates a database provider instance from a connection string by detecting the provider type.
    /// </summary>
    /// <param name="connectionString">The connection string to analyze.</param>
    /// <returns>An IDbProvider instance.</returns>
    /// <exception cref="ArgumentException">Thrown when provider cannot be detected from connection string.</exception>
    public static IDbProvider CreateProviderFromConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
        }

        // Detect provider based on connection string patterns

        // PostgreSQL detection (most specific patterns first)
        if (connectionString.Contains("Server=postgresql", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Port=5432", StringComparison.OrdinalIgnoreCase))
        {
            return new PostgreSqlProvider();
        }

        // MySQL/MariaDB detection (Uid is MySQL specific, also Database= not Initial Catalog=)
        if (connectionString.Contains("Uid=", StringComparison.OrdinalIgnoreCase) &&
            (connectionString.Contains("Pwd=", StringComparison.OrdinalIgnoreCase) ||
             connectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase)) &&
            connectionString.Contains("Database=", StringComparison.OrdinalIgnoreCase))
        {
            return new MySqlProvider();
        }

        // MySQL/MariaDB detection (Port=3306 is MySQL specific)
        if (connectionString.Contains("Port=3306", StringComparison.OrdinalIgnoreCase))
        {
            return new MySqlProvider();
        }

        // PostgreSQL with Host= (must come after MySQL Uid/Pwd check)
        if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
        {
            return new PostgreSqlProvider();
        }

        // SQL Server detection (Integrated Security, Trusted_Connection, or typical SQL Server patterns)
        if (connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) &&
            (connectionString.Contains("Integrated Security=", StringComparison.OrdinalIgnoreCase) ||
             connectionString.Contains("Trusted_Connection=", StringComparison.OrdinalIgnoreCase) ||
             connectionString.Contains("User Id=", StringComparison.OrdinalIgnoreCase)))
        {
            return new SqlServerProvider();
        }

        // SQL Server detection (Server= without Data Source)
        if (connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
        {
            return new SqlServerProvider();
        }

        // SQLite detection
        if (connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) ||
            connectionString.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
        {
            return new SqliteProvider();
        }

        throw new ArgumentException(
            $"Could not detect database provider from connection string: '{connectionString}'",
            nameof(connectionString));
    }

    /// <summary>
    /// Gets all supported provider types.
    /// </summary>
    /// <returns>An enumerable of supported provider type names.</returns>
    public static IEnumerable<string> GetSupportedProviders()
    {
        return new[]
        {
            "PostgreSQL",
            "SqlServer",
            "Sqlite",
            "MySQL",
            "MariaDB"
        };
    }
}
