using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Relay.Core.EventSourcing.Infrastructure.Database;

/// <summary>
/// SQLite database provider implementation.
/// </summary>
public class SqliteProvider : IDbProvider
{
    private static readonly Type? UseSqliteMethodType =
        typeof(DbContextOptionsBuilder).Assembly.GetType(
            "Microsoft.EntityFrameworkCore.SqliteServiceCollectionExtensions");

    /// <inheritdoc />
    public DatabaseProvider Provider => DatabaseProvider.Sqlite;

    /// <inheritdoc />
    public string ProviderName => "SQLite";

    /// <inheritdoc />
    public string ProviderType => "Microsoft.EntityFrameworkCore.Sqlite";

    /// <inheritdoc />
    public void Configure(DbContextOptionsBuilder<EventStoreDbContext> optionsBuilder, string connectionString)
    {
        var sqliteAssembly = Type.GetType(
            "Microsoft.EntityFrameworkCore.Sqlite.SqliteDbContextOptionsBuilder, Microsoft.EntityFrameworkCore.Sqlite");

        if (sqliteAssembly == null)
        {
            throw new InvalidOperationException(
                "SQLite EF Core provider is not installed. Install 'Microsoft.EntityFrameworkCore.Sqlite' package.");
        }

        var method = typeof(DbContextOptionsBuilder).Assembly
            .GetType("Microsoft.EntityFrameworkCore.SqliteDbContextOptionsExtensions")
            ?.GetMethod(
                "UseSqlite",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[] { typeof(DbContextOptionsBuilder), typeof(string) },
                null);

        if (method == null)
        {
            throw new InvalidOperationException(
                "Could not find UseSqlite extension method. Ensure correct version of 'Microsoft.EntityFrameworkCore.Sqlite' is installed.");
        }

        method.Invoke(null, new object[] { optionsBuilder, connectionString });
    }
}
