using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Relay.Core.EventSourcing.Infrastructure.Database;

/// <summary>
/// MySQL database provider implementation.
/// Supports both MySQL (via Pomelo.EntityFrameworkCore.MySql) and MariaDB.
/// </summary>
public class MySqlProvider : IDbProvider
{
    private static readonly Type? UseMySqlMethodType =
        typeof(DbContextOptionsBuilder).Assembly.GetType(
            "Pomelo.EntityFrameworkCore.MySql.Infrastructure.MySqlDbContextOptionsExtensions");

    /// <inheritdoc />
    public DatabaseProvider Provider => DatabaseProvider.MySQL;

    /// <inheritdoc />
    public string ProviderName => "MySQL";

    /// <inheritdoc />
    public string ProviderType => "Pomelo.EntityFrameworkCore.MySql";

    /// <inheritdoc />
    public void Configure(DbContextOptionsBuilder<EventStoreDbContext> optionsBuilder, string connectionString)
    {
        if (UseMySqlMethodType == null)
        {
            throw new InvalidOperationException(
                "MySQL EF Core provider is not installed. Install 'Pomelo.EntityFrameworkCore.MySql' package.");
        }

        var method = UseMySqlMethodType.GetMethod(
            "UseMySql",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new[] { typeof(DbContextOptionsBuilder), typeof(string), typeof(object) },
            null);

        if (method == null)
        {
            // Fallback to version without ServerVersion parameter
            method = UseMySqlMethodType.GetMethod(
                "UseMySql",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[] { typeof(DbContextOptionsBuilder), typeof(string) },
                null);

            if (method == null)
            {
                throw new InvalidOperationException(
                    "Could not find UseMySql extension method. Ensure correct version of 'Pomelo.EntityFrameworkCore.MySql' is installed.");
            }

            method.Invoke(null, new object[] { optionsBuilder, connectionString });
        }
        else
        {
            // Use version with ServerVersion parameter (newer versions)
            // Create a ServerVersion instance using reflection
            var serverVersionType = UseMySqlMethodType.Assembly.GetType(
                "Pomelo.EntityFrameworkCore.MySql.Infrastructure.ServerVersion");

            if (serverVersionType != null)
            {
                var versionConstructor = serverVersionType.GetConstructor(
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[] { typeof(Version) },
                    null);

                if (versionConstructor != null)
                {
                    // Get the latest auto-detected version
                    var autoVersionProperty = serverVersionType.GetProperty("AutoDetect",
                        BindingFlags.Static | BindingFlags.Public);

                    object? serverVersion = null;

                    if (autoVersionProperty?.GetValue(null) is object autoVersion)
                    {
                        serverVersion = autoVersion;
                    }
                    else
                    {
                        // Fallback: assume MySQL 8.0
                        serverVersion = versionConstructor.Invoke(null, new object?[] { new Version(8, 0) });
                    }

                    if (serverVersion != null)
                    {
                        method.Invoke(null, new[] { optionsBuilder, connectionString, serverVersion });
                    }
                }
            }
            else
            {
                method.Invoke(null, new object[] { optionsBuilder, connectionString });
            }
        }
    }
}
