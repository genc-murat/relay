using System;
using System.Linq;
using Relay.Core.EventSourcing.Infrastructure.Database;
using Xunit;

namespace Relay.Core.Tests.EventSourcing.Database;

/// <summary>
/// Unit tests for DbProviderFactory.
/// </summary>
public class DbProviderFactoryTests
{
    [Theory]
    [InlineData("postgresql")]
    [InlineData("PostgreSQL")]
    [InlineData("postgres")]
    [InlineData("Postgres")]
    [InlineData("npgsql")]
    [InlineData("Npgsql.EntityFrameworkCore.PostgreSQL")]
    public void CreateProvider_WithPostgreSqlProvider_ReturnsPostgreSqlProvider(string providerType)
    {
        // Act
        var provider = DbProviderFactory.CreateProvider(providerType);

        // Assert
        Assert.IsType<PostgreSqlProvider>(provider);
        Assert.Equal("PostgreSQL", provider.ProviderName);
    }

    [Theory]
    [InlineData("sqlserver")]
    [InlineData("SqlServer")]
    [InlineData("mssql")]
    [InlineData("sql server")]
    [InlineData("SQL Server")]
    [InlineData("Microsoft.EntityFrameworkCore.SqlServer")]
    public void CreateProvider_WithSqlServerProvider_ReturnsSqlServerProvider(string providerType)
    {
        // Act
        var provider = DbProviderFactory.CreateProvider(providerType);

        // Assert
        Assert.IsType<SqlServerProvider>(provider);
        Assert.Equal("SQL Server", provider.ProviderName);
    }

    [Theory]
    [InlineData("sqlite")]
    [InlineData("SQLite")]
    [InlineData("Sqlite")]
    [InlineData("Microsoft.EntityFrameworkCore.Sqlite")]
    public void CreateProvider_WithSqliteProvider_ReturnsSqliteProvider(string providerType)
    {
        // Act
        var provider = DbProviderFactory.CreateProvider(providerType);

        // Assert
        Assert.IsType<SqliteProvider>(provider);
        Assert.Equal("SQLite", provider.ProviderName);
    }

    [Fact]
    public void CreateProvider_WithInvalidProvider_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => DbProviderFactory.CreateProvider("InvalidProvider"));

        Assert.Contains("Unsupported database provider", exception.Message);
    }

    [Fact]
    public void CreateProvider_WithNullProvider_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => DbProviderFactory.CreateProvider(null!));

        Assert.Contains("Unsupported database provider", exception.Message);
    }

    [Theory]
    [InlineData("Host=localhost;Database=relay_events;Username=postgres;Password=postgres")]
    [InlineData("Host=db.example.com;Port=5432;Database=events")]
    [InlineData("Server=postgresql://localhost:5432/relay_events")]
    public void CreateProviderFromConnectionString_WithPostgresConnectionString_ReturnsPostgreSqlProvider(
        string connectionString)
    {
        // Act
        var provider = DbProviderFactory.CreateProviderFromConnectionString(connectionString);

        // Assert
        Assert.IsType<PostgreSqlProvider>(provider);
    }

    [Theory]
    [InlineData("Server=.\\SQLEXPRESS;Database=relay_events;Trusted_Connection=true;")]
    [InlineData("Server=sql.example.com;Database=relay_events;User Id=sa;Password=password;")]
    [InlineData("Server=tcp:server.database.windows.net,1433")]
    public void CreateProviderFromConnectionString_WithSqlServerConnectionString_ReturnsSqlServerProvider(
        string connectionString)
    {
        // Act
        var provider = DbProviderFactory.CreateProviderFromConnectionString(connectionString);

        // Assert
        Assert.IsType<SqlServerProvider>(provider);
    }

    [Theory]
    [InlineData("Data Source=relay_events.db")]
    [InlineData("Data Source=./data/events.db")]
    [InlineData("Filename=relay_events.db")]
    [InlineData("relay_events.db")]
    public void CreateProviderFromConnectionString_WithSqliteConnectionString_ReturnsSqliteProvider(
        string connectionString)
    {
        // Act
        var provider = DbProviderFactory.CreateProviderFromConnectionString(connectionString);

        // Assert
        Assert.IsType<SqliteProvider>(provider);
    }

    [Fact]
    public void CreateProviderFromConnectionString_WithNullConnectionString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => DbProviderFactory.CreateProviderFromConnectionString(null!));

        Assert.Contains("Connection string cannot be null or empty", exception.Message);
    }

    [Fact]
    public void CreateProviderFromConnectionString_WithEmptyConnectionString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => DbProviderFactory.CreateProviderFromConnectionString(""));

        Assert.Contains("Connection string cannot be null or empty", exception.Message);
    }

    [Fact]
    public void CreateProviderFromConnectionString_WithWhitespaceConnectionString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => DbProviderFactory.CreateProviderFromConnectionString("   "));

        Assert.Contains("Connection string cannot be null or empty", exception.Message);
    }

    [Fact]
    public void CreateProviderFromConnectionString_WithUnrecognizedConnectionString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => DbProviderFactory.CreateProviderFromConnectionString("unrecognized-connection-string"));

        Assert.Contains("Could not detect database provider", exception.Message);
    }

    [Fact]
    public void GetSupportedProviders_ReturnsAllSupportedProviders()
    {
        // Act
        var providers = DbProviderFactory.GetSupportedProviders().ToList();

        // Assert
        Assert.NotEmpty(providers);
        Assert.Contains("PostgreSQL", providers);
        Assert.Contains("SqlServer", providers);
        Assert.Contains("Sqlite", providers);
    }
}
