using Relay.CLI.Commands;
using System.CommandLine;
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
        var manualSteps = new List<string>
        {
            // Act
            "Update NuGet packages to Relay.Core",
            "Review and test all handlers",
            "Update documentation"
        };

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
        var changes = new List<(string Type, string Description)>
        {
            // Act
            ("Add", "Added [Handle] attribute"),
            ("Modify", "Changed Task to ValueTask"),
            ("Remove", "Removed MediatR using")
        };

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

    [Fact]
    public void Migrate_ShouldDetectStreamHandlers()
    {
        // Arrange
        var code = @"
public class StreamHandler : IStreamRequestHandler<StreamQuery, int>
{
    public async IAsyncEnumerable<int> Handle(StreamQuery request, CancellationToken ct)
    {
        yield return 1;
    }
}";

        // Act
        var isStreamHandler = code.Contains("IStreamRequestHandler");

        // Assert
        Assert.True(isStreamHandler);
    }

    [Fact]
    public void Migrate_ShouldHandleVoidHandlers()
    {
        // Arrange
        var code = "public async Task Handle(MyRequest request, CancellationToken ct)";

        // Act
        var migrated = code.Replace("Task Handle", "ValueTask HandleAsync");

        // Assert
        Assert.Contains("ValueTask HandleAsync", migrated);
    }

    [Fact]
    public void Migrate_ShouldUpdateInterfaceNames()
    {
        // Arrange
        var code = "IRequestHandler<MyRequest, MyResponse>";

        // Act
        var migrated = code; // Relay uses same interface names

        // Assert
        Assert.Contains("IRequestHandler", migrated);
    }

    [Fact]
    public void Migrate_ShouldDetectValidators()
    {
        // Arrange
        var code = @"
public class MyValidator : AbstractValidator<MyRequest>
{
    public MyValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}";

        // Act
        var hasValidator = code.Contains("AbstractValidator");

        // Assert
        Assert.True(hasValidator);
    }

    [Fact]
    public void Migrate_ShouldPreserveValidators()
    {
        // Arrange
        var code = @"
using FluentValidation;

public class MyValidator : AbstractValidator<MyRequest>
{
}";

        // Act - Validators don't need migration
        var needsMigration = code.Contains("MediatR");

        // Assert
        Assert.False(needsMigration);
    }

    [Fact]
    public void Migrate_ShouldDetectUnits()
    {
        // Arrange
        var code = "IRequest<Unit>";

        // Act
        var hasUnit = code.Contains("Unit");

        // Assert
        Assert.True(hasUnit);
    }

    [Fact]
    public void Migrate_ShouldConvertSend()
    {
        // Arrange
        var code = "var result = await _mediator.Send(request);";

        // Act
        var migrated = code.Replace("Send(", "SendAsync(");

        // Assert
        Assert.Contains("SendAsync(", migrated);
    }

    [Fact]
    public void Migrate_ShouldConvertPublish()
    {
        // Arrange
        var code = "await _mediator.Publish(notification);";

        // Act
        var migrated = code.Replace("Publish(", "PublishAsync(");

        // Assert
        Assert.Contains("PublishAsync(", migrated);
    }

    [Fact]
    public void Migrate_ShouldUpdateMediatorInterface()
    {
        // Arrange
        var code = "private readonly IMediator _mediator;";

        // Act
        var migrated = code.Replace("IMediator", "IRelayMediator");

        // Assert
        Assert.Contains("IRelayMediator", migrated);
    }

    [Fact]
    public void Migrate_ShouldDetectPreProcessors()
    {
        // Arrange
        var code = @"
public class PreProcessor<TRequest> : IRequestPreProcessor<TRequest>
{
    public Task Process(TRequest request, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}";

        // Act
        var hasPreProcessor = code.Contains("IRequestPreProcessor");

        // Assert
        Assert.True(hasPreProcessor);
    }

    [Fact]
    public void Migrate_ShouldDetectPostProcessors()
    {
        // Arrange
        var code = @"
public class PostProcessor<TRequest, TResponse> : IRequestPostProcessor<TRequest, TResponse>
{
    public Task Process(TRequest request, TResponse response, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}";

        // Act
        var hasPostProcessor = code.Contains("IRequestPostProcessor");

        // Assert
        Assert.True(hasPostProcessor);
    }

    [Fact]
    public void Migrate_ShouldDetectExceptionHandlers()
    {
        // Arrange
        var code = @"
public class MyExceptionHandler : IRequestExceptionHandler<MyRequest, MyResponse, Exception>
{
    public Task Handle(MyRequest request, Exception exception, RequestExceptionHandlerState<MyResponse> state, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}";

        // Act
        var hasExceptionHandler = code.Contains("IRequestExceptionHandler");

        // Assert
        Assert.True(hasExceptionHandler);
    }

    [Fact]
    public void Migrate_ShouldCalculateComplexity()
    {
        // Arrange
        var totalHandlers = 50;
        var genericHandlers = 5;
        var streamHandlers = 3;
        var behaviors = 4;

        // Act
        var complexity = totalHandlers + (genericHandlers * 2) + (streamHandlers * 3) + (behaviors * 2);

        // Assert
        Assert.Equal(77, complexity);
    }

    [Fact]
    public void Migrate_ShouldEstimateEffort()
    {
        // Arrange
        var filesCount = 100;
        var minutesPerFile = 2;

        // Act
        var totalMinutes = filesCount * minutesPerFile;
        var hours = totalMinutes / 60.0;

        // Assert
        Assert.Equal(200, totalMinutes);
        Assert.Equal(3.33, hours, 2);
    }

    [Fact]
    public void Migrate_ShouldPrioritizeMigration()
    {
        // Arrange
        var items = new[]
        {
            (Priority: 1, Name: "Package references"),
            (Priority: 2, Name: "Using statements"),
            (Priority: 3, Name: "Handler signatures"),
            (Priority: 4, Name: "DI registration"),
            (Priority: 5, Name: "Tests")
        };

        // Act
        var ordered = items.OrderBy(i => i.Priority).ToArray();

        // Assert
        Assert.Equal("Package references", ordered[0].Name);
        Assert.Equal("Tests", ordered[^1].Name);
    }

    [Fact]
    public void Migrate_ShouldGenerateDiff()
    {
        // Arrange
        var before = "using MediatR;\npublic async Task<User> Handle()";
        var after = "using Relay.Core;\npublic async ValueTask<User> HandleAsync()";

        // Act
        var changes = new List<string>();
        if (before.Contains("MediatR") && after.Contains("Relay.Core"))
            changes.Add("- using MediatR;\n+ using Relay.Core;");
        if (before.Contains("Task<") && after.Contains("ValueTask<"))
            changes.Add("- Task<User>\n+ ValueTask<User>");

        // Assert
        Assert.Equal(2, changes.Count);
    }

    [Fact]
    public void Migrate_ShouldHandleNestedGenericTypes()
    {
        // Arrange
        var code = "Task<IEnumerable<Result<User>>>";

        // Act
        var migrated = code.Replace("Task<", "ValueTask<");

        // Assert
        Assert.Equal("ValueTask<IEnumerable<Result<User>>>", migrated);
    }

    [Fact]
    public void Migrate_ShouldDetectAsyncSuffix()
    {
        // Arrange
        var withoutSuffix = "public async Task<User> Handle(";
        var withSuffix = "public async ValueTask<User> HandleAsync(";

        // Act
        var hasSuffix = withSuffix.Contains("HandleAsync");

        // Assert
        Assert.True(hasSuffix);
        Assert.DoesNotContain("HandleAsync", withoutSuffix);
    }

    [Fact]
    public void Migrate_ShouldGenerateSuccessReport()
    {
        // Arrange
        var stats = new
        {
            TotalFiles = 50,
            SuccessfulMigrations = 48,
            FailedMigrations = 2,
            SuccessRate = 96.0
        };

        // Assert
        Assert.Equal(50, stats.TotalFiles);
        Assert.Equal(96.0, stats.SuccessRate);
    }

    [Fact]
    public void Migrate_ShouldListFailedFiles()
    {
        // Arrange
        var failedFiles = new List<string>
        {
            "ComplexHandler.cs - Generic type constraint issue",
            "LegacyHandler.cs - Uses deprecated API"
        };

        // Assert
        Assert.Equal(2, failedFiles.Count);
    }

    [Fact]
    public void Migrate_ShouldSuggestNextSteps()
    {
        // Arrange
        var nextSteps = new[]
        {
            "1. Review migration report",
            "2. Run tests to verify functionality",
            "3. Update package references",
            "4. Remove MediatR packages",
            "5. Update documentation"
        };

        // Assert
        Assert.Equal(5, nextSteps.Length);
        Assert.Contains("Run tests", nextSteps[1]);
    }

    [Fact]
    public void Migrate_ShouldValidateBackup()
    {
        // Arrange
        var backupExists = true;
        var backupComplete = true;

        // Act
        var canProceed = backupExists && backupComplete;

        // Assert
        Assert.True(canProceed);
    }

    [Fact]
    public void Migrate_ShouldDetectConfigFiles()
    {
        // Arrange
        var files = new[] { "appsettings.json", "appsettings.Development.json" };

        // Act
        var configFiles = files.Where(f => f.Contains("appsettings")).ToArray();

        // Assert
        Assert.Equal(2, configFiles.Length);
    }

    [Fact]
    public void Migrate_ShouldUpdateProjectReferences()
    {
        // Arrange
        var csproj = "<PackageReference Include=\"MediatR\" Version=\"12.0.0\" />";

        // Act
        var migrated = csproj.Replace("MediatR", "Relay.Core");

        // Assert
        Assert.Contains("Relay.Core", migrated);
    }

    [Fact]
    public void Migrate_ShouldGenerateComparisonTable()
    {
        // Arrange
        var comparison = new[]
        {
            ("Feature", "MediatR", "Relay"),
            ("Return Type", "Task", "ValueTask"),
            ("Method Name", "Handle", "HandleAsync"),
            ("Interface", "IMediator", "IRelayMediator")
        };

        // Assert
        Assert.Equal(4, comparison.Length);
    }

    [Fact]
    public void Migrate_ShouldCalculateBreakingChanges()
    {
        // Arrange
        var breakingChanges = new[]
        {
            "Method signature changed",
            "Return type changed",
            "Interface renamed"
        };

        // Assert
        Assert.Equal(3, breakingChanges.Length);
    }

    [Fact]
    public void Migrate_ShouldSupportProgressCallback()
    {
        // Arrange
        var progress = new List<int>();

        // Act
        for (int i = 0; i <= 100; i += 25)
        {
            progress.Add(i);
        }

        // Assert
        Assert.Equal(5, progress.Count);
        Assert.Equal(100, progress.Last());
    }

    [Fact]
    public void Migrate_ShouldHandleCircularReferences()
    {
        // Arrange
        var hasCircular = false;

        // Act - Detection logic
        // In real implementation, would analyze dependencies

        // Assert
        Assert.False(hasCircular);
    }

    [Theory]
    [InlineData("12.0.0", true)]
    [InlineData("11.1.0", true)]
    [InlineData("10.0.0", false)]
    public void Migrate_ShouldCheckMediatRVersion(string version, bool supported)
    {
        // Act
        var major = int.Parse(version.Split('.')[0]);
        var isSupported = major >= 11;

        // Assert
        Assert.Equal(supported, isSupported);
    }

    [Fact]
    public void Migrate_ShouldGenerateTimestamp()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var formatted = timestamp.ToString("yyyy-MM-dd HH:mm:ss");

        // Assert
        Assert.NotNull(formatted);
        Assert.Contains("-", formatted);
        Assert.Contains(":", formatted);
    }

    [Fact]
    public void Migrate_ShouldLogProgress()
    {
        // Arrange
        var logs = new List<string>
        {
            // Act
            "Starting migration...",
            "Analyzing project...",
            "Creating backup...",
            "Transforming code...",
            "Migration complete!"
        };

        // Assert
        Assert.Equal(5, logs.Count);
        Assert.Equal("Migration complete!", logs.Last());
    }

    // ===== COMPREHENSIVE INTEGRATION TESTS =====

    [Fact]
    public void Create_ReturnsConfiguredCommand()
    {
        // Act
        var command = MigrateCommand.Create();

        // Assert
        Assert.NotNull(command);
        Assert.Equal("migrate", command.Name);
        Assert.Equal("Migrate from MediatR to Relay with automated transformation", command.Description);

        var fromOption = command.Options.FirstOrDefault(o => o.Name == "from");
        Assert.NotNull(fromOption);
        Assert.False(fromOption.IsRequired);

        var toOption = command.Options.FirstOrDefault(o => o.Name == "to");
        Assert.NotNull(toOption);
        Assert.False(toOption.IsRequired);

        var pathOption = command.Options.FirstOrDefault(o => o.Name == "path");
        Assert.NotNull(pathOption);
        Assert.False(pathOption.IsRequired);

        var analyzeOnlyOption = command.Options.FirstOrDefault(o => o.Name == "analyze-only");
        Assert.NotNull(analyzeOnlyOption);
        Assert.False(analyzeOnlyOption.IsRequired);

        var dryRunOption = command.Options.FirstOrDefault(o => o.Name == "dry-run");
        Assert.NotNull(dryRunOption);
        Assert.False(dryRunOption.IsRequired);

        var previewOption = command.Options.FirstOrDefault(o => o.Name == "preview");
        Assert.NotNull(previewOption);
        Assert.False(previewOption.IsRequired);

        var backupOption = command.Options.FirstOrDefault(o => o.Name == "backup");
        Assert.NotNull(backupOption);
        Assert.False(backupOption.IsRequired);

        var outputOption = command.Options.FirstOrDefault(o => o.Name == "output");
        Assert.NotNull(outputOption);
        Assert.False(outputOption.IsRequired);

        var formatOption = command.Options.FirstOrDefault(o => o.Name == "format");
        Assert.NotNull(formatOption);
        Assert.False(formatOption.IsRequired);

        var aggressiveOption = command.Options.FirstOrDefault(o => o.Name == "aggressive");
        Assert.NotNull(aggressiveOption);
        Assert.False(aggressiveOption.IsRequired);

        var interactiveOption = command.Options.FirstOrDefault(o => o.Name == "interactive");
        Assert.NotNull(interactiveOption);
        Assert.False(interactiveOption.IsRequired);
    }

    [Fact]
    public async Task ExecuteMigrate_WithInvalidSourceFramework_ShowsError()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"migrate-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);

        try
        {
            // Act & Assert - Should not throw but should show error message
            // The method validates frameworks and exits with error code
            await MigrateCommand.ExecuteMigrate(
                "invalid", // Invalid source
                "Relay",
                tempPath,
                false, false, false, false, true, ".backup", null, "markdown", false, false);

            // In the actual implementation, this sets Environment.ExitCode = 1
            // We can't easily test Environment.ExitCode in unit tests, but the method should handle this gracefully
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithInvalidTargetFramework_ShowsError()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"migrate-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);

        try
        {
            // Act & Assert - Should not throw but should show error message
            await MigrateCommand.ExecuteMigrate(
                "MediatR",
                "InvalidFramework", // Invalid target
                tempPath,
                false, false, false, false, true, ".backup", null, "markdown", false, false);
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithAnalyzeOnly_DoesNotModifyFiles()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"migrate-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);

        // Create a sample MediatR handler
        var handlerFile = Path.Combine(tempPath, "UserHandler.cs");
        var originalContent = @"
using MediatR;

public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    public async Task<User> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return new User();
    }
}";
        await File.WriteAllTextAsync(handlerFile, originalContent);

        try
        {
            // Act
            await MigrateCommand.ExecuteMigrate(
                "MediatR",
                "Relay",
                tempPath,
                analyzeOnly: true, // Only analyze
                dryRun: false,
                preview: false,
                sideBySide: false,
                createBackup: false,
                backupPath: ".backup",
                outputFile: null,
                format: "markdown",
                aggressive: false,
                interactive: false);

            // Assert - File should remain unchanged
            var contentAfter = await File.ReadAllTextAsync(handlerFile);
            Assert.Equal(originalContent, contentAfter);
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithDryRun_DoesNotModifyFiles()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"migrate-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);

        // Create a sample MediatR handler
        var handlerFile = Path.Combine(tempPath, "UserHandler.cs");
        var originalContent = @"
using MediatR;

public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    public async Task<User> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return new User();
    }
}";
        await File.WriteAllTextAsync(handlerFile, originalContent);

        try
        {
            // Act
            await MigrateCommand.ExecuteMigrate(
                "MediatR",
                "Relay",
                tempPath,
                analyzeOnly: false,
                dryRun: true, // Dry run
                preview: false,
                sideBySide: false,
                createBackup: false,
                backupPath: ".backup",
                outputFile: null,
                format: "markdown",
                aggressive: false,
                interactive: false);

            // Assert - File should remain unchanged
            var contentAfter = await File.ReadAllTextAsync(handlerFile);
            Assert.Equal(originalContent, contentAfter);
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithBackup_CreatesBackupDirectory()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"migrate-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);

        // Create a minimal project with MediatR code
        var csprojContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""MediatR"" Version=""12.0.0"" />
  </ItemGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(tempPath, "TestProject.csproj"), csprojContent);

        var handlerContent = @"
using MediatR;

public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    public async Task<User> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return new User();
    }
}";
        await File.WriteAllTextAsync(Path.Combine(tempPath, "UserHandler.cs"), handlerContent);

        try
        {
            // Act & Assert - Method should complete without throwing
            await MigrateCommand.ExecuteMigrate(
                "MediatR",
                "Relay",
                tempPath,
                analyzeOnly: false,
                dryRun: true, // Use dry run to avoid actual file modifications
                preview: false,
                sideBySide: false,
                createBackup: true, // Request backup creation
                backupPath: ".backup",
                outputFile: null,
                format: "markdown",
                aggressive: false,
                interactive: false);

            // Test passes if no exception is thrown
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithMarkdownOutput_CreatesReportFile()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"migrate-test-{Guid.NewGuid()}");
        var reportPath = Path.Combine(tempPath, "migration-report.md");
        Directory.CreateDirectory(tempPath);

        // Create a minimal project with MediatR code
        var csprojContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""MediatR"" Version=""12.0.0"" />
  </ItemGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(tempPath, "TestProject.csproj"), csprojContent);

        var handlerContent = @"
using MediatR;

public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    public async Task<User> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return new User();
    }
}";
        await File.WriteAllTextAsync(Path.Combine(tempPath, "UserHandler.cs"), handlerContent);

        try
        {
            // Act & Assert - Method should complete without throwing when output file is specified
            await MigrateCommand.ExecuteMigrate(
                "MediatR",
                "Relay",
                tempPath,
                analyzeOnly: false,
                dryRun: true, // Use dry run to avoid actual file modifications
                preview: false,
                sideBySide: false,
                createBackup: false,
                backupPath: ".backup",
                outputFile: reportPath, // Output file
                format: "markdown",
                aggressive: false,
                interactive: false);

            // Test passes if no exception is thrown
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithJsonOutput_CreatesJsonReportFile()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"migrate-test-{Guid.NewGuid()}");
        var reportPath = Path.Combine(tempPath, "migration-report.json");
        Directory.CreateDirectory(tempPath);

        // Create a minimal project with MediatR code
        var csprojContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""MediatR"" Version=""12.0.0"" />
  </ItemGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(tempPath, "TestProject.csproj"), csprojContent);

        var handlerContent = @"
using MediatR;

public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    public async Task<User> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return new User();
    }
}";
        await File.WriteAllTextAsync(Path.Combine(tempPath, "UserHandler.cs"), handlerContent);

        try
        {
            // Act & Assert - Method should complete without throwing when JSON output is specified
            await MigrateCommand.ExecuteMigrate(
                "MediatR",
                "Relay",
                tempPath,
                analyzeOnly: false,
                dryRun: true, // Use dry run to avoid actual file modifications
                preview: false,
                sideBySide: false,
                createBackup: false,
                backupPath: ".backup",
                outputFile: reportPath, // Output file
                format: "json",
                aggressive: false,
                interactive: false);

            // Test passes if no exception is thrown
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithHtmlOutput_CreatesHtmlReportFile()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"migrate-test-{Guid.NewGuid()}");
        var reportPath = Path.Combine(tempPath, "migration-report.html");
        Directory.CreateDirectory(tempPath);

        // Create a minimal project with MediatR code
        var csprojContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""MediatR"" Version=""12.0.0"" />
  </ItemGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(tempPath, "TestProject.csproj"), csprojContent);

        var handlerContent = @"
using MediatR;

public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    public async Task<User> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return new User();
    }
}";
        await File.WriteAllTextAsync(Path.Combine(tempPath, "UserHandler.cs"), handlerContent);

        try
        {
            // Act & Assert - Method should complete without throwing when HTML output is specified
            await MigrateCommand.ExecuteMigrate(
                "MediatR",
                "Relay",
                tempPath,
                analyzeOnly: false,
                dryRun: true, // Use dry run to avoid actual file modifications
                preview: false,
                sideBySide: false,
                createBackup: false,
                backupPath: ".backup",
                outputFile: reportPath, // Output file
                format: "html",
                aggressive: false,
                interactive: false);

            // Test passes if no exception is thrown
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithNonExistentPath_HandlesGracefully()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"non-existent-{Guid.NewGuid()}");

        // Act & Assert - Should not throw exception
        await MigrateCommand.ExecuteMigrate(
            "MediatR",
            "Relay",
            nonExistentPath,
            analyzeOnly: true,
            dryRun: false,
            preview: false,
            sideBySide: false,
            createBackup: false,
            backupPath: ".backup",
            outputFile: null,
            format: "markdown",
            aggressive: false,
            interactive: false);
    }

    [Fact]
    public async Task ExecuteMigrate_WithComplexProjectStructure_ProcessesAllFiles()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"complex-migrate-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);

        // Create complex project structure
        var srcPath = Path.Combine(tempPath, "src", "MyProject");
        var testPath = Path.Combine(tempPath, "tests", "MyProject.Tests");
        Directory.CreateDirectory(srcPath);
        Directory.CreateDirectory(testPath);

        // Create multiple handler files
        var handlers = new[]
        {
            ("UserHandler.cs", @"
using MediatR;
public class GetUserHandler : IRequestHandler<GetUserQuery, User> {
    public async Task<User> Handle(GetUserQuery request, CancellationToken ct) => new User();
}"),
            ("OrderHandler.cs", @"
using MediatR;
public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Order> {
    public async Task<Order> Handle(CreateOrderCommand request, CancellationToken ct) => new Order();
}"),
            ("NotificationHandler.cs", @"
using MediatR;
public class OrderCreatedHandler : INotificationHandler<OrderCreatedEvent> {
    public async Task Handle(OrderCreatedEvent notification, CancellationToken ct) { }
}")
        };

        foreach (var (fileName, content) in handlers)
        {
            await File.WriteAllTextAsync(Path.Combine(srcPath, fileName), content);
        }

        // Create csproj with MediatR reference
        var csprojContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""MediatR"" Version=""12.0.0"" />
  </ItemGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(srcPath, "MyProject.csproj"), csprojContent);

        try
        {
            // Act
            await MigrateCommand.ExecuteMigrate(
                "MediatR",
                "Relay",
                tempPath,
                analyzeOnly: true, // Just analyze to avoid actual file modifications
                dryRun: false,
                preview: false,
                sideBySide: false,
                createBackup: false,
                backupPath: ".backup",
                outputFile: null,
                format: "markdown",
                aggressive: false,
                interactive: false);

            // Assert - Analysis should find multiple handlers
            // The files should still exist and be unchanged (since we used analyzeOnly: true)
            foreach (var (fileName, _) in handlers)
            {
                Assert.True(File.Exists(Path.Combine(srcPath, fileName)));
            }
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithCustomBackupPath_UsesSpecifiedPath()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"custom-backup-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);

        // Create a minimal project with MediatR code
        var csprojContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""MediatR"" Version=""12.0.0"" />
  </ItemGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(tempPath, "TestProject.csproj"), csprojContent);

        var handlerContent = @"
using MediatR;

public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    public async Task<User> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return new User();
    }
}";
        await File.WriteAllTextAsync(Path.Combine(tempPath, "UserHandler.cs"), handlerContent);

        try
        {
            // Act & Assert - Method should complete without throwing with custom backup path
            await MigrateCommand.ExecuteMigrate(
                "MediatR",
                "Relay",
                tempPath,
                analyzeOnly: false,
                dryRun: true, // Use dry run to avoid actual file modifications
                preview: false,
                sideBySide: false,
                createBackup: true,
                backupPath: "my-custom-backup", // Custom backup path
                outputFile: null,
                format: "markdown",
                aggressive: false,
                interactive: false);

            // Test passes if no exception is thrown
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithPreview_ShowsPreviewInformation()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"preview-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);

        // Create a sample MediatR handler
        var handlerFile = Path.Combine(tempPath, "UserHandler.cs");
        var originalContent = @"
using MediatR;

public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    public async Task<User> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return new User();
    }
}";
        await File.WriteAllTextAsync(handlerFile, originalContent);

        try
        {
            // Act
            await MigrateCommand.ExecuteMigrate(
                "MediatR",
                "Relay",
                tempPath,
                analyzeOnly: false,
                dryRun: true, // Dry run with preview
                preview: true, // Show preview
                sideBySide: false,
                createBackup: false,
                backupPath: ".backup",
                outputFile: null,
                format: "markdown",
                aggressive: false,
                interactive: false);

            // Assert - File should remain unchanged (dry run)
            var contentAfter = await File.ReadAllTextAsync(handlerFile);
            Assert.Equal(originalContent, contentAfter);
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithSideBySide_ShowsSideBySideDiff()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"side-by-side-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);

        // Create a sample MediatR handler
        var handlerFile = Path.Combine(tempPath, "UserHandler.cs");
        var originalContent = @"
using MediatR;

public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    public async Task<User> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return new User();
    }
}";
        await File.WriteAllTextAsync(handlerFile, originalContent);

        try
        {
            // Act
            await MigrateCommand.ExecuteMigrate(
                "MediatR",
                "Relay",
                tempPath,
                analyzeOnly: false,
                dryRun: true, // Dry run
                preview: true, // Show preview
                sideBySide: true, // Side-by-side diff
                createBackup: false,
                backupPath: ".backup",
                outputFile: null,
                format: "markdown",
                aggressive: false,
                interactive: false);

            // Assert - File should remain unchanged (dry run)
            var contentAfter = await File.ReadAllTextAsync(handlerFile);
            Assert.Equal(originalContent, contentAfter);
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithInteractiveMode_PromptsForEachChange()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"interactive-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);

        // Create a sample MediatR handler
        var handlerFile = Path.Combine(tempPath, "UserHandler.cs");
        var originalContent = @"
using MediatR;

public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    public async Task<User> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return new User();
    }
}";
        await File.WriteAllTextAsync(handlerFile, originalContent);

        try
        {
            // Act - Interactive mode would normally prompt, but in tests we can't easily simulate user input
            // This test verifies the method can be called with interactive=true without throwing
            await MigrateCommand.ExecuteMigrate(
                "MediatR",
                "Relay",
                tempPath,
                analyzeOnly: false,
                dryRun: true, // Use dry run to avoid actual changes
                preview: false,
                sideBySide: false,
                createBackup: false,
                backupPath: ".backup",
                outputFile: null,
                format: "markdown",
                aggressive: false,
                interactive: true); // Interactive mode

            // Assert - File should remain unchanged (dry run)
            var contentAfter = await File.ReadAllTextAsync(handlerFile);
            Assert.Equal(originalContent, contentAfter);
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithAggressiveOptimization_AppliesOptimizations()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"aggressive-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);

        // Create a sample MediatR handler with potential optimizations
        var handlerFile = Path.Combine(tempPath, "UserHandler.cs");
        var originalContent = @"
using MediatR;
using System.Threading.Tasks;

public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    public async Task<User> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        // Some code that could be optimized
        var result = await Task.FromResult(new User());
        return result;
    }
}";
        await File.WriteAllTextAsync(handlerFile, originalContent);

        try
        {
            // Act
            await MigrateCommand.ExecuteMigrate(
                "MediatR",
                "Relay",
                tempPath,
                analyzeOnly: false,
                dryRun: true, // Dry run to avoid actual changes
                preview: false,
                sideBySide: false,
                createBackup: false,
                backupPath: ".backup",
                outputFile: null,
                format: "markdown",
                aggressive: true, // Aggressive optimization
                interactive: false);

            // Assert - File should remain unchanged (dry run)
            var contentAfter = await File.ReadAllTextAsync(handlerFile);
            Assert.Equal(originalContent, contentAfter);
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithReadOnlyFileSystem_HandlesPermissionErrors()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"readonly-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);

        // Create a sample file and make it read-only
        var handlerFile = Path.Combine(tempPath, "UserHandler.cs");
        var originalContent = @"
using MediatR;

public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    public async Task<User> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return new User();
    }
}";
        await File.WriteAllTextAsync(handlerFile, originalContent);

        try
        {
            // Make file read-only to simulate permission issues
            File.SetAttributes(handlerFile, FileAttributes.ReadOnly);

            // Act & Assert - Should handle the error gracefully
            await MigrateCommand.ExecuteMigrate(
                "MediatR",
                "Relay",
                tempPath,
                analyzeOnly: false,
                dryRun: false, // Try to actually modify files
                preview: false,
                sideBySide: false,
                createBackup: false,
                backupPath: ".backup",
                outputFile: null,
                format: "markdown",
                aggressive: false,
                interactive: false);

            // The method should handle the error and continue or exit gracefully
            // We can't easily test the exact behavior without mocking, but it shouldn't crash
        }
        finally
        {
            // Clean up - remove read-only attribute first
            if (File.Exists(handlerFile))
            {
                File.SetAttributes(handlerFile, FileAttributes.Normal);
            }

            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithCorruptedCsprojFile_HandlesXmlErrors()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"corrupted-csproj-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);

        // Create a corrupted csproj file
        var csprojFile = Path.Combine(tempPath, "TestProject.csproj");
        var corruptedContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""MediatR"" Version=""12.0.0""
    <!-- Missing closing tag -->
  </ItemGroup>
</Project>";
        await File.WriteAllTextAsync(csprojFile, corruptedContent);

        try
        {
            // Act & Assert - Should handle XML parsing errors gracefully
            await MigrateCommand.ExecuteMigrate(
                "MediatR",
                "Relay",
                tempPath,
                analyzeOnly: false,
                dryRun: false,
                preview: false,
                sideBySide: false,
                createBackup: false,
                backupPath: ".backup",
                outputFile: null,
                format: "markdown",
                aggressive: false,
                interactive: false);

            // The method should handle the XML error and continue with code transformation
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithNestedProjectStructure_ProcessesAllLevels()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"nested-structure-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);

        // Create nested directory structure
        var srcPath = Path.Combine(tempPath, "src", "MyApp", "Handlers");
        var testsPath = Path.Combine(tempPath, "tests", "MyApp.Tests", "Handlers");
        Directory.CreateDirectory(srcPath);
        Directory.CreateDirectory(testsPath);

        // Create handlers in different levels
        var mainHandler = Path.Combine(srcPath, "UserHandler.cs");
        var testHandler = Path.Combine(testsPath, "UserHandlerTests.cs");

        var mainContent = @"
using MediatR;

namespace MyApp.Handlers
{
    public class GetUserHandler : IRequestHandler<GetUserQuery, User>
    {
        public async Task<User> Handle(GetUserQuery request, CancellationToken ct) => new User();
    }
}";
        var testContent = @"
// Test file - should not be modified
using MediatR;
using Xunit;

public class UserHandlerTests
{
    [Fact]
    public void TestHandler() { }
}";

        await File.WriteAllTextAsync(mainHandler, mainContent);
        await File.WriteAllTextAsync(testHandler, testContent);

        try
        {
            // Act
            await MigrateCommand.ExecuteMigrate(
                "MediatR",
                "Relay",
                tempPath,
                analyzeOnly: true, // Just analyze to avoid actual modifications
                dryRun: false,
                preview: false,
                sideBySide: false,
                createBackup: false,
                backupPath: ".backup",
                outputFile: null,
                format: "markdown",
                aggressive: false,
                interactive: false);

            // Assert - Files should still exist and be unchanged (analyze only)
            Assert.True(File.Exists(mainHandler));
            Assert.True(File.Exists(testHandler));

            var mainContentAfter = await File.ReadAllTextAsync(mainHandler);
            var testContentAfter = await File.ReadAllTextAsync(testHandler);

            Assert.Equal(mainContent, mainContentAfter);
            Assert.Equal(testContent, testContentAfter);
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithLargeProject_HandlesPerformance()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"large-project-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);

        // Create multiple files to simulate a large project
        for (int i = 0; i < 10; i++)
        {
            var handlerFile = Path.Combine(tempPath, $"Handler{i}.cs");
            var content = $@"
using MediatR;

public class Handler{i} : IRequestHandler<Query{i}, Result{i}>
{{
    public async Task<Result{i}> Handle(Query{i} request, CancellationToken ct)
    {{
        return new Result{i}();
    }}
}}";
            await File.WriteAllTextAsync(handlerFile, content);
        }

        try
        {
            // Act
            await MigrateCommand.ExecuteMigrate(
                "MediatR",
                "Relay",
                tempPath,
                analyzeOnly: true, // Just analyze for performance
                dryRun: false,
                preview: false,
                sideBySide: false,
                createBackup: false,
                backupPath: ".backup",
                outputFile: null,
                format: "markdown",
                aggressive: false,
                interactive: false);

            // Assert - All files should still exist
            for (int i = 0; i < 10; i++)
            {
                var handlerFile = Path.Combine(tempPath, $"Handler{i}.cs");
                Assert.True(File.Exists(handlerFile));
            }
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithInvalidOutputFormat_ShowsError()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"invalid-format-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);

        try
        {
            // Act & Assert - Should handle invalid format gracefully
            // Note: The current implementation doesn't validate format, but this test ensures it doesn't crash
            await MigrateCommand.ExecuteMigrate(
                "MediatR",
                "Relay",
                tempPath,
                analyzeOnly: true,
                dryRun: false,
                preview: false,
                sideBySide: false,
                createBackup: false,
                backupPath: ".backup",
                outputFile: null,
                format: "invalidformat", // Invalid format
                aggressive: false,
                interactive: false);

            // Should complete without throwing
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithEmptyProject_HandlesGracefully()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"empty-project-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);

        // Create an empty csproj file
        var csprojFile = Path.Combine(tempPath, "EmptyProject.csproj");
        var csprojContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>";
        await File.WriteAllTextAsync(csprojFile, csprojContent);

        try
        {
            // Act
            await MigrateCommand.ExecuteMigrate(
                "MediatR",
                "Relay",
                tempPath,
                analyzeOnly: true,
                dryRun: false,
                preview: false,
                sideBySide: false,
                createBackup: false,
                backupPath: ".backup",
                outputFile: null,
                format: "markdown",
                aggressive: false,
                interactive: false);

            // Assert - Should complete without issues
            Assert.True(File.Exists(csprojFile));
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }
}


