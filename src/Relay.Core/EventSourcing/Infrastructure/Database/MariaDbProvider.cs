using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Relay.Core.EventSourcing.Infrastructure.Database;

/// <summary>
/// MariaDB database provider implementation.
/// Uses the same Pomelo MySQL provider but optimized for MariaDB compatibility.
/// </summary>
public class MariaDbProvider : IDbProvider
{
    private static readonly Type? UseMySqlMethodType =
        typeof(DbContextOptionsBuilder).Assembly.GetType(
            "Pomelo.EntityFrameworkCore.MySql.Infrastructure.MySqlDbContextOptionsExtensions");

    /// <inheritdoc />
    public DatabaseProvider Provider => DatabaseProvider.MariaDB;

    /// <inheritdoc />
    public string ProviderName => "MariaDB";

    /// <inheritdoc />
    public string ProviderType => "Pomelo.EntityFrameworkCore.MySql";

    /// <inheritdoc />
    public void Configure(DbContextOptionsBuilder<EventStoreDbContext> optionsBuilder, string connectionString)
    {
        if (UseMySqlMethodType == null)
        {
            throw new InvalidOperationException(
                "MariaDB EF Core provider is not installed. Install 'Pomelo.EntityFrameworkCore.MySql' package.");
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
                        // Fallback: assume MariaDB 10.3
                        serverVersion = versionConstructor.Invoke(null, new object?[] { new Version(10, 3) });
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
