# Database Independence Guide for EventStoreDbContext

## Overview

The `EventStoreDbContextFactory` has been refactored to support multiple database providers, making your event sourcing infrastructure database-agnostic. This guide explains the architecture and how to use different database providers.

## Supported Database Providers

- **PostgreSQL** (Default)
- **MySQL** / **MariaDB** (via reflection if provider is installed)
- **SQL Server** (via reflection if provider is installed)
- **SQLite** (via reflection if provider is installed)

## DatabaseProvider Enum

The `DatabaseProvider` enumeration provides type-safe provider selection:

```csharp
public enum DatabaseProvider
{
    PostgreSQL,   // Npgsql provider
    MySQL,        // Pomelo provider
    MariaDB,      // Pomelo provider (optimized for MariaDB)
    SqlServer,    // SQL Server provider
    Sqlite        // SQLite provider
}
```

**Benefits of using the enum:**
- ✅ Compile-time type safety
- ✅ IntelliSense support
- ✅ No string typos
- ✅ Clear intent
- ✅ Recommended approach for new code

## Architecture

### Core Components

#### 1. **IDbProvider Interface** (`Infrastructure/Database/IDbProvider.cs`)

The foundation for database provider abstraction:

```csharp
public interface IDbProvider
{
    DatabaseProvider Provider { get; }    // Enum for type-safe access
    string ProviderName { get; }          // Display name (e.g., "PostgreSQL")
    string ProviderType { get; }          // Full provider type
    void Configure(DbContextOptionsBuilder<EventStoreDbContext> optionsBuilder, string connectionString);
}
```

#### 2. **Provider Implementations**

- `PostgreSqlProvider`: Uses Npgsql for PostgreSQL (included by default)
- `MySqlProvider`: Uses Pomelo provider for MySQL
- `MariaDbProvider`: Uses Pomelo provider optimized for MariaDB
- `SqlServerProvider`: Uses reflection to dynamically load SQL Server provider
- `SqliteProvider`: Uses reflection to dynamically load SQLite provider

#### 3. **DbProviderFactory** (`Infrastructure/Database/DbProviderFactory.cs`)

Factory for creating provider instances with three methods:

```csharp
// By DatabaseProvider enum (Type-safe - Recommended!)
var provider = DbProviderFactory.CreateProvider(DatabaseProvider.PostgreSQL);

// By provider type name string (Backward compatible)
var provider = DbProviderFactory.CreateProvider("PostgreSQL");

// By connection string detection (Auto-detection)
var provider = DbProviderFactory.CreateProviderFromConnectionString(connectionString);
```

#### 4. **EventStoreDbContextFactory** (Refactored)

Now supports multiple configuration methods:

- Command-line arguments
- Environment variables
- Auto-detection from connection string
- Fallback to PostgreSQL default

## Usage Examples

### 1. Auto-Detection from Connection String

```csharp
var services = new ServiceCollection();

// Automatically detects PostgreSQL from connection string
services.AddEfCoreEventStore("Host=localhost;Database=relay_events;Username=postgres;Password=postgres");

// Automatically detects SQL Server
services.AddEfCoreEventStore("Server=.\\SQLEXPRESS;Database=relay_events;Trusted_Connection=true;");

// Automatically detects SQLite
services.AddEfCoreEventStore("Data Source=relay_events.db");
```

### 2. Explicit Provider Specification using Enum (Recommended)

```csharp
var services = new ServiceCollection();

// Explicitly specify provider using DatabaseProvider enum - Type-safe!
services.AddEfCoreEventStore(
    DatabaseProvider.PostgreSQL,
    "Host=localhost;Database=relay_events;");

services.AddEfCoreEventStore(
    DatabaseProvider.MySQL,
    "Server=localhost;Database=relay_events;Uid=root;Pwd=password;");

services.AddEfCoreEventStore(
    DatabaseProvider.MariaDB,
    "Server=localhost;Database=relay_events;Uid=root;Pwd=password;");

services.AddEfCoreEventStore(
    DatabaseProvider.SqlServer,
    "Server=localhost;Database=relay_events;");

services.AddEfCoreEventStore(
    DatabaseProvider.Sqlite,
    "Data Source=relay_events.db");
```

### 3. Explicit Provider Specification using String

```csharp
var services = new ServiceCollection();

// Explicitly specify provider using string - for backward compatibility
services.AddEfCoreEventStore("PostgreSQL", "Host=localhost;Database=relay_events;");
services.AddEfCoreEventStore("MySQL", "Server=localhost;Database=relay_events;Uid=root;Pwd=password;");
services.AddEfCoreEventStore("MariaDB", "Server=localhost;Database=relay_events;Uid=root;Pwd=password;");
services.AddEfCoreEventStore("SqlServer", "Server=localhost;Database=relay_events;");
services.AddEfCoreEventStore("Sqlite", "Data Source=relay_events.db");
```

### 4. Custom Configuration

```csharp
var services = new ServiceCollection();

// Use the existing custom options action approach
services.AddEfCoreEventStore(options =>
    options.UseNpgsql("Host=localhost;Database=relay_events;"));
```

### 5. Environment Variables

Set environment variables before runtime:

```bash
# Configure PostgreSQL
set EVENTSTORE_DB_PROVIDER=PostgreSQL
set EVENTSTORE_CONNECTION_STRING=Host=localhost;Database=relay_events;

# Or configure SQL Server
set EVENTSTORE_DB_PROVIDER=SqlServer
set EVENTSTORE_CONNECTION_STRING=Server=localhost;Database=relay_events;
```

### 6. Design-Time Migrations (EF Core CLI)

For EF Core CLI tools (`dotnet ef`), use command-line arguments:

```bash
# Use PostgreSQL (default)
dotnet ef migrations add InitialMigration

# Use MySQL
dotnet ef migrations add InitialMigration -- --provider=MySQL --connection="Server=localhost;Database=relay_events;Uid=root;Pwd=password;"

# Use MariaDB
dotnet ef migrations add InitialMigration -- --provider=MariaDB --connection="Server=localhost;Database=relay_events;Uid=root;Pwd=password;"

# Use SQL Server
dotnet ef migrations add InitialMigration -- --provider=SqlServer --connection="Server=localhost;Database=relay_events;"

# Use SQLite
dotnet ef migrations add InitialMigration -- --provider=Sqlite --connection="Data Source=relay_events.db"
```

## Configuration Priority

The configuration is resolved in this order:

1. **Command-line arguments** (highest priority)
   - `--provider=<ProviderName>`
   - `--connection=<ConnectionString>`

2. **Environment variables**
   - `EVENTSTORE_DB_PROVIDER`
   - `EVENTSTORE_CONNECTION_STRING`

3. **Provider detection from connection string**
   - Analyzes the connection string to determine provider type

4. **Default fallback** (lowest priority)
   - PostgreSQL with localhost connection

## Connection String Patterns

The factory auto-detects providers based on connection string patterns:

### PostgreSQL Patterns
```
Host=localhost;Database=relay_events;Username=postgres;Password=postgres
Server=postgresql://localhost:5432/relay_events
Host=db.example.com;Port=5432;Database=events
```

### MySQL / MariaDB Patterns
```
Server=localhost;Database=relay_events;Uid=root;Pwd=password;
Server=db.example.com;Database=events;Uid=admin;Pwd=secret;
Server=localhost;Port=3306;Database=relay_events;Uid=root;Pwd=password;
Host=localhost;Database=relay_events;Uid=root;Pwd=password;
```

**Key Identifiers:**
- `Uid=` or `Pwd=` (MySQL/MariaDB specific)
- `Port=3306` (MySQL default port)
- `Database=` not `Initial Catalog=`

### SQL Server Patterns
```
Server=.\\SQLEXPRESS;Database=relay_events;Trusted_Connection=true;
Server=sql.example.com;Database=relay_events;User Id=sa;Password=password;
Server=tcp:server.database.windows.net,1433
```

### SQLite Patterns
```
Data Source=relay_events.db
Data Source=./data/events.db
Filename=relay_events.db
relay_events.db
```

## Installing Additional Providers

### MySQL / MariaDB

```bash
dotnet add package Pomelo.EntityFrameworkCore.MySql
```

### SQL Server

```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

### SQLite

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

Once installed, the factory can dynamically load and use them via reflection.

## Creating Custom Providers

To support additional database providers, implement `IDbProvider`:

```csharp
public class MyCustomProvider : IDbProvider
{
    public string ProviderName => "MyDatabase";

    public string ProviderType => "MyCompany.EntityFrameworkCore.MyDatabase";

    public void Configure(DbContextOptionsBuilder<EventStoreDbContext> optionsBuilder, string connectionString)
    {
        // Configure your custom provider
        optionsBuilder.UseMyDatabase(connectionString);
    }
}
```

Then register it in the factory:

```csharp
public static IDbProvider CreateProvider(string providerType)
{
    return providerType?.ToLowerInvariant() switch
    {
        "mydatabase" => new MyCustomProvider(),
        // ... existing providers
        _ => throw new ArgumentException($"Unsupported database provider: '{providerType}'", nameof(providerType))
    };
}
```

## Best Practices

### 1. Environment-Specific Configuration

Use environment variables for different environments:

```csharp
if (env.IsProduction())
{
    // Production: SQL Server
    services.AddEfCoreEventStore("SqlServer", Configuration.GetConnectionString("EventStore"));
}
else if (env.IsDevelopment())
{
    // Development: SQLite
    services.AddEfCoreEventStore("Sqlite", "Data Source=dev_events.db");
}
```

### 2. Migration Strategy

Create provider-specific migrations:

```bash
# PostgreSQL migrations
dotnet ef migrations add Initial_PostgreSQL

# SQL Server migrations
dotnet ef migrations add Initial_SqlServer

# SQLite migrations
dotnet ef migrations add Initial_SQLite
```

### 3. Testing

Use SQLite in-memory database for testing:

```csharp
services.AddDbContext<EventStoreDbContext>(options =>
    options.UseSqlite("Data Source=:memory:"));
```

### 4. Connection String Management

- Never hardcode connection strings
- Use configuration files or environment variables
- Leverage ASP.NET Core configuration providers
- Use Azure Key Vault or similar for production secrets

## Troubleshooting

### Error: "Provider is not installed"

If you see "SQL Server EF Core provider is not installed":

```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

### Provider detection fails

If connection string detection fails:

1. Check connection string format matches expected patterns
2. Explicitly specify provider: `services.AddEfCoreEventStore("SqlServer", connectionString)`
3. Use environment variables as fallback

### Migrations failing

- Ensure the database provider is installed
- Verify connection string is correct for the provider
- Check if the database exists and is accessible
- Review migration history in `__EFMigrationsHistory` table

## Performance Considerations

- **PostgreSQL**: Best for production environments with high throughput and advanced features
- **MySQL / MariaDB**: Excellent for web applications, good performance at scale, wide hosting support
- **SQL Server**: Enterprise-grade with advanced indexing, query optimization, and integration
- **SQLite**: Suitable for development and small deployments

## Example: Complete Setup

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

var eventStoreConnection = builder.Configuration.GetConnectionString("EventStore");

// Add EventStore with auto-detected provider
builder.Services.AddEfCoreEventStore(eventStoreConnection);

// Ensure database is migrated
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.EnsureEventStoreDatabaseAsync();
}

app.Run();
```

## Migration Snapshot Updates

When switching database providers, you may need to update migration snapshots:

1. The base `EventStoreDbContext` configuration is provider-agnostic
2. Provider-specific configurations are applied at runtime
3. Migration snapshots contain provider-specific metadata
4. Consider creating separate migration files per provider if needed

## Related Documentation

- [EntityFramework Core Providers](https://docs.microsoft.com/en-us/ef/core/providers/)
- [Design-Time DbContext Creation](https://docs.microsoft.com/en-us/ef/core/cli/dbcontext-creation)
- [Relay Event Sourcing Overview](./README.md)

## Support

For issues or questions:

1. Check the troubleshooting section above
2. Review unit tests in `tests/Relay.Core.Tests/EventSourcing/Database/`
3. File an issue on the [GitHub repository](https://github.com/genc-murat/relay)
