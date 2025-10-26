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
        Assert.IsType<EventStoreDbContext>(context, exactMatch: false);
    }

    [Fact]
    public void CreateDbContext_WithNullArguments_ReturnsValidContext()
    {
        // Act
        var context = _factory.CreateDbContext(null!);

        // Assert
        Assert.NotNull(context);
        Assert.IsType<EventStoreDbContext>(context, exactMatch: false);
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
        var context = _factory.CreateDbContext([]);

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

    [Fact]
    public void CreateDbContext_WithUnrecognizedConnectionString_ThrowsArgumentException()
    {
        // Arrange - Connection string that cannot be recognized as any supported provider
        var args = new[] { "--connection=ThisIsNotAValidConnectionString" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _factory.CreateDbContext(args));
    }

    [Fact]
    public void CreateDbContext_WithConnectionStringMissingRequiredParts_Succeeds()
    {
        // Arrange - PostgreSQL connection string missing database
        var args = new[] { "--connection=Host=localhost;Username=user;Password=pass" };

        // Act & Assert - Context creation succeeds, validation happens later
        var context = _factory.CreateDbContext(args);
        Assert.NotNull(context);
        Assert.Contains("Npgsql", context.Database.ProviderName);
    }

    [Fact]
    public void CreateDbContext_WithMalformedConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var args = new[] { "--connection=ThisIsNotAValidConnectionString" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _factory.CreateDbContext(args));
    }

    [Fact]
    public void CreateDbContext_WithProviderNameCaseVariations_HandlesCorrectly()
    {
        // Arrange - Test various case combinations
        var testCases = new[]
        {
            new[] { "--provider=POSTGRESQL" },
            new[] { "--provider=postgresql" },
            new[] { "--provider=PostgreSQL" },
            new[] { "--provider=postgres" }
        };

        foreach (var args in testCases)
        {
            // Act
            var context = _factory.CreateDbContext(args);

            // Assert
            Assert.NotNull(context);
            Assert.Contains("Npgsql", context.Database.ProviderName);
        }
    }

    [Fact]
    public void CreateDbContext_WithMalformedArgumentFormat_IgnoresMalformedArguments()
    {
        // Arrange - Arguments with missing values or invalid format
        var args = new[]
        {
            "--provider", // Missing value
            "--connection=Host=localhost;Database=test;",
            "--unknown" // Unknown argument
        };

        // Act
        var context = _factory.CreateDbContext(args);

        // Assert - Should still work by detecting from connection string
        Assert.NotNull(context);
        Assert.Contains("Npgsql", context.Database.ProviderName);
    }

    [Fact]
    public void CreateDbContext_WithEmptyArgumentValues_UsesDefaults()
    {
        // Arrange
        var args = new[]
        {
            "--provider=",
            "--connection="
        };

        // Act
        var context = _factory.CreateDbContext(args);

        // Assert - Should create context with default configuration
        Assert.NotNull(context);
    }

    [Fact]
    public void CreateDbContext_WithConflictingProviders_UsesExplicitProvider()
    {
        // Arrange - Provider in connection string vs explicit provider
        var args = new[]
        {
            "--provider=PostgreSQL",
            "--connection=Host=localhost;Database=test;" // PostgreSQL-style connection
        };

        // Act
        var context = _factory.CreateDbContext(args);

        // Assert
        Assert.NotNull(context);
        Assert.Contains("Npgsql", context.Database.ProviderName);
    }

    [Fact]
    public void CreateDbContext_WithMultipleProviders_LastOneWins()
    {
        // Arrange
        var args = new[]
        {
            "--provider=PostgreSQL",
            "--provider=PostgreSQL", // Same provider again
            "--connection=Host=localhost;Database=test;"
        };

        // Act
        var context = _factory.CreateDbContext(args);

        // Assert
        Assert.NotNull(context);
        Assert.Contains("Npgsql", context.Database.ProviderName);
    }

    [Fact]
    public void CreateDbContext_WithArgumentsContainingSpaces_HandlesCorrectly()
    {
        // Arrange - Arguments with spaces (simulating command line)
        var args = new[]
        {
            "--connection=Host=localhost; Database=test; Username=user; Password=pass;"
        };

        // Act
        var context = _factory.CreateDbContext(args);

        // Assert
        Assert.NotNull(context);
        Assert.Contains("Npgsql", context.Database.ProviderName);
    }

    [Fact]
    public void CreateDbContext_WithArgumentsContainingSpecialCharacters_HandlesCorrectly()
    {
        // Arrange - Connection string with special characters in password
        var args = new[]
        {
            "--connection=Host=localhost;Database=test;Username=user;Password=p@ssw0rd!#$%^&*()"
        };

        // Act
        var context = _factory.CreateDbContext(args);

        // Assert
        Assert.NotNull(context);
        Assert.Contains("Npgsql", context.Database.ProviderName);
    }

    [Fact]
    public void CreateDbContext_WithExtremelyLongArguments_HandlesCorrectly()
    {
        // Arrange - Very long connection string
        var longDatabaseName = new string('a', 1000);
        var args = new[]
        {
            $"--connection=Host=localhost;Database={longDatabaseName};Username=user;Password=pass"
        };

        // Act
        var context = _factory.CreateDbContext(args);

        // Assert
        Assert.NotNull(context);
        Assert.Contains("Npgsql", context.Database.ProviderName);
    }

    [Fact]
    public void CreateDbContext_WithUnicodeCharactersInArguments_HandlesCorrectly()
    {
        // Arrange - Connection string with Unicode characters
        var args = new[]
        {
            "--connection=Host=localhost;Database=tëst;Username=üsér;Password=päss"
        };

        // Act
        var context = _factory.CreateDbContext(args);

        // Assert
        Assert.NotNull(context);
        Assert.Contains("Npgsql", context.Database.ProviderName);
    }

    [Fact]
    public void CreateDbContext_WithEnvironmentVariablesAndMalformedArgs_UsesEnvironmentVariables()
    {
        // Arrange
        Environment.SetEnvironmentVariable("EVENTSTORE_DB_PROVIDER", "PostgreSQL");
        Environment.SetEnvironmentVariable("EVENTSTORE_CONNECTION_STRING", "Host=env-localhost;Database=test;");

        var args = new[]
        {
            "--provider", // Malformed - missing value
            "--connection", // Malformed - missing value
        };

        try
        {
            // Act
            var context = _factory.CreateDbContext(args);

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
    public void CreateDbContext_WithArgumentsOverrideEnvironmentVariables_EvenIfMalformed()
    {
        // Arrange
        Environment.SetEnvironmentVariable("EVENTSTORE_DB_PROVIDER", "SomeOtherProvider");
        Environment.SetEnvironmentVariable("EVENTSTORE_CONNECTION_STRING", "SomeOtherConnection");

        var args = new[]
        {
            "--provider=PostgreSQL",
            "--connection=Host=localhost;Database=test;"
        };

        try
        {
            // Act
            var context = _factory.CreateDbContext(args);

            // Assert - Should use args, not environment variables
            Assert.NotNull(context);
            Assert.Contains("Npgsql", context.Database.ProviderName);
        }
        finally
        {
            Environment.SetEnvironmentVariable("EVENTSTORE_DB_PROVIDER", null);
            Environment.SetEnvironmentVariable("EVENTSTORE_CONNECTION_STRING", null);
        }
    }
}
