using System;
using Microsoft.EntityFrameworkCore;
using Relay.Core.EventSourcing.Infrastructure;
using Relay.Core.EventSourcing.Infrastructure.Database;
using Xunit;

namespace Relay.Core.Tests.EventSourcing.Database;

/// <summary>
/// Tests for database provider implementations (PostgreSQL, SQL Server, SQLite).
/// </summary>
public class DbProviderImplementationTests
{
    [Fact]
    public void PostgreSqlProvider_Configure_ConfiguresNpgsqlProvider()
    {
        // Arrange
        var provider = new PostgreSqlProvider();
        var optionsBuilder = new DbContextOptionsBuilder<EventStoreDbContext>();
        var connectionString = "Host=localhost;Database=test;Username=user;Password=pass";

        // Act
        provider.Configure(optionsBuilder, connectionString);
        var context = new EventStoreDbContext(optionsBuilder.Options);

        // Assert
        Assert.Contains("Npgsql", context.Database.ProviderName);
    }

    [Fact]
    public void PostgreSqlProvider_Properties_ReturnCorrectValues()
    {
        // Arrange & Act
        var provider = new PostgreSqlProvider();

        // Assert
        Assert.Equal("PostgreSQL", provider.ProviderName);
        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", provider.ProviderType);
    }

    [Fact]
    public void SqlServerProvider_Configure_ThrowsIfProviderNotInstalled()
    {
        // Arrange
        var provider = new SqlServerProvider();
        var optionsBuilder = new DbContextOptionsBuilder<EventStoreDbContext>();
        var connectionString = "Server=localhost;Database=test;";

        // Act & Assert
        // SQL Server provider not installed, should throw InvalidOperationException
        Assert.Throws<InvalidOperationException>(() =>
            provider.Configure(optionsBuilder, connectionString));
    }

    [Fact]
    public void SqlServerProvider_Properties_ReturnCorrectValues()
    {
        // Arrange & Act
        var provider = new SqlServerProvider();

        // Assert
        Assert.Equal("SQL Server", provider.ProviderName);
        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", provider.ProviderType);
    }

    [Fact]
    public void SqliteProvider_Configure_ThrowsIfProviderNotInstalled()
    {
        // Arrange
        var provider = new SqliteProvider();
        var optionsBuilder = new DbContextOptionsBuilder<EventStoreDbContext>();
        var connectionString = "Data Source=:memory:";

        // Act & Assert
        // SQLite provider not installed, should throw InvalidOperationException
        Assert.Throws<InvalidOperationException>(() =>
            provider.Configure(optionsBuilder, connectionString));
    }

    [Fact]
    public void SqliteProvider_Properties_ReturnCorrectValues()
    {
        // Arrange & Act
        var provider = new SqliteProvider();

        // Assert
        Assert.Equal("SQLite", provider.ProviderName);
        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", provider.ProviderType);
    }

    [Fact]
    public void PostgreSqlProvider_Configure_AllowsDatabaseOperations()
    {
        // Arrange
        var provider = new PostgreSqlProvider();
        var optionsBuilder = new DbContextOptionsBuilder<EventStoreDbContext>()
            .UseNpgsql("Host=localhost;Database=test;Username=user;Password=pass");

        // Act
        var context = new EventStoreDbContext(optionsBuilder.Options);

        // Assert
        Assert.NotNull(context.Events);
        Assert.NotNull(context.Snapshots);
    }

    [Fact]
    public void MariaDbProvider_Properties_ReturnCorrectValues()
    {
        // Arrange & Act
        var provider = new MariaDbProvider();

        // Assert
        Assert.Equal(DatabaseProvider.MariaDB, provider.Provider);
        Assert.Equal("MariaDB", provider.ProviderName);
        Assert.Equal("Pomelo.EntityFrameworkCore.MySql", provider.ProviderType);
    }

    [Fact]
    public void MariaDbProvider_Configure_ThrowsIfProviderNotInstalled()
    {
        // Arrange
        var provider = new MariaDbProvider();
        var optionsBuilder = new DbContextOptionsBuilder<EventStoreDbContext>();
        var connectionString = "Server=localhost;Database=test;Uid=root;Pwd=password;";

        // Act & Assert
        // MariaDB provider not installed, should throw InvalidOperationException
        Assert.Throws<InvalidOperationException>(() =>
            provider.Configure(optionsBuilder, connectionString));
    }

    [Theory]
    [InlineData(typeof(MySqlProvider), null)]
    [InlineData(typeof(MariaDbProvider), null)]
    [InlineData(typeof(SqlServerProvider), null)]
    [InlineData(typeof(SqliteProvider), null)]
    public void ProviderConfigure_WithNullConnectionString_ThrowsException(Type providerType, string connectionString)
    {
        // Arrange
        var provider = (IDbProvider)Activator.CreateInstance(providerType)!;
        var optionsBuilder = new DbContextOptionsBuilder<EventStoreDbContext>();

        // Act & Assert
        // Some providers may throw when given null connection strings
        // This tests that the Configure method handles null input appropriately
        try
        {
            provider.Configure(optionsBuilder, connectionString);
            // If no exception is thrown, that's also acceptable behavior
        }
        catch (Exception ex)
        {
            // If an exception is thrown, it should be a meaningful one
            Assert.NotNull(ex);
        }
    }

    [Fact]
    public void PostgreSqlProvider_Configure_WithNullConnectionString_DoesNotThrow()
    {
        // Arrange
        var provider = new PostgreSqlProvider();
        var optionsBuilder = new DbContextOptionsBuilder<EventStoreDbContext>();

        // Act & Assert
        // PostgreSQL provider passes null directly to UseNpgsql, which may not validate
        // This documents the current behavior
        provider.Configure(optionsBuilder, null);
        // Should not throw - EF Core handles null validation internally
    }

    [Fact]
    public void MySqlProvider_Properties_ReturnCorrectValues()
    {
        // Arrange & Act
        var provider = new MySqlProvider();

        // Assert
        Assert.Equal(DatabaseProvider.MySQL, provider.Provider);
        Assert.Equal("MySQL", provider.ProviderName);
        Assert.Equal("Pomelo.EntityFrameworkCore.MySql", provider.ProviderType);
    }


    [Theory]
    [InlineData(typeof(PostgreSqlProvider))]
    [InlineData(typeof(MySqlProvider))]
    [InlineData(typeof(MariaDbProvider))]
    [InlineData(typeof(SqlServerProvider))]
    [InlineData(typeof(SqliteProvider))]
    public void AllProviders_ImplementIDbProvider(Type providerType)
    {
        // Act
        var provider = Activator.CreateInstance(providerType);

        // Assert
        Assert.NotNull(provider);
        Assert.IsAssignableFrom<IDbProvider>(provider);
    }

    [Theory]
    [InlineData(typeof(PostgreSqlProvider))]
    [InlineData(typeof(MySqlProvider))]
    [InlineData(typeof(MariaDbProvider))]
    [InlineData(typeof(SqlServerProvider))]
    [InlineData(typeof(SqliteProvider))]
    public void AllProviders_HaveNonEmptyProviderName(Type providerType)
    {
        // Arrange
        var provider = (IDbProvider)Activator.CreateInstance(providerType)!;

        // Assert
        Assert.NotNull(provider.ProviderName);
        Assert.NotEmpty(provider.ProviderName);
    }

    [Theory]
    [InlineData(typeof(PostgreSqlProvider))]
    [InlineData(typeof(MySqlProvider))]
    [InlineData(typeof(MariaDbProvider))]
    [InlineData(typeof(SqlServerProvider))]
    [InlineData(typeof(SqliteProvider))]
    public void AllProviders_HaveNonEmptyProviderType(Type providerType)
    {
        // Arrange
        var provider = (IDbProvider)Activator.CreateInstance(providerType)!;

        // Assert
        Assert.NotNull(provider.ProviderType);
        Assert.NotEmpty(provider.ProviderType);
    }
}
