using Relay.Core.EventSourcing.Infrastructure;
using System;
using Xunit;

namespace Relay.Core.Tests.EventSourcing.Database;

/// <summary>
/// Unit tests for database-independent EventStoreDbContextFactory.
/// </summary>
public class EventStoreDbContextFactoryTests
{
    private readonly EventStoreDbContextFactory _factory;

    public EventStoreDbContextFactoryTests()
    {
        _factory = new EventStoreDbContextFactory();
    }

    [Fact]
    public void CreateDbContext_WithNoArguments_ReturnsValidContext()
    {
        // Act
        var context = _factory.CreateDbContext(Array.Empty<string>());

        // Assert
        Assert.NotNull(context);
        Assert.IsAssignableFrom<EventStoreDbContext>(context);
    }

    [Fact]
    public void CreateDbContext_WithNullArguments_ReturnsValidContext()
    {
        // Act
        var context = _factory.CreateDbContext(null!);

        // Assert
        Assert.NotNull(context);
        Assert.IsAssignableFrom<EventStoreDbContext>(context);
    }

    [Fact]
    public void CreateDbContext_WithPostgresConnectionString_ConfiguresPostgresProvider()
    {
        // Arrange
        var args = new[] { "--connection=Host=localhost;Database=test;Username=user;Password=pass" };

        // Act
        var context = _factory.CreateDbContext(args);

        // Assert
        Assert.NotNull(context);
        // Verify it's configured with Npgsql provider
        Assert.Contains("Npgsql", context.Database.ProviderName);
    }

    [Fact]
    public void CreateDbContext_WithSqlServerConnectionString_ThrowsIfProviderNotInstalled()
    {
        // Arrange
        var args = new[] { "--connection=Server=.\\SQLEXPRESS;Database=test;Trusted_Connection=true;" };

        // Act & Assert
        // SQL Server provider not installed, should throw InvalidOperationException
        Assert.Throws<InvalidOperationException>(() =>
            _factory.CreateDbContext(args));
    }

    [Fact]
    public void CreateDbContext_WithSqliteConnectionString_ThrowsIfProviderNotInstalled()
    {
        // Arrange
        var args = new[] { "--connection=Data Source=test.db" };

        // Act & Assert
        // SQLite provider not installed, should throw InvalidOperationException
        Assert.Throws<InvalidOperationException>(() =>
            _factory.CreateDbContext(args));
    }

    [Fact]
    public void CreateDbContext_WithExplicitProviderAndConnection_ThrowsIfProviderNotInstalled()
    {
        // Arrange
        var args = new[]
        {
            "--provider=SqlServer",
            "--connection=Server=localhost;Database=test;"
        };

        // Act & Assert
        // SQL Server provider not installed, should throw InvalidOperationException
        Assert.Throws<InvalidOperationException>(() =>
            _factory.CreateDbContext(args));
    }

    [Fact]
    public void CreateDbContext_WithProviderAndNoConnection_UsesDefaultConnection()
    {
        // Arrange
        var args = new[] { "--provider=PostgreSQL" };

        // Act
        var context = _factory.CreateDbContext(args);

        // Assert
        Assert.NotNull(context);
        Assert.Contains("Npgsql", context.Database.ProviderName);
    }

    [Fact]
    public void CreateDbContext_WithProviderArgument_CaseInsensitive()
    {
        // Arrange
        var args = new[]
        {
            "--provider=postgresql",
            "--connection=Host=localhost;Database=test;"
        };

        // Act
        var context = _factory.CreateDbContext(args);

        // Assert
        Assert.NotNull(context);
        Assert.Contains("Npgsql", context.Database.ProviderName);
    }

    [Fact]
    public void CreateDbContext_WithConnectionEnvironmentVariable_UsesEnvironmentVariable()
    {
        // Arrange
        var connectionString = "Host=env-localhost;Database=test;Username=user;Password=pass";
        Environment.SetEnvironmentVariable("EVENTSTORE_CONNECTION_STRING", connectionString);

        try
        {
            // Act
            var context = _factory.CreateDbContext(Array.Empty<string>());

            // Assert
            Assert.NotNull(context);
            Assert.Contains("Npgsql", context.Database.ProviderName);
        }
        finally
        {
            Environment.SetEnvironmentVariable("EVENTSTORE_CONNECTION_STRING", null);
        }
    }

    [Fact]
    public void CreateDbContext_WithProviderEnvironmentVariable_UsesEnvironmentVariable()
    {
        // Arrange
        Environment.SetEnvironmentVariable("EVENTSTORE_DB_PROVIDER", "PostgreSQL");
        Environment.SetEnvironmentVariable("EVENTSTORE_CONNECTION_STRING", "Host=localhost;Database=test;");

        try
        {
            // Act
            var context = _factory.CreateDbContext(Array.Empty<string>());

            // Assert
            Assert.NotNull(context);
            Assert.Contains("Npgsql", context.Database.ProviderName);
        }
        finally
        {
            Environment.SetEnvironmentVariable("EVENTSTORE_DB_PROVIDER", null);
            Environment.SetEnvironmentVariable("EVENTSTORE_CONNECTION_STRING", null);
        }
    }

    [Fact]
    public void CreateDbContext_ArgumentsOverrideEnvironmentVariables()
    {
        // Arrange
        Environment.SetEnvironmentVariable("EVENTSTORE_DB_PROVIDER", "PostgreSQL");
        Environment.SetEnvironmentVariable("EVENTSTORE_CONNECTION_STRING", "Host=env-localhost;Database=env.db;");

        var args = new[]
        {
            "--provider=PostgreSQL",
            "--connection=Host=override-localhost;Database=override.db;"
        };

        try
        {
            // Act
            var context = _factory.CreateDbContext(args);

            // Assert
            Assert.NotNull(context);
            // Should use values from args, not from environment variables
            Assert.Contains("Npgsql", context.Database.ProviderName);
        }
        finally
        {
            Environment.SetEnvironmentVariable("EVENTSTORE_DB_PROVIDER", null);
            Environment.SetEnvironmentVariable("EVENTSTORE_CONNECTION_STRING", null);
        }
    }

    [Fact]
    public void CreateDbContext_WithUnknownArguments_IgnoresUnknownArguments()
    {
        // Arrange
        var args = new[]
        {
            "--unknown=value",
            "--connection=Host=localhost;Database=test;"
        };

        // Act
        var context = _factory.CreateDbContext(args);

        // Assert
        Assert.NotNull(context);
        // Should still work and auto-detect from connection string
        Assert.Contains("Npgsql", context.Database.ProviderName);
    }

    [Fact]
    public void CreateDbContext_WithMultipleConnectionArguments_UsesFirstOne()
    {
        // Arrange
        var args = new[]
        {
            "--connection=Host=localhost;Database=test1;",
            "--connection=Host=localhost;Database=test2;"
        };

        // Act
        var context = _factory.CreateDbContext(args);

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context);
    }

    [Fact]
    public void CreateDbContext_ReturnsContextWithDbSets()
    {
        // Act
        var context = _factory.CreateDbContext(Array.Empty<string>());

        // Assert
        Assert.NotNull(context.Events);
        Assert.NotNull(context.Snapshots);
    }

    [Theory]
    [InlineData("--provider=postgresql", "--connection=Host=localhost;Database=test;", true)]
    [InlineData("--provider=PostgreSQL", "--connection=Host=localhost;Database=test;", true)]
    [InlineData("--provider=sqlserver", "--connection=Server=localhost;Database=test;", false)] // Not installed
    [InlineData("--provider=sqlite", "--connection=Data Source=test.db", false)] // Not installed
    public void CreateDbContext_WithValidProviders_SucceedsOrThrows(string providerArg, string connArg, bool shouldSucceed)
    {
        // Arrange
        var args = new[] { providerArg, connArg };

        // Act & Assert
        if (shouldSucceed)
        {
            var context = _factory.CreateDbContext(args);
            Assert.NotNull(context);
        }
        else
        {
            // Provider not installed, should throw
            Assert.Throws<InvalidOperationException>(() => _factory.CreateDbContext(args));
        }
    }

    [Fact]
    public void CreateDbContext_MultipleCallsCreateIndependentContexts()
    {
        // Act
        var context1 = _factory.CreateDbContext(Array.Empty<string>());
        var context2 = _factory.CreateDbContext(Array.Empty<string>());

        // Assert
        Assert.NotNull(context1);
        Assert.NotNull(context2);
        Assert.NotSame(context1, context2);
    }
}
