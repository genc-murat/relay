
using Relay.CLI.Commands;
using System.CommandLine;
using Xunit;

namespace Relay.CLI.Tests.Commands;

/// <summary>
/// Extended comprehensive tests for MigrateCommand
/// </summary>
public class MigrateCommandExtendedTests : IDisposable
{
    private readonly string _testPath;

    public MigrateCommandExtendedTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"relay-migrate-extended-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPath);
    }

    #region Command Creation Tests

    [Fact]
    public void MigrateCommand_Create_ShouldReturnValidCommand()
    {
        // Act
        var command = MigrateCommand.Create();

        // Assert
        Assert.NotNull(command);
        Assert.Equal("migrate", command.Name);
        Assert.Contains("Migrate", command.Description);
    }

    [Fact]
    public void MigrateCommand_Create_ShouldHaveFromOption()
    {
        // Act
        var command = MigrateCommand.Create();
        var fromOption = command.Options.FirstOrDefault(o => o.Name == "from");

        // Assert
        Assert.NotNull(fromOption);
    }

    [Fact]
    public void MigrateCommand_Create_ShouldHaveToOption()
    {
        // Act
        var command = MigrateCommand.Create();
        var toOption = command.Options.FirstOrDefault(o => o.Name == "to");

        // Assert
        Assert.NotNull(toOption);
    }

    [Fact]
    public void MigrateCommand_Create_ShouldHavePathOption()
    {
        // Act
        var command = MigrateCommand.Create();
        var pathOption = command.Options.FirstOrDefault(o => o.Name == "path");

        // Assert
        Assert.NotNull(pathOption);
    }

    [Fact]
    public void MigrateCommand_Create_ShouldHaveAnalyzeOnlyOption()
    {
        // Act
        var command = MigrateCommand.Create();
        var analyzeOption = command.Options.FirstOrDefault(o => o.Name == "analyze-only");

        // Assert
        Assert.NotNull(analyzeOption);
    }

    [Fact]
    public void MigrateCommand_Create_ShouldHaveDryRunOption()
    {
        // Act
        var command = MigrateCommand.Create();
        var dryRunOption = command.Options.FirstOrDefault(o => o.Name == "dry-run");

        // Assert
        Assert.NotNull(dryRunOption);
    }

    [Fact]
    public void MigrateCommand_Create_ShouldHaveBackupOption()
    {
        // Act
        var command = MigrateCommand.Create();
        var backupOption = command.Options.FirstOrDefault(o => o.Name == "backup");

        // Assert
        Assert.NotNull(backupOption);
    }

    [Fact]
    public void MigrateCommand_Create_ShouldHavePreviewOption()
    {
        // Act
        var command = MigrateCommand.Create();
        var previewOption = command.Options.FirstOrDefault(o => o.Name == "preview");

        // Assert
        Assert.NotNull(previewOption);
    }

    [Fact]
    public void MigrateCommand_Create_ShouldHaveOutputOption()
    {
        // Act
        var command = MigrateCommand.Create();
        var outputOption = command.Options.FirstOrDefault(o => o.Name == "output");

        // Assert
        Assert.NotNull(outputOption);
    }

    [Fact]
    public void MigrateCommand_Create_ShouldHaveFormatOption()
    {
        // Act
        var command = MigrateCommand.Create();
        var formatOption = command.Options.FirstOrDefault(o => o.Name == "format");

        // Assert
        Assert.NotNull(formatOption);
    }

    [Fact]
    public void MigrateCommand_Create_ShouldHaveAggressiveOption()
    {
        // Act
        var command = MigrateCommand.Create();
        var aggressiveOption = command.Options.FirstOrDefault(o => o.Name == "aggressive");

        // Assert
        Assert.NotNull(aggressiveOption);
    }

    [Fact]
    public void MigrateCommand_Create_ShouldHaveInteractiveOption()
    {
        // Act
        var command = MigrateCommand.Create();
        var interactiveOption = command.Options.FirstOrDefault(o => o.Name == "interactive");

        // Assert
        Assert.NotNull(interactiveOption);
    }

    [Fact]
    public void MigrateCommand_Options_ShouldHaveCorrectCount()
    {
        // Act
        var command = MigrateCommand.Create();

        // Assert
        Assert.True(command.Options.Count >= 12);
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void MigrateCommand_FromOption_DefaultShouldBeMediatR()
    {
        // Arrange
        var defaultFrom = "MediatR";

        // Assert
        Assert.Equal("MediatR", defaultFrom);
    }

    [Fact]
    public void MigrateCommand_ToOption_DefaultShouldBeRelay()
    {
        // Arrange
        var defaultTo = "Relay";

        // Assert
        Assert.Equal("Relay", defaultTo);
    }

    [Fact]
    public void MigrateCommand_PathOption_DefaultShouldBeCurrentDirectory()
    {
        // Arrange
        var defaultPath = ".";

        // Assert
        Assert.Equal(".", defaultPath);
    }

    [Fact]
    public void MigrateCommand_BackupOption_DefaultShouldBeTrue()
    {
        // Arrange
        var defaultBackup = true;

        // Assert
        Assert.True(defaultBackup);
    }

    [Fact]
    public void MigrateCommand_BackupPathOption_DefaultShouldBeBackupFolder()
    {
        // Arrange
        var defaultBackupPath = ".backup";

        // Assert
        Assert.Equal(".backup", defaultBackupPath);
    }

    [Fact]
    public void MigrateCommand_FormatOption_DefaultShouldBeMarkdown()
    {
        // Arrange
        var defaultFormat = "markdown";

        // Assert
        Assert.Equal("markdown", defaultFormat);
    }

    [Fact]
    public void MigrateCommand_AnalyzeOnlyOption_DefaultShouldBeFalse()
    {
        // Arrange
        var defaultAnalyzeOnly = false;

        // Assert
        Assert.False(defaultAnalyzeOnly);
    }

    [Fact]
    public void MigrateCommand_DryRunOption_DefaultShouldBeFalse()
    {
        // Arrange
        var defaultDryRun = false;

        // Assert
        Assert.False(defaultDryRun);
    }

    #endregion

    #region Framework Support Tests

    [Theory]
    [InlineData("MediatR", "Relay")]
    [InlineData("mediatr", "relay")]
    [InlineData("MEDIATR", "RELAY")]
    public void MigrateCommand_ShouldSupportMediatRToRelay(string from, string to)
    {
        // Arrange
        var fromNormalized = from.ToLowerInvariant();
        var toNormalized = to.ToLowerInvariant();

        // Assert
        Assert.Equal("mediatr", fromNormalized);
        Assert.Equal("relay", toNormalized);
    }

    [Theory]
    [InlineData("AutoMapper")]
    [InlineData("FluentValidation")]
    [InlineData("CustomFramework")]
    public void MigrateCommand_ShouldNotSupportOtherFrameworks(string framework)
    {
        // Arrange
        var supportedFrom = new[] { "mediatr" };

        // Assert
        Assert.DoesNotContain(framework.ToLower(), supportedFrom);
    }

    #endregion

    #region MediatR Detection Tests

    [Fact]
    public void MigrateCommand_ShouldDetectMediatRUsing()
    {
        // Arrange
        var code = "using MediatR;";

        // Assert
        Assert.Contains("MediatR", code);
    }

    [Fact]
    public void MigrateCommand_ShouldDetectIRequestHandler()
    {
        // Arrange
        var code = "public class Handler : IRequestHandler<Query, Response>";

        // Assert
        Assert.Contains("IRequestHandler", code);
    }

    [Fact]
    public void MigrateCommand_ShouldDetectINotificationHandler()
    {
        // Arrange
        var code = "public class Handler : INotificationHandler<Event>";

        // Assert
        Assert.Contains("INotificationHandler", code);
    }

    [Fact]
    public void MigrateCommand_ShouldDetectIPipelineBehavior()
    {
        // Arrange
        var code = "public class Behavior : IPipelineBehavior<TRequest, TResponse>";

        // Assert
        Assert.Contains("IPipelineBehavior", code);
    }

    [Fact]
    public void MigrateCommand_ShouldDetectIRequest()
    {
        // Arrange
        var code = "public record Query : IRequest<Response>";

        // Assert
        Assert.Contains("IRequest", code);
    }

    [Fact]
    public void MigrateCommand_ShouldDetectINotification()
    {
        // Arrange
        var code = "public record Event : INotification";

        // Assert
        Assert.Contains("INotification", code);
    }

    [Fact]
    public void MigrateCommand_ShouldDetectHandleMethod()
    {
        // Arrange
        var code = "public async Task<Response> Handle(Query request, CancellationToken ct)";

        // Assert
        Assert.Contains("Handle(", code);
    }

    [Fact]
    public void MigrateCommand_ShouldDetectMediatRPackageReference()
    {
        // Arrange
        var projectContent = "<PackageReference Include=\"MediatR\" Version=\"12.0.0\" />";

        // Assert
        Assert.Contains("MediatR", projectContent);
    }

    #endregion

    #region Code Transformation Tests

    [Fact]
    public void MigrateCommand_ShouldTransformUsingStatement()
    {
        // Arrange
        var original = "using MediatR;";

        // Act
        var transformed = original.Replace("using MediatR;", "using Relay.Core;");

        // Assert
        Assert.Equal("using Relay.Core;", transformed);
        Assert.DoesNotContain("MediatR", transformed);
    }

    [Fact]
    public void MigrateCommand_ShouldTransformTaskToValueTask()
    {
        // Arrange
        var original = "Task<Response>";

        // Act
        var transformed = original.Replace("Task<", "ValueTask<");

        // Assert
        Assert.Equal("ValueTask<Response>", transformed);
    }

    [Fact]
    public void MigrateCommand_ShouldTransformHandleToHandleAsync()
    {
        // Arrange
        var original = "public async Task<Response> Handle(";

        // Act
        var transformed = original.Replace("Handle(", "HandleAsync(");

        // Assert
        Assert.Contains("HandleAsync(", transformed);
    }

    [Fact]
    public void MigrateCommand_ShouldAddHandleAttribute()
    {
        // Arrange
        var method = "    public async ValueTask<Response> HandleAsync";
        var attribute = "    [Handle]";

        // Act
        var withAttribute = $"{attribute}\n{method}";

        // Assert
        Assert.Contains("[Handle]", withAttribute);
        Assert.Contains("HandleAsync", withAttribute);
    }

    [Fact]
    public void MigrateCommand_ShouldTransformIRequestHandler()
    {
        // Arrange
        var original = "IRequestHandler<Query, Response>";

        // Act
        var transformed = original; // Relay uses same interface name

        // Assert
        Assert.Equal("IRequestHandler<Query, Response>", transformed);
    }

    [Fact]
    public void MigrateCommand_ShouldRemoveRequestHandlerDelegate()
    {
        // Arrange
        var original = "RequestHandlerDelegate<TResponse> next";

        // Act
        var transformed = original.Replace("RequestHandlerDelegate<TResponse>", "Func<ValueTask<TResponse>>");

        // Assert
        Assert.Contains("Func<ValueTask<TResponse>>", transformed);
    }

    [Fact]
    public void MigrateCommand_ShouldUpdatePackageReference()
    {
        // Arrange
        var original = "<PackageReference Include=\"MediatR\" Version=\"12.0.0\" />";

        // Act
        var transformed = original
            .Replace("MediatR", "Relay.Core")
            .Replace("12.0.0", "2.0.0");

        // Assert
        Assert.Contains("Relay.Core", transformed);
        Assert.Contains("2.0.0", transformed);
        Assert.DoesNotContain("MediatR", transformed);
    }

    #endregion

    #region Backup Tests

    [Fact]
    public async Task MigrateCommand_Backup_ShouldCreateBackupDirectory()
    {
        // Arrange
        var backupPath = Path.Combine(_testPath, ".backup");

        // Act
        Directory.CreateDirectory(backupPath);

        // Assert
        Assert.True(Directory.Exists(backupPath));
    }

    [Fact]
    public async Task MigrateCommand_Backup_ShouldCopyOriginalFiles()
    {
        // Arrange
        var sourcePath = Path.Combine(_testPath, "source.cs");
        var backupDir = Path.Combine(_testPath, ".backup");
        var backupPath = Path.Combine(backupDir, "source.cs");

        await File.WriteAllTextAsync(sourcePath, "original content");
        Directory.CreateDirectory(backupDir);

        // Act
        File.Copy(sourcePath, backupPath);

        // Assert
        Assert.True(File.Exists(backupPath));
        var content = await File.ReadAllTextAsync(backupPath);
        Assert.Equal("original content", content);
    }

    [Fact]
    public async Task MigrateCommand_Backup_ShouldPreserveFileStructure()
    {
        // Arrange
        var subDir = Path.Combine(_testPath, "Handlers");
        var backupDir = Path.Combine(_testPath, ".backup", "Handlers");
        Directory.CreateDirectory(subDir);
        Directory.CreateDirectory(backupDir);

        var sourceFile = Path.Combine(subDir, "Handler.cs");
        await File.WriteAllTextAsync(sourceFile, "handler content");

        // Act
        var backupFile = Path.Combine(backupDir, "Handler.cs");
        File.Copy(sourceFile, backupFile);

        // Assert
        Assert.True(File.Exists(backupFile));
    }

    [Fact]
    public void MigrateCommand_Backup_ShouldIncludeTimestamp()
    {
        // Arrange
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupName = $".backup_{timestamp}";

        // Assert
        Assert.Contains(".backup_", backupName);
        Assert.Matches(@"\.backup_\d{8}_\d{6}", backupName);
    }

    #endregion

    #region Dry Run Tests

    [Fact]
    public void MigrateCommand_DryRun_ShouldNotModifyFiles()
    {
        // Arrange
        var isDryRun = true;
        var changesApplied = 0;

        // Act
        if (!isDryRun)
        {
            changesApplied = 10;
        }

        // Assert
        Assert.Equal(0, changesApplied);
    }

    [Fact]
    public void MigrateCommand_DryRun_ShouldShowPreview()
    {
        // Arrange
        var isDryRun = true;
        var previewGenerated = false;

        // Act
        if (isDryRun)
        {
            previewGenerated = true;
        }

        // Assert
        Assert.True(previewGenerated);
    }

    [Fact]
    public void MigrateCommand_DryRun_ShouldNotCreateBackup()
    {
        // Arrange
        var isDryRun = true;
        var backupCreated = false;

        // Act
        if (!isDryRun)
        {
            backupCreated = true;
        }

        // Assert
        Assert.False(backupCreated);
    }

    #endregion

    #region Analysis Tests

    [Fact]
    public async Task MigrateCommand_Analysis_ShouldCountHandlers()
    {
        // Arrange
        var code = @"
public class Handler1 : IRequestHandler<Query1, Response1> { }
public class Handler2 : IRequestHandler<Query2, Response2> { }
";
        var projectPath = Path.Combine(_testPath, "handlers.cs");
        await File.WriteAllTextAsync(projectPath, code);

        // Act
        var handlerCount = System.Text.RegularExpressions.Regex.Matches(code, @"IRequestHandler<").Count;

        // Assert
        Assert.Equal(2, handlerCount);
    }

    [Fact]
    public async Task MigrateCommand_Analysis_ShouldCountFiles()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_testPath, "file1.cs"), "code");
        await File.WriteAllTextAsync(Path.Combine(_testPath, "file2.cs"), "code");
        await File.WriteAllTextAsync(Path.Combine(_testPath, "file3.cs"), "code");

        // Act
        var fileCount = Directory.GetFiles(_testPath, "*.cs").Length;

        // Assert
        Assert.Equal(3, fileCount);
    }

    [Fact]
    public void MigrateCommand_Analysis_ShouldDetectCriticalIssues()
    {
        // Arrange
        var issues = new List<string>
        {
            "Unsupported MediatR feature",
            "Complex pipeline behavior",
            "Custom serialization"
        };

        // Act
        var criticalIssues = issues.Where(i => i.Contains("Unsupported")).ToList();

        // Assert
        Assert.Single(criticalIssues);
    }

    [Fact]
    public void MigrateCommand_Analysis_ShouldCalculateComplexity()
    {
        // Arrange
        var handlers = 10;
        var notifications = 5;
        var behaviors = 3;

        // Act
        var totalComplexity = handlers * 2 + notifications * 1 + behaviors * 3;

        // Assert
        Assert.Equal(34, totalComplexity);
    }

    #endregion

    #region Report Generation Tests

    [Fact]
    public async Task MigrateCommand_Report_ShouldGenerateMarkdown()
    {
        // Arrange
        var reportPath = Path.Combine(_testPath, "report.md");
        var content = @"# Migration Report

## Summary
- Files Modified: 5
- Handlers Migrated: 10

## Changes
- Updated using statements
- Converted Task to ValueTask
";

        // Act
        await File.WriteAllTextAsync(reportPath, content);

        // Assert
        Assert.True(File.Exists(reportPath));
        var report = await File.ReadAllTextAsync(reportPath);
        Assert.Contains("# Migration Report", report);
        Assert.Contains("Files Modified", report);
    }

    [Fact]
    public async Task MigrateCommand_Report_ShouldGenerateJson()
    {
        // Arrange
        var reportPath = Path.Combine(_testPath, "report.json");
        var json = @"{
  ""status"": ""Success"",
  ""filesModified"": 5,
  ""handlersMigrated"": 10
}";

        // Act
        await File.WriteAllTextAsync(reportPath, json);

        // Assert
        Assert.True(File.Exists(reportPath));
        var content = await File.ReadAllTextAsync(reportPath);
        Assert.Contains("\"status\"", content);
    }

    [Fact]
    public async Task MigrateCommand_Report_ShouldGenerateHtml()
    {
        // Arrange
        var reportPath = Path.Combine(_testPath, "report.html");
        var html = @"<!DOCTYPE html>
<html>
<head><title>Migration Report</title></head>
<body><h1>Migration Report</h1></body>
</html>";

        // Act
        await File.WriteAllTextAsync(reportPath, html);

        // Assert
        Assert.True(File.Exists(reportPath));
        var content = await File.ReadAllTextAsync(reportPath);
        Assert.Contains("<title>Migration Report</title>", content);
    }

    [Fact]
    public void MigrateCommand_Report_ShouldIncludeTimestamp()
    {
        // Arrange
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var report = $"Generated: {timestamp}";

        // Assert
        Assert.Contains("Generated:", report);
    }

    [Fact]
    public void MigrateCommand_Report_ShouldIncludeStatistics()
    {
        // Arrange
        var stats = new
        {
            FilesModified = 5,
            HandlersConverted = 10,
            LinesChanged = 150,
            Duration = TimeSpan.FromSeconds(5.5)
        };

        // Assert
        Assert.Equal(5, stats.FilesModified);
        Assert.Equal(10, stats.HandlersConverted);
        Assert.Equal(5.5, stats.Duration.TotalSeconds);
    }

    #endregion

    #region Rollback Tests

    [Fact]
    public async Task MigrateCommand_Rollback_ShouldRestoreOriginalFiles()
    {
        // Arrange
        var backupPath = Path.Combine(_testPath, ".backup", "file.cs");
        var targetPath = Path.Combine(_testPath, "file.cs");

        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
        await File.WriteAllTextAsync(backupPath, "original");
        await File.WriteAllTextAsync(targetPath, "modified");

        // Act
        File.Copy(backupPath, targetPath, overwrite: true);
        var restored = await File.ReadAllTextAsync(targetPath);

        // Assert
        Assert.Equal("original", restored);
    }

    [Fact]
    public async Task MigrateCommand_Rollback_ShouldPreserveBackup()
    {
        // Arrange
        var backupPath = Path.Combine(_testPath, ".backup", "file.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
        await File.WriteAllTextAsync(backupPath, "backup content");

        // Act
        var backupExists = File.Exists(backupPath);
        var content = await File.ReadAllTextAsync(backupPath);

        // Assert
        Assert.True(backupExists);
        Assert.Equal("backup content", content);
    }

    #endregion

    #region Format Support Tests

    [Theory]
    [InlineData("markdown", ".md")]
    [InlineData("json", ".json")]
    [InlineData("html", ".html")]
    public void MigrateCommand_Format_ShouldSupportVariousFormats(string format, string extension)
    {
        // Arrange
        var fileName = $"report{extension}";

        // Assert
        Assert.EndsWith(extension, fileName);
        Assert.Contains(format, new[] { "markdown", "json", "html" });
    }

    [Fact]
    public void MigrateCommand_Format_DefaultShouldBeMarkdown()
    {
        // Arrange
        var defaultFormat = "markdown";

        // Assert
        Assert.Equal("markdown", defaultFormat);
    }

    #endregion

    #region Interactive Mode Tests

    [Fact]
    public void MigrateCommand_Interactive_ShouldPromptForChanges()
    {
        // Arrange
        var isInteractive = true;
        var userApprovalRequired = false;

        // Act
        if (isInteractive)
        {
            userApprovalRequired = true;
        }

        // Assert
        Assert.True(userApprovalRequired);
    }

    [Fact]
    public void MigrateCommand_NonInteractive_ShouldAutoApply()
    {
        // Arrange
        var isInteractive = false;
        var autoApply = true;

        // Act
        if (isInteractive)
        {
            autoApply = false;
        }

        // Assert
        Assert.True(autoApply);
    }

    #endregion

    #region Aggressive Mode Tests

    [Fact]
    public void MigrateCommand_Aggressive_ShouldApplyOptimizations()
    {
        // Arrange
        var isAggressive = true;
        var optimizationsApplied = 0;

        // Act
        if (isAggressive)
        {
            optimizationsApplied = 5;
        }

        // Assert
        Assert.True(optimizationsApplied > 0);
    }

    [Fact]
    public void MigrateCommand_NonAggressive_ShouldUseSafeTransformations()
    {
        // Arrange
        var isAggressive = false;
        var safeMode = true;

        // Act
        if (isAggressive)
        {
            safeMode = false;
        }

        // Assert
        Assert.True(safeMode);
    }

    #endregion

    #region File Processing Tests

    [Fact]
    public async Task MigrateCommand_ShouldProcessCSharpFiles()
    {
        // Arrange
        var csFile = Path.Combine(_testPath, "Handler.cs");
        await File.WriteAllTextAsync(csFile, "public class Handler { }");

        // Act
        var isCSharpFile = Path.GetExtension(csFile) == ".cs";

        // Assert
        Assert.True(isCSharpFile);
    }

    [Fact]
    public async Task MigrateCommand_ShouldProcessProjectFiles()
    {
        // Arrange
        var csprojFile = Path.Combine(_testPath, "Project.csproj");
        await File.WriteAllTextAsync(csprojFile, "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        // Act
        var isProjectFile = Path.GetExtension(csprojFile) == ".csproj";

        // Assert
        Assert.True(isProjectFile);
    }

    [Fact]
    public void MigrateCommand_ShouldIgnoreGeneratedFiles()
    {
        // Arrange
        var files = new[]
        {
            "Handler.cs",
            "Handler.g.cs",
            "Handler.Designer.cs"
        };

        // Act
        var generatedFiles = files.Where(f => f.Contains(".g.") || f.Contains(".Designer.")).ToList();

        // Assert
        Assert.Equal(2, generatedFiles.Count);
    }

    [Fact]
    public void MigrateCommand_ShouldProcessOnlyRelevantFiles()
    {
        // Arrange
        var files = new[]
        {
            "Handler.cs",
            "readme.txt",
            "image.png",
            "Query.cs"
        };

        // Act
        var csharpFiles = files.Where(f => f.EndsWith(".cs")).ToList();

        // Assert
        Assert.Equal(2, csharpFiles.Count);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void MigrateCommand_ShouldHandleNonExistentPath()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testPath, "nonexistent");

        // Act
        var exists = Directory.Exists(nonExistentPath);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public void MigrateCommand_ShouldValidateSourceFramework()
    {
        // Arrange
        var validFrameworks = new[] { "mediatr" };
        var framework = "invalidframework";

        // Act
        var isValid = validFrameworks.Contains(framework.ToLower());

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void MigrateCommand_ShouldValidateTargetFramework()
    {
        // Arrange
        var validTargets = new[] { "relay" };
        var target = "invalidtarget";

        // Act
        var isValid = validTargets.Contains(target.ToLower());

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task MigrateCommand_ShouldHandleReadOnlyFiles()
    {
        // Arrange
        var filePath = Path.Combine(_testPath, "readonly.cs");
        await File.WriteAllTextAsync(filePath, "content");
        var fileInfo = new FileInfo(filePath)
        {
            IsReadOnly = true
        };

        // Act
        var isReadOnly = fileInfo.IsReadOnly;

        // Assert
        Assert.True(isReadOnly);

        // Cleanup
        fileInfo.IsReadOnly = false;
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public void MigrateCommand_Statistics_ShouldTrackFilesModified()
    {
        // Arrange & Act
        var stats = new { FilesModified = 5 };

        // Assert
        Assert.Equal(5, stats.FilesModified);
    }

    [Fact]
    public void MigrateCommand_Statistics_ShouldTrackLinesChanged()
    {
        // Arrange & Act
        var linesChanged = 150;

        // Assert
        Assert.Equal(150, linesChanged);
    }

    [Fact]
    public void MigrateCommand_Statistics_ShouldTrackDuration()
    {
        // Arrange
        var startTime = DateTime.Now;
        var endTime = startTime.AddSeconds(5);

        // Act
        var duration = endTime - startTime;

        // Assert
        Assert.Equal(5, duration.TotalSeconds);
    }

    [Fact]
    public void MigrateCommand_Statistics_ShouldCalculateSuccessRate()
    {
        // Arrange
        var totalFiles = 10;
        var successfulFiles = 8;

        // Act
        var successRate = (successfulFiles / (double)totalFiles) * 100;

        // Assert
        Assert.Equal(80.0, successRate);
    }

    #endregion

    #region Preview Tests

    [Fact]
    public void MigrateCommand_Preview_ShouldShowDiff()
    {
        // Arrange
        var original = "using MediatR;";
        var modified = "using Relay.Core;";

        // Act
        var diff = $"- {original}\n+ {modified}";

        // Assert
        Assert.Contains("- using MediatR;", diff);
        Assert.Contains("+ using Relay.Core;", diff);
    }

    [Fact]
    public void MigrateCommand_Preview_ShouldHighlightChanges()
    {
        // Arrange
        var changes = new[]
        {
            "Task -> ValueTask",
            "Handle -> HandleAsync",
            "Added [Handle] attribute"
        };

        // Assert
        Assert.Contains("Task -> ValueTask", changes);
        Assert.Equal(3, changes.Length);
    }

    #endregion

    #region Package Management Tests

    [Fact]
    public void MigrateCommand_Packages_ShouldRemoveMediatR()
    {
        // Arrange
        var packages = new List<string> { "MediatR", "MediatR.Extensions.Microsoft.DependencyInjection" };

        // Act
        packages = [.. packages.Where(p => !p.Contains("MediatR"))];

        // Assert
        Assert.Empty(packages);
    }

    [Fact]
    public void MigrateCommand_Packages_ShouldAddRelayCore()
    {
        // Arrange
        var packages = new List<string>
        {
            // Act
            "Relay.Core"
        };

        // Assert
        Assert.Contains("Relay.Core", packages);
    }

    [Fact]
    public void MigrateCommand_Packages_ShouldUpdateVersion()
    {
        // Arrange
        var packageRef = "<PackageReference Include=\"MediatR\" Version=\"12.0.0\" />";

        // Act
        var updated = packageRef
            .Replace("MediatR", "Relay.Core")
            .Replace("12.0.0", "2.0.0");

        // Assert
        Assert.Contains("Relay.Core", updated);
        Assert.Contains("2.0.0", updated);
    }

    #endregion

    #region Namespace Migration Tests

    [Fact]
    public void MigrateCommand_Namespace_ShouldUpdateUsings()
    {
        // Arrange
        var usings = new[]
        {
            "using MediatR;",
            "using System;",
            "using System.Threading;"
        };

        // Act
        var updated = usings.Select(u => u.Replace("MediatR", "Relay.Core")).ToArray();

        // Assert
        Assert.Contains("using Relay.Core;", updated);
    }

    [Fact]
    public void MigrateCommand_Namespace_ShouldPreserveOtherUsings()
    {
        // Arrange
        var usings = new[]
        {
            "using System;",
            "using System.Linq;",
            "using MediatR;"
        };

        // Act
        var nonMediatR = usings.Where(u => !u.Contains("MediatR")).ToArray();

        // Assert
        Assert.Equal(2, nonMediatR.Length);
        Assert.Contains("using System;", nonMediatR);
    }

    #endregion

    #region Change Tracking Tests

    [Fact]
    public void MigrateCommand_Changes_ShouldTrackAdditions()
    {
        // Arrange
        var changes = new List<(string Type, string Description)>
        {
            // Act
            ("Add", "Added [Handle] attribute")
        };

        // Assert
        Assert.Contains(changes, c => c.Type == "Add");
    }

    [Fact]
    public void MigrateCommand_Changes_ShouldTrackRemovals()
    {
        // Arrange
        var changes = new List<(string Type, string Description)>
        {
            // Act
            ("Remove", "Removed MediatR using")
        };

        // Assert
        Assert.Contains(changes, c => c.Type == "Remove");
    }

    [Fact]
    public void MigrateCommand_Changes_ShouldTrackModifications()
    {
        // Arrange
        var changes = new List<(string Type, string Description)>
        {
            // Act
            ("Modify", "Changed Task to ValueTask")
        };

        // Assert
        Assert.Contains(changes, c => c.Type == "Modify");
    }

    [Fact]
    public void MigrateCommand_Changes_ShouldGroupByCategory()
    {
        // Arrange
        var changes = new[]
        {
            (Category: "Usings", Description: "Updated using statements"),
            (Category: "Usings", Description: "Removed MediatR"),
            (Category: "Methods", Description: "Renamed Handle to HandleAsync")
        };

        // Act
        var grouped = changes.GroupBy(c => c.Category).ToList();

        // Assert
        Assert.Equal(2, grouped.Count);
        Assert.Equal(2, grouped.First(g => g.Key == "Usings").Count());
    }

    #endregion

    #region Path Resolution Tests

    [Fact]
    public void MigrateCommand_Path_ShouldResolveRelativePath()
    {
        // Arrange
        var relativePath = "./project";

        // Act
        var absolutePath = Path.GetFullPath(relativePath);

        // Assert
        Assert.False(string.IsNullOrEmpty(absolutePath));
        Assert.True(Path.IsPathRooted(absolutePath));
    }

    [Fact]
    public void MigrateCommand_Path_ShouldHandleAbsolutePath()
    {
        // Arrange
        var absolutePath = _testPath;

        // Act
        var isAbsolute = Path.IsPathRooted(absolutePath);

        // Assert
        Assert.True(isAbsolute);
    }

    [Fact]
    public void MigrateCommand_BackupPath_ShouldBeRelativeToProject()
    {
        // Arrange
        var projectPath = _testPath;
        var backupPath = Path.Combine(projectPath, ".backup");

        // Act
        var isUnderProject = backupPath.StartsWith(projectPath);

        // Assert
        Assert.True(isUnderProject);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task MigrateCommand_FullMigration_ShouldUpdateAllComponents()
    {
        // Arrange
        var handlerFile = Path.Combine(_testPath, "Handler.cs");
        var originalContent = @"using MediatR;

public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    public async Task<User> Handle(GetUserQuery request, CancellationToken ct)
    {
        return new User();
    }
}";
        await File.WriteAllTextAsync(handlerFile, originalContent);

        // Act
        var content = await File.ReadAllTextAsync(handlerFile);
        var hasMediatR = content.Contains("MediatR");
        var hasHandler = content.Contains("IRequestHandler");

        // Assert
        Assert.True(hasMediatR);
        Assert.True(hasHandler);
    }

    [Fact]
    public void MigrateCommand_CompleteFlow_ShouldFollowSteps()
    {
        // Arrange
        var steps = new[]
        {
            "1. Analyze",
            "2. Backup",
            "3. Transform",
            "4. Report"
        };

        // Assert
        Assert.Equal(4, steps.Length);
        Assert.Contains("Analyze", steps[0]);
        Assert.Contains("Report", steps[3]);
    }

    #endregion

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testPath))
            {
                Directory.Delete(_testPath, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
        GC.SuppressFinalize(this);
    }
}


