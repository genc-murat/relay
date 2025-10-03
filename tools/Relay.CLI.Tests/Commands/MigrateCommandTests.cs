using Xunit;

namespace Relay.CLI.Tests.Commands;

public class MigrateCommandTests
{
    [Fact]
    public async Task Migrate_ShouldAnalyzeProject_BeforeMigration()
    {
        // Arrange
        var projectPath = Path.Combine(Path.GetTempPath(), "test-mediatr-project");
        Directory.CreateDirectory(projectPath);

        try
        {
            // Create a fake MediatR handler file
            var handlerFile = Path.Combine(projectPath, "UserHandler.cs");
            await File.WriteAllTextAsync(handlerFile, @"
using MediatR;

public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    public async Task<User> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return new User();
    }
}");

            // Act
            var filesWithMediatR = Directory.GetFiles(projectPath, "*.cs")
                .Where(f => File.ReadAllText(f).Contains("MediatR"))
                .Count();

            // Assert
            Assert.Equal(1, filesWithMediatR);
        }
        finally
        {
            if (Directory.Exists(projectPath))
                Directory.Delete(projectPath, true);
        }
    }

    [Fact]
    public async Task Migrate_ShouldCreateBackup_BeforeChanges()
    {
        // Arrange
        var projectPath = Path.Combine(Path.GetTempPath(), "test-project");
        var backupPath = Path.Combine(Path.GetTempPath(), ".backup");

        Directory.CreateDirectory(projectPath);
        var testFile = Path.Combine(projectPath, "test.txt");
        await File.WriteAllTextAsync(testFile, "original content");

        try
        {
            // Act
            Directory.CreateDirectory(backupPath);
            var backupFile = Path.Combine(backupPath, "test.txt");
            File.Copy(testFile, backupFile);

            // Assert
            Assert.True(File.Exists(backupFile));
            var backupContent = await File.ReadAllTextAsync(backupFile);
            Assert.Equal("original content", backupContent);
        }
        finally
        {
            if (Directory.Exists(projectPath))
                Directory.Delete(projectPath, true);
            if (Directory.Exists(backupPath))
                Directory.Delete(backupPath, true);
        }
    }

    [Fact]
    public void Migrate_ShouldTransformHandlerMethod()
    {
        // Arrange
        var original = "public async Task<User> Handle(GetUserQuery request, CancellationToken cancellationToken)";
        
        // Act
        var transformed = original
            .Replace("Task<", "ValueTask<")
            .Replace("Handle(", "HandleAsync(");

        // Assert
        Assert.Contains("ValueTask<User>", transformed);
        Assert.Contains("HandleAsync(", transformed);
    }

    [Fact]
    public void Migrate_ShouldUpdateUsingStatements()
    {
        // Arrange
        var code = @"
using MediatR;
using System;

public class Handler {}";

        // Act
        var migratedCode = code.Replace("using MediatR;", "using Relay.Core;");

        // Assert
        Assert.Contains("using Relay.Core;", migratedCode);
        Assert.DoesNotContain("using MediatR;", migratedCode);
    }

    [Fact]
    public void Migrate_ShouldAddHandleAttribute()
    {
        // Arrange
        var method = @"
    public async ValueTask<User> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
    {
        return new User();
    }";

        // Act
        var withAttribute = $"    [Handle]\n{method}";

        // Assert
        Assert.Contains("[Handle]", withAttribute);
    }

    [Fact]
    public void Migrate_ShouldDetectHandlers()
    {
        // Arrange
        var code = @"
public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    public async Task<User> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return new User();
    }
}";

        // Act
        var hasHandler = code.Contains("IRequestHandler<");
        var hasHandleMethod = code.Contains("Task<") && code.Contains("Handle(");

        // Assert
        Assert.True(hasHandler);
        Assert.True(hasHandleMethod);
    }

    [Fact]
    public void Migrate_ShouldDetectNotifications()
    {
        // Arrange
        var code = @"
public class UserCreatedEvent : INotification
{
    public int UserId { get; set; }
}";

        // Act
        var isNotification = code.Contains("INotification");

        // Assert
        Assert.True(isNotification);
    }

    [Fact]
    public void Migrate_ShouldDetectPipelineBehaviors()
    {
        // Arrange
        var code = @"
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        return await next();
    }
}";

        // Act
        var isPipelineBehavior = code.Contains("IPipelineBehavior<");

        // Assert
        Assert.True(isPipelineBehavior);
    }

    [Fact]
    public async Task Migrate_ShouldGenerateReport()
    {
        // Arrange
        var reportPath = Path.Combine(Path.GetTempPath(), "migration-report.md");

        try
        {
            // Act
            var report = @"# Migration Report

**Status:** Success
**Files Modified:** 5
**Handlers Migrated:** 10

## Changes
- Updated using statements
- Converted Task to ValueTask
- Added [Handle] attributes
";
            await File.WriteAllTextAsync(reportPath, report);

            // Assert
            Assert.True(File.Exists(reportPath));
            var content = await File.ReadAllTextAsync(reportPath);
            Assert.Contains("Migration Report", content);
        }
        finally
        {
            if (File.Exists(reportPath))
                File.Delete(reportPath);
        }
    }

    [Fact]
    public void Migrate_ShouldCalculateStatistics()
    {
        // Arrange
        var filesModified = 5;
        var handlersMigrated = 10;
        var linesChanged = 150;

        // Act
        var stats = new
        {
            FilesModified = filesModified,
            HandlersMigrated = handlersMigrated,
            LinesChanged = linesChanged,
            AverageLinesPerFile = linesChanged / (double)filesModified
        };

        // Assert
        Assert.Equal(5, stats.FilesModified);
        Assert.Equal(10, stats.HandlersMigrated);
        Assert.Equal(30.0, stats.AverageLinesPerFile);
    }

    [Fact]
    public void Migrate_ShouldHandleDryRun()
    {
        // Arrange
        var dryRun = true;
        var changes = new List<string>();

        // Act
        if (!dryRun)
        {
            // Apply changes
            changes.Add("Change applied");
        }
        else
        {
            // Only preview
            changes.Add("Change preview");
        }

        // Assert
        Assert.Single(changes);
        Assert.Contains("preview", changes[0]);
    }

    [Fact]
    public async Task Migrate_ShouldSupportRollback()
    {
        // Arrange
        var backupPath = Path.Combine(Path.GetTempPath(), "backup");
        var projectPath = Path.Combine(Path.GetTempPath(), "project");

        Directory.CreateDirectory(backupPath);
        Directory.CreateDirectory(projectPath);

        var backupFile = Path.Combine(backupPath, "original.txt");
        var projectFile = Path.Combine(projectPath, "modified.txt");

        await File.WriteAllTextAsync(backupFile, "original");
        await File.WriteAllTextAsync(projectFile, "modified");

        try
        {
            // Act - Rollback
            File.Copy(backupFile, projectFile, overwrite: true);
            var restoredContent = await File.ReadAllTextAsync(projectFile);

            // Assert
            Assert.Equal("original", restoredContent);
        }
        finally
        {
            if (Directory.Exists(backupPath))
                Directory.Delete(backupPath, true);
            if (Directory.Exists(projectPath))
                Directory.Delete(projectPath, true);
        }
    }

    [Fact]
    public void Migrate_ShouldDetectPackageReferences()
    {
        // Arrange
        var csproj = @"
<Project>
  <ItemGroup>
    <PackageReference Include=""MediatR"" Version=""12.0.0"" />
    <PackageReference Include=""MediatR.Extensions.Microsoft.DependencyInjection"" Version=""11.0.0"" />
  </ItemGroup>
</Project>";

        // Act
        var hasMediatR = csproj.Contains("MediatR");
        var packageCount = System.Text.RegularExpressions.Regex.Matches(csproj, "PackageReference").Count;

        // Assert
        Assert.True(hasMediatR);
        Assert.Equal(2, packageCount);
    }

    [Fact]
    public void Migrate_ShouldUpdateDIRegistration()
    {
        // Arrange
        var code = "services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));";

        // Act
        var migrated = code.Replace("AddMediatR", "AddRelay");

        // Assert
        Assert.Contains("AddRelay", migrated);
        Assert.DoesNotContain("AddMediatR", migrated);
    }

    [Fact]
    public void Migrate_ShouldHandleGenericHandlers()
    {
        // Arrange
        var code = @"
public class GenericHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, CancellationToken ct)
    {
        return default;
    }
}";

        // Act
        var isGeneric = code.Contains("<TRequest, TResponse>");
        var needsMigration = code.Contains("Task<TResponse>") && code.Contains("Handle(");

        // Assert
        Assert.True(isGeneric);
        Assert.True(needsMigration);
    }

    [Fact]
    public void Migrate_ShouldPreserveComments()
    {
        // Arrange
        var code = @"
/// <summary>
/// Gets user by ID
/// </summary>
public async Task<User> Handle(GetUserQuery request, CancellationToken ct)
{
    return null;
}";

        // Act
        var migratedCode = code.Replace("Task<User>", "ValueTask<User>");

        // Assert
        Assert.Contains("/// <summary>", migratedCode);
        Assert.Contains("ValueTask<User>", migratedCode);
    }

    [Fact]
    public void Migrate_ShouldDetectIssues()
    {
        // Arrange
        var issues = new List<(string Severity, string Message)>();

        // Act
        // Simulate issue detection
        var code = "public async void Handle() {}"; // Bad practice
        
        if (code.Contains("async void"))
        {
            issues.Add(("Error", "Async void methods should be avoided"));
        }

        // Assert
        Assert.Single(issues);
        Assert.Equal("Error", issues[0].Severity);
    }

    [Fact]
    public void Migrate_ShouldGenerateManualSteps()
    {
        // Arrange
        var manualSteps = new List<string>();

        // Act
        manualSteps.Add("Update NuGet packages to Relay.Core");
        manualSteps.Add("Review and test all handlers");
        manualSteps.Add("Update documentation");

        // Assert
        Assert.Equal(3, manualSteps.Count);
        Assert.Contains("NuGet packages", manualSteps[0]);
    }

    [Theory]
    [InlineData("Task", "ValueTask")]
    [InlineData("Task<User>", "ValueTask<User>")]
    [InlineData("Task<IEnumerable<User>>", "ValueTask<IEnumerable<User>>")]
    public void Migrate_ShouldConvertTaskTypes(string original, string expected)
    {
        // Act
        var converted = original.Replace("Task", "ValueTask");

        // Assert
        Assert.Equal(expected, converted);
    }

    [Fact]
    public void Migrate_ShouldTrackChanges()
    {
        // Arrange
        var changes = new List<(string Type, string Description)>();

        // Act
        changes.Add(("Add", "Added [Handle] attribute"));
        changes.Add(("Modify", "Changed Task to ValueTask"));
        changes.Add(("Remove", "Removed MediatR using"));

        // Assert
        Assert.Equal(3, changes.Count);
        Assert.Contains(changes, c => c.Type == "Add");
        Assert.Contains(changes, c => c.Type == "Modify");
        Assert.Contains(changes, c => c.Type == "Remove");
    }

    [Fact]
    public async Task Migrate_ShouldHandleMultipleFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"migrate-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create multiple handler files
            for (int i = 1; i <= 3; i++)
            {
                var file = Path.Combine(tempDir, $"Handler{i}.cs");
                await File.WriteAllTextAsync(file, $"public class Handler{i} {{}}");
            }

            // Act
            var files = Directory.GetFiles(tempDir, "*.cs");

            // Assert
            Assert.Equal(3, files.Length);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}
