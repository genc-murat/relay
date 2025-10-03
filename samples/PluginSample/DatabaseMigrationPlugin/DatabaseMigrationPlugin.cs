using Relay.CLI.Plugins;
using System.Text;
using System.Text.Json;

namespace DatabaseMigrationPlugin;

/// <summary>
/// A plugin that manages database migrations
/// </summary>
[RelayPlugin("db-migration", "1.0.0")]
public class DatabaseMigrationPlugin : IRelayPlugin
{
    public string Name => "Database Migration";
    public string Version => "1.0.0";
    public string Description => "Manages database migrations for your Relay applications";
    public string[] Authors => new[] { "Relay Team" };
    public string[] Tags => new[] { "database", "migration", "schema", "ef-core" };
    public string MinimumRelayVersion => "2.1.0";

    private IPluginContext? _context;
    private string _migrationsPath = "";

    public async Task<bool> InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {
        _context = context;
        _migrationsPath = Path.Combine(context.WorkingDirectory, "Migrations");
        
        if (!await _context.FileSystem.DirectoryExistsAsync(_migrationsPath))
        {
            await _context.FileSystem.CreateDirectoryAsync(_migrationsPath);
        }

        _context.Logger.LogInformation("Database Migration Plugin initialized");
        return true;
    }

    public async Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (_context == null)
        {
            Console.WriteLine("Plugin not initialized");
            return 1;
        }

        if (args.Length == 0)
        {
            Console.WriteLine(GetHelp());
            return 0;
        }

        try
        {
            var command = args[0].ToLower();

            return command switch
            {
                "create" => await CreateMigrationAsync(args.Skip(1).ToArray()),
                "up" => await ApplyMigrationsAsync(args.Skip(1).ToArray()),
                "down" => await RollbackMigrationAsync(args.Skip(1).ToArray()),
                "list" => await ListMigrationsAsync(),
                "status" => await ShowMigrationStatusAsync(),
                "reset" => await ResetDatabaseAsync(),
                _ => HandleUnknownCommand(command)
            };
        }
        catch (Exception ex)
        {
            _context.Logger.LogError($"Migration failed: {ex.Message}", ex);
            return 1;
        }
    }

    public Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        _context?.Logger.LogInformation("Database Migration Plugin cleaned up");
        return Task.CompletedTask;
    }

    public string GetHelp()
    {
        return @"
Database Migration Plugin - Manage database migrations

Usage:
  relay plugin run db-migration <command> [options]

Commands:
  create <name>        Create a new migration
  up                   Apply all pending migrations
  down [steps]         Rollback migrations (default: 1 step)
  list                 List all migrations
  status               Show migration status
  reset                Reset database (WARNING: destroys all data)

Options:
  --connection <conn>  Database connection string
  --provider <name>    Database provider (sqlserver, postgres, mysql, sqlite)
  --target <version>   Target specific migration version

Examples:
  # Create a new migration
  relay plugin run db-migration create AddProductTable

  # Apply all pending migrations
  relay plugin run db-migration up

  # Rollback last migration
  relay plugin run db-migration down

  # Rollback 3 migrations
  relay plugin run db-migration down 3

  # List all migrations
  relay plugin run db-migration list

  # Check migration status
  relay plugin run db-migration status
";
    }

    private async Task<int> CreateMigrationAsync(string[] args)
    {
        if (_context == null) return 1;

        if (args.Length == 0)
        {
            _context.Logger.LogError("Migration name is required");
            return 1;
        }

        var migrationName = args[0];
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var fileName = $"{timestamp}_{migrationName}";

        var migration = new Migration
        {
            Id = Guid.NewGuid(),
            Name = migrationName,
            FileName = fileName,
            Timestamp = timestamp,
            CreatedAt = DateTime.UtcNow
        };

        // Generate migration file
        var migrationContent = GenerateMigrationFile(migration);
        var filePath = Path.Combine(_migrationsPath, $"{fileName}.cs");
        
        await _context.FileSystem.WriteFileAsync(filePath, migrationContent);

        // Update migration history
        await AddToMigrationHistoryAsync(migration);

        _context.Logger.LogInformation($"✅ Created migration: {fileName}");
        _context.Logger.LogInformation($"   File: {filePath}");
        
        return 0;
    }

    private async Task<int> ApplyMigrationsAsync(string[] args)
    {
        if (_context == null) return 1;

        var migrations = await GetPendingMigrationsAsync();

        if (!migrations.Any())
        {
            _context.Logger.LogInformation("No pending migrations");
            return 0;
        }

        _context.Logger.LogInformation($"Applying {migrations.Count} migration(s)...");

        foreach (var migration in migrations)
        {
            _context.Logger.LogInformation($"  Applying: {migration.Name}");
            
            // Simulate applying migration
            await Task.Delay(500);
            
            migration.AppliedAt = DateTime.UtcNow;
            await UpdateMigrationHistoryAsync(migration);
            
            _context.Logger.LogInformation($"  ✅ Applied: {migration.Name}");
        }

        _context.Logger.LogInformation($"✅ Successfully applied {migrations.Count} migration(s)");
        return 0;
    }

    private async Task<int> RollbackMigrationAsync(string[] args)
    {
        if (_context == null) return 1;

        var steps = 1;
        if (args.Length > 0 && int.TryParse(args[0], out var parsedSteps))
        {
            steps = parsedSteps;
        }

        var appliedMigrations = (await GetAllMigrationsAsync())
            .Where(m => m.AppliedAt.HasValue)
            .OrderByDescending(m => m.Timestamp)
            .Take(steps)
            .ToList();

        if (!appliedMigrations.Any())
        {
            _context.Logger.LogInformation("No migrations to rollback");
            return 0;
        }

        _context.Logger.LogInformation($"Rolling back {appliedMigrations.Count} migration(s)...");

        foreach (var migration in appliedMigrations)
        {
            _context.Logger.LogInformation($"  Rolling back: {migration.Name}");
            
            // Simulate rolling back migration
            await Task.Delay(500);
            
            migration.AppliedAt = null;
            await UpdateMigrationHistoryAsync(migration);
            
            _context.Logger.LogInformation($"  ✅ Rolled back: {migration.Name}");
        }

        _context.Logger.LogInformation($"✅ Successfully rolled back {appliedMigrations.Count} migration(s)");
        return 0;
    }

    private async Task<int> ListMigrationsAsync()
    {
        if (_context == null) return 1;

        var migrations = await GetAllMigrationsAsync();

        if (!migrations.Any())
        {
            _context.Logger.LogInformation("No migrations found");
            return 0;
        }

        Console.WriteLine("\nMigrations:");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine($"{"Status",-10} {"Timestamp",-20} {"Name",-30}");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        foreach (var migration in migrations.OrderBy(m => m.Timestamp))
        {
            var status = migration.AppliedAt.HasValue ? "✅ Applied" : "⏳ Pending";
            Console.WriteLine($"{status,-10} {migration.Timestamp,-20} {migration.Name,-30}");
        }

        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

        return 0;
    }

    private async Task<int> ShowMigrationStatusAsync()
    {
        if (_context == null) return 1;

        var migrations = await GetAllMigrationsAsync();
        var applied = migrations.Count(m => m.AppliedAt.HasValue);
        var pending = migrations.Count(m => !m.AppliedAt.HasValue);

        Console.WriteLine("\n📊 Migration Status");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine($"Total migrations:   {migrations.Count}");
        Console.WriteLine($"Applied:            {applied}");
        Console.WriteLine($"Pending:            {pending}");
        
        if (applied > 0)
        {
            var lastApplied = migrations
                .Where(m => m.AppliedAt.HasValue)
                .OrderByDescending(m => m.Timestamp)
                .FirstOrDefault();
            
            if (lastApplied != null)
            {
                Console.WriteLine($"Last applied:       {lastApplied.Name}");
                Console.WriteLine($"Applied at:         {lastApplied.AppliedAt:yyyy-MM-dd HH:mm:ss}");
            }
        }

        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

        return 0;
    }

    private async Task<int> ResetDatabaseAsync()
    {
        if (_context == null) return 1;

        _context.Logger.LogWarning("⚠️  WARNING: This will reset the database and destroy all data!");
        _context.Logger.LogInformation("This operation cannot be undone.");

        // In a real implementation, this would actually reset the database
        await Task.Delay(1000);

        _context.Logger.LogInformation("✅ Database reset complete");
        return 0;
    }

    private int HandleUnknownCommand(string command)
    {
        if (_context == null) return 1;

        _context.Logger.LogError($"Unknown command: {command}");
        Console.WriteLine(GetHelp());
        return 1;
    }

    private string GenerateMigrationFile(Migration migration)
    {
        return $@"using Microsoft.EntityFrameworkCore.Migrations;

namespace MyApp.Migrations;

/// <summary>
/// Migration: {migration.Name}
/// Created: {migration.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC
/// </summary>
public partial class {migration.Name} : Microsoft.EntityFrameworkCore.Migrations.Migration
{{
    protected override void Up(MigrationBuilder migrationBuilder)
    {{
        // TODO: Add your migration logic here
        // Example:
        // migrationBuilder.CreateTable(
        //     name: ""Products"",
        //     columns: table => new
        //     {{
        //         Id = table.Column<Guid>(nullable: false),
        //         Name = table.Column<string>(maxLength: 200, nullable: false),
        //         Price = table.Column<decimal>(type: ""decimal(18,2)"", nullable: false),
        //         CreatedAt = table.Column<DateTime>(nullable: false)
        //     }},
        //     constraints: table =>
        //     {{
        //         table.PrimaryKey(""PK_Products"", x => x.Id);
        //     }});
    }}

    protected override void Down(MigrationBuilder migrationBuilder)
    {{
        // TODO: Add your rollback logic here
        // Example:
        // migrationBuilder.DropTable(name: ""Products"");
    }}
}}
";
    }

    private async Task<List<Migration>> GetAllMigrationsAsync()
    {
        if (_context == null) return new List<Migration>();

        var historyFile = Path.Combine(_migrationsPath, "migration-history.json");
        
        if (!await _context.FileSystem.FileExistsAsync(historyFile))
        {
            return new List<Migration>();
        }

        var json = await _context.FileSystem.ReadFileAsync(historyFile);
        return JsonSerializer.Deserialize<List<Migration>>(json) ?? new List<Migration>();
    }

    private async Task<List<Migration>> GetPendingMigrationsAsync()
    {
        var allMigrations = await GetAllMigrationsAsync();
        return allMigrations.Where(m => !m.AppliedAt.HasValue).OrderBy(m => m.Timestamp).ToList();
    }

    private async Task AddToMigrationHistoryAsync(Migration migration)
    {
        var migrations = await GetAllMigrationsAsync();
        migrations.Add(migration);
        await SaveMigrationHistoryAsync(migrations);
    }

    private async Task UpdateMigrationHistoryAsync(Migration migration)
    {
        var migrations = await GetAllMigrationsAsync();
        var existing = migrations.FirstOrDefault(m => m.Id == migration.Id);
        
        if (existing != null)
        {
            existing.AppliedAt = migration.AppliedAt;
            await SaveMigrationHistoryAsync(migrations);
        }
    }

    private async Task SaveMigrationHistoryAsync(List<Migration> migrations)
    {
        if (_context == null) return;

        var historyFile = Path.Combine(_migrationsPath, "migration-history.json");
        var json = JsonSerializer.Serialize(migrations, new JsonSerializerOptions { WriteIndented = true });
        await _context.FileSystem.WriteFileAsync(historyFile, json);
    }
}

internal class Migration
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Timestamp { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? AppliedAt { get; set; }
}
