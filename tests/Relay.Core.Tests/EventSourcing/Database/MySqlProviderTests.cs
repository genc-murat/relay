using System;
using Relay.Core.EventSourcing.Infrastructure.Database;
using Xunit;

namespace Relay.Core.Tests.EventSourcing.Database;

/// <summary>
/// Unit tests for MySQL/MariaDB provider factory and implementation.
/// </summary>
public class MySqlProviderTests
{
    [Fact]
    public void CreateProvider_WithMySql_ReturnsMySqlProvider()
    {
        // Act
        var provider = DbProviderFactory.CreateProvider("mysql");

        // Assert
        Assert.IsType<MySqlProvider>(provider);
        Assert.Equal("MySQL", provider.ProviderName);
    }

    [Fact]
    public void CreateProvider_WithMySQL_CaseInsensitive_ReturnsMySqlProvider()
    {
        // Act
        var provider = DbProviderFactory.CreateProvider("MySQL");

        // Assert
        Assert.IsType<MySqlProvider>(provider);
    }

    [Fact]
    public void CreateProvider_WithMariaDb_ReturnsMySqlProvider()
    {
        // Act
        var provider = DbProviderFactory.CreateProvider("mariadb");

        // Assert
        Assert.IsType<MySqlProvider>(provider);
    }

    [Fact]
    public void CreateProvider_WithMariaDB_CaseInsensitive_ReturnsMySqlProvider()
    {
        // Act
        var provider = DbProviderFactory.CreateProvider("MariaDB");

        // Assert
        Assert.IsType<MySqlProvider>(provider);
    }

    [Fact]
    public void CreateProvider_WithPomeloFullName_ReturnsMySqlProvider()
    {
        // Act
        var provider = DbProviderFactory.CreateProvider("pomelo.entityframeworkcore.mysql");

        // Assert
        Assert.IsType<MySqlProvider>(provider);
    }

    [Fact]
    public void MySqlProvider_Properties_ReturnCorrectValues()
    {
        // Arrange & Act
        var provider = new MySqlProvider();

        // Assert
        Assert.Equal("MySQL", provider.ProviderName);
        Assert.Equal("Pomelo.EntityFrameworkCore.MySql", provider.ProviderType);
    }

    [Theory]
    [InlineData("Server=localhost;Database=relay_events;Uid=root;Pwd=password;")]
    [InlineData("Server=db.example.com;Database=events;Uid=admin;Pwd=secret;")]
    [InlineData("Server=localhost;Port=3306;Database=relay_events;Uid=root;Pwd=password;")]
    [InlineData("Server=db.example.com;Port=3306;Database=events;Uid=admin;Pwd=secret;")]
    public void CreateProviderFromConnectionString_WithMySqlConnectionString_ReturnsMySqlProvider(
        string connectionString)
    {
        // Act
        var provider = DbProviderFactory.CreateProviderFromConnectionString(connectionString);

        // Assert
        Assert.IsType<MySqlProvider>(provider);
    }

    [Theory]
    [InlineData("Server=localhost;Database=mydb;Uid=root;Pwd=pass;")]
    [InlineData("Server=mysql.example.com;Database=events;Uid=admin;Pwd=pass;")]
    [InlineData("Server=db;Database=test;Uid=user;Pwd=pwd;")]
    public void CreateProviderFromConnectionString_WithVariousMySqlFormats_DetectsMySql(
        string connectionString)
    {
        // Act
        var provider = DbProviderFactory.CreateProviderFromConnectionString(connectionString);

        // Assert
        Assert.IsType<MySqlProvider>(provider);
        Assert.Equal("MySQL", provider.ProviderName);
    }

    [Fact]
    public void CreateProviderFromConnectionString_WithMySqlDefaultPort_DetectsMySql()
    {
        // Arrange
        var connectionString = "Server=localhost;Port=3306;Database=relay_events;Uid=root;Pwd=password;";

        // Act
        var provider = DbProviderFactory.CreateProviderFromConnectionString(connectionString);

        // Assert
        Assert.IsType<MySqlProvider>(provider);
    }

    [Fact]
    public void MySqlProvider_Configure_ThrowsIfProviderNotInstalled()
    {
        // Arrange
        var provider = new MySqlProvider();
        var optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<
            Relay.Core.EventSourcing.Infrastructure.EventStoreDbContext>();
        var connectionString = "Server=localhost;Database=test;Uid=root;Pwd=password;";

        // Act & Assert
        // MySQL provider not installed, should throw InvalidOperationException
        Assert.Throws<InvalidOperationException>(() =>
            provider.Configure(optionsBuilder, connectionString));
    }

    [Fact]
    public void GetSupportedProviders_IncludesMysql()
    {
        // Act
        var providers = DbProviderFactory.GetSupportedProviders();

        // Assert
        Assert.Contains("MySQL", providers);
    }

    [Fact]
    public void GetSupportedProviders_IncludesMariaDb()
    {
        // Act
        var providers = DbProviderFactory.GetSupportedProviders();

        // Assert
        Assert.Contains("MariaDB", providers);
    }

    [Fact]
    public void CreateProvider_WithInvalidMySqlVariation_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            DbProviderFactory.CreateProvider("mysqlserver"));
    }

    [Theory]
    [InlineData("Server=localhost;Database=test;Uid=admin;Pwd=pass;")]
    [InlineData("Server=db.example.com;Database=events;Uid=user;Pwd=secret;")]
    public void CreateProviderFromConnectionString_WithMySqlConnectionString_NotDetectedAsOtherProvider(
        string connectionString)
    {
        // Act
        var provider = DbProviderFactory.CreateProviderFromConnectionString(connectionString);

        // Assert
        // Should detect as MySQL, not SQL Server
        Assert.IsType<MySqlProvider>(provider);
        Assert.False(provider is SqlServerProvider, "Should not be detected as SQL Server");
    }
}
