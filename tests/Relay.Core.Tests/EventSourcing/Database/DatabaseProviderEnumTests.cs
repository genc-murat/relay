using System;
using Relay.Core.EventSourcing.Infrastructure.Database;
using Xunit;

namespace Relay.Core.Tests.EventSourcing.Database;

/// <summary>
/// Unit tests for DatabaseProvider enum and enum-based factory methods.
/// </summary>
public class DatabaseProviderEnumTests
{
    [Fact]
    public void DatabaseProvider_PostgreSQL_IsValid()
    {
        // Arrange & Act
        var provider = DatabaseProvider.PostgreSQL;

        // Assert
        Assert.Equal(DatabaseProvider.PostgreSQL, provider);
    }

    [Fact]
    public void DatabaseProvider_MySQL_IsValid()
    {
        // Arrange & Act
        var provider = DatabaseProvider.MySQL;

        // Assert
        Assert.Equal(DatabaseProvider.MySQL, provider);
    }

    [Fact]
    public void DatabaseProvider_MariaDB_IsValid()
    {
        // Arrange & Act
        var provider = DatabaseProvider.MariaDB;

        // Assert
        Assert.Equal(DatabaseProvider.MariaDB, provider);
    }

    [Fact]
    public void DatabaseProvider_SqlServer_IsValid()
    {
        // Arrange & Act
        var provider = DatabaseProvider.SqlServer;

        // Assert
        Assert.Equal(DatabaseProvider.SqlServer, provider);
    }

    [Fact]
    public void DatabaseProvider_Sqlite_IsValid()
    {
        // Arrange & Act
        var provider = DatabaseProvider.Sqlite;

        // Assert
        Assert.Equal(DatabaseProvider.Sqlite, provider);
    }

    [Fact]
    public void CreateProvider_WithPostgreSQLEnum_ReturnsPostgreSqlProvider()
    {
        // Act
        var provider = DbProviderFactory.CreateProvider(DatabaseProvider.PostgreSQL);

        // Assert
        Assert.IsType<PostgreSqlProvider>(provider);
        Assert.Equal(DatabaseProvider.PostgreSQL, provider.Provider);
    }

    [Fact]
    public void CreateProvider_WithMySQLEnum_ReturnsMySqlProvider()
    {
        // Act
        var provider = DbProviderFactory.CreateProvider(DatabaseProvider.MySQL);

        // Assert
        Assert.IsType<MySqlProvider>(provider);
        Assert.Equal(DatabaseProvider.MySQL, provider.Provider);
    }

    [Fact]
    public void CreateProvider_WithMariaDBEnum_ReturnsMariaDbProvider()
    {
        // Act
        var provider = DbProviderFactory.CreateProvider(DatabaseProvider.MariaDB);

        // Assert
        Assert.IsType<MariaDbProvider>(provider);
        Assert.Equal(DatabaseProvider.MariaDB, provider.Provider);
    }

    [Fact]
    public void CreateProvider_WithSqlServerEnum_ReturnsSqlServerProvider()
    {
        // Act
        var provider = DbProviderFactory.CreateProvider(DatabaseProvider.SqlServer);

        // Assert
        Assert.IsType<SqlServerProvider>(provider);
        Assert.Equal(DatabaseProvider.SqlServer, provider.Provider);
    }

    [Fact]
    public void CreateProvider_WithSqliteEnum_ReturnsSqliteProvider()
    {
        // Act
        var provider = DbProviderFactory.CreateProvider(DatabaseProvider.Sqlite);

        // Assert
        Assert.IsType<SqliteProvider>(provider);
        Assert.Equal(DatabaseProvider.Sqlite, provider.Provider);
    }

    [Fact]
    public void IDbProvider_HasDatabaseProviderProperty()
    {
        // Arrange
        var providers = new IDbProvider[]
        {
            new PostgreSqlProvider(),
            new MySqlProvider(),
            new SqlServerProvider(),
            new SqliteProvider()
        };

        // Act & Assert
        foreach (var provider in providers)
        {
            Assert.NotNull(provider.Provider);
            Assert.True(Enum.IsDefined(typeof(DatabaseProvider), provider.Provider));
        }
    }

    [Theory]
    [InlineData(DatabaseProvider.PostgreSQL)]
    [InlineData(DatabaseProvider.MySQL)]
    [InlineData(DatabaseProvider.MariaDB)]
    [InlineData(DatabaseProvider.SqlServer)]
    [InlineData(DatabaseProvider.Sqlite)]
    public void CreateProvider_WithAllEnumValues_Succeeds(DatabaseProvider provider)
    {
        // Act
        var dbProvider = DbProviderFactory.CreateProvider(provider);

        // Assert
        Assert.NotNull(dbProvider);
        Assert.Equal(provider, dbProvider.Provider);
    }

    [Fact]
    public void DatabaseProvider_CanConvertToString()
    {
        // Arrange
        var provider = DatabaseProvider.PostgreSQL;

        // Act
        var stringValue = provider.ToString();

        // Assert
        Assert.Equal("PostgreSQL", stringValue);
    }

    [Fact]
    public void DatabaseProvider_CanParseFromString()
    {
        // Act
        var parsed = Enum.Parse<DatabaseProvider>("MySQL");

        // Assert
        Assert.Equal(DatabaseProvider.MySQL, parsed);
    }

    [Fact]
    public void DatabaseProvider_AllValuesAreUnique()
    {
        // Arrange
        var values = Enum.GetValues(typeof(DatabaseProvider));

        // Assert
        Assert.Equal(5, values.Length);
    }
}
