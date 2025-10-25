using Microsoft.EntityFrameworkCore;

namespace Relay.Core.EventSourcing.Infrastructure.Database;

/// <summary>
/// PostgreSQL database provider implementation.
/// </summary>
public class PostgreSqlProvider : IDbProvider
{
    /// <inheritdoc />
    public DatabaseProvider Provider => DatabaseProvider.PostgreSQL;

    /// <inheritdoc />
    public string ProviderName => "PostgreSQL";

    /// <inheritdoc />
    public string ProviderType => "Npgsql.EntityFrameworkCore.PostgreSQL";

    /// <inheritdoc />
    public void Configure(DbContextOptionsBuilder<EventStoreDbContext> optionsBuilder, string connectionString)
    {
        optionsBuilder.UseNpgsql(connectionString);
    }
}
