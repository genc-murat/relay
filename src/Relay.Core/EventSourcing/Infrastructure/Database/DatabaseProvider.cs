namespace Relay.Core.EventSourcing.Infrastructure.Database;

/// <summary>
/// Enumeration of supported database providers.
/// </summary>
public enum DatabaseProvider
{
    /// <summary>
    /// PostgreSQL database provider (Npgsql).
    /// </summary>
    PostgreSQL,

    /// <summary>
    /// MySQL database provider (Pomelo).
    /// </summary>
    MySQL,

    /// <summary>
    /// MariaDB database provider (Pomelo - compatible with MySQL provider).
    /// </summary>
    MariaDB,

    /// <summary>
    /// SQL Server database provider.
    /// </summary>
    SqlServer,

    /// <summary>
    /// SQLite database provider.
    /// </summary>
    Sqlite
}
