using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Relay.Core.EventSourcing.Infrastructure.Database;

/// <summary>
/// SQL Server database provider implementation.
/// </summary>
public class SqlServerProvider : IDbProvider
{
    private static readonly Type? UseSqlServerMethodType =
        typeof(DbContextOptionsBuilder).Assembly.GetType(
            "Microsoft.EntityFrameworkCore.SqlServerDbContextOptionsExtensions");

    /// <inheritdoc />
    public DatabaseProvider Provider => DatabaseProvider.SqlServer;

    /// <inheritdoc />
    public string ProviderName => "SQL Server";

    /// <inheritdoc />
    public string ProviderType => "Microsoft.EntityFrameworkCore.SqlServer";

    /// <inheritdoc />
    public void Configure(DbContextOptionsBuilder<EventStoreDbContext> optionsBuilder, string connectionString)
    {
        if (UseSqlServerMethodType == null)
        {
            throw new InvalidOperationException(
                "SQL Server EF Core provider is not installed. Install 'Microsoft.EntityFrameworkCore.SqlServer' package.");
        }

        var method = UseSqlServerMethodType.GetMethod(
            "UseSqlServer",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new[] { typeof(DbContextOptionsBuilder), typeof(string) },
            null);

        if (method == null)
        {
            throw new InvalidOperationException(
                "Could not find UseSqlServer extension method. Ensure correct version of 'Microsoft.EntityFrameworkCore.SqlServer' is installed.");
        }

        method.Invoke(null, new object[] { optionsBuilder, connectionString });
    }
}
