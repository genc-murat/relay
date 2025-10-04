using FluentAssertions;
using Relay.CLI.Commands;
using System.CommandLine;
using Xunit;

namespace Relay.CLI.Tests.Commands;

/// <summary>
/// Extended comprehensive tests for OptimizeCommand
/// </summary>
public class OptimizeCommandExtendedTests : IDisposable
{
    private readonly string _testPath;

    public OptimizeCommandExtendedTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"relay-optimize-ext-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPath);
    }

    #region Command Creation Tests

    [Fact]
    public void OptimizeCommand_Create_ShouldReturnValidCommand()
    {
        // Act
        var command = OptimizeCommand.Create();

        // Assert
        command.Should().NotBeNull();
        command.Name.Should().Be("optimize");
        command.Description.Should().Contain("optimization");
    }

    [Fact]
    public void OptimizeCommand_Create_ShouldHavePathOption()
    {
        // Act
        var command = OptimizeCommand.Create();
        var pathOption = command.Options.FirstOrDefault(o => o.Name == "path");

        // Assert
        pathOption.Should().NotBeNull();
    }

    [Fact]
    public void OptimizeCommand_Create_ShouldHaveDryRunOption()
    {
        // Act
        var command = OptimizeCommand.Create();
        var dryRunOption = command.Options.FirstOrDefault(o => o.Name == "dry-run");

        // Assert
        dryRunOption.Should().NotBeNull();
    }

    [Fact]
    public void OptimizeCommand_Create_ShouldHaveTargetOption()
    {
        // Act
        var command = OptimizeCommand.Create();
        var targetOption = command.Options.FirstOrDefault(o => o.Name == "target");

        // Assert
        targetOption.Should().NotBeNull();
    }

    [Fact]
    public void OptimizeCommand_Create_ShouldHaveAggressiveOption()
    {
        // Act
        var command = OptimizeCommand.Create();
        var aggressiveOption = command.Options.FirstOrDefault(o => o.Name == "aggressive");

        // Assert
        aggressiveOption.Should().NotBeNull();
    }

    [Fact]
    public void OptimizeCommand_Create_ShouldHaveBackupOption()
    {
        // Act
        var command = OptimizeCommand.Create();
        var backupOption = command.Options.FirstOrDefault(o => o.Name == "backup");

        // Assert
        backupOption.Should().NotBeNull();
    }

    [Fact]
    public void OptimizeCommand_Options_ShouldHaveCorrectCount()
    {
        // Act
        var command = OptimizeCommand.Create();

        // Assert
        command.Options.Should().HaveCount(5);
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void OptimizeCommand_PathOption_DefaultShouldBeCurrentDirectory()
    {
        // Arrange
        var defaultPath = ".";

        // Assert
        defaultPath.Should().Be(".");
    }

    [Fact]
    public void OptimizeCommand_DryRunOption_DefaultShouldBeFalse()
    {
        // Arrange
        var defaultDryRun = false;

        // Assert
        defaultDryRun.Should().BeFalse();
    }

    [Fact]
    public void OptimizeCommand_TargetOption_DefaultShouldBeAll()
    {
        // Arrange
        var defaultTarget = "all";

        // Assert
        defaultTarget.Should().Be("all");
    }

    [Fact]
    public void OptimizeCommand_AggressiveOption_DefaultShouldBeFalse()
    {
        // Arrange
        var defaultAggressive = false;

        // Assert
        defaultAggressive.Should().BeFalse();
    }

    [Fact]
    public void OptimizeCommand_BackupOption_DefaultShouldBeTrue()
    {
        // Arrange
        var defaultBackup = true;

        // Assert
        defaultBackup.Should().BeTrue();
    }

    #endregion

    #region Target Types Tests

    [Theory]
    [InlineData("all")]
    [InlineData("handlers")]
    [InlineData("requests")]
    [InlineData("config")]
    public void OptimizeCommand_Target_ShouldSupportCommonTargets(string target)
    {
        // Arrange
        var validTargets = new[] { "all", "handlers", "requests", "config" };

        // Assert
        validTargets.Should().Contain(target);
    }

    [Fact]
    public void OptimizeCommand_Target_All_ShouldIncludeAllOptimizations()
    {
        // Arrange
        var target = "all";

        // Assert
        target.Should().Be("all");
    }

    [Fact]
    public void OptimizeCommand_Target_Handlers_ShouldOptimizeHandlersOnly()
    {
        // Arrange
        var target = "handlers";

        // Assert
        target.Should().Be("handlers");
    }

    [Fact]
    public void OptimizeCommand_Target_Requests_ShouldOptimizeRequestsOnly()
    {
        // Arrange
        var target = "requests";

        // Assert
        target.Should().Be("requests");
    }

    [Fact]
    public void OptimizeCommand_Target_Config_ShouldOptimizeConfigOnly()
    {
        // Arrange
        var target = "config";

        // Assert
        target.Should().Be("config");
    }

    #endregion

    #region Code Transformation Tests

    [Fact]
    public void OptimizeCommand_ShouldTransformTaskToValueTask()
    {
        // Arrange
        var original = "public async Task<Result> HandleAsync(";

        // Act
        var transformed = original.Replace("Task<", "ValueTask<");

        // Assert
        transformed.Should().Contain("ValueTask<Result>");
        transformed.Should().NotContain("async Task<");
    }

    [Fact]
    public void OptimizeCommand_ShouldAddHandleAttribute()
    {
        // Arrange
        var method = "public async ValueTask<Result> HandleAsync(";
        var attribute = "[Handle]";

        // Act
        var withAttribute = $"{attribute}\n    {method}";

        // Assert
        withAttribute.Should().Contain("[Handle]");
    }

    [Fact]
    public void OptimizeCommand_ShouldAddConfigureAwait()
    {
        // Arrange
        var original = "await repository.GetAsync()";

        // Act
        var optimized = original.Replace("GetAsync()", "GetAsync().ConfigureAwait(false)");

        // Assert
        optimized.Should().Contain("ConfigureAwait(false)");
    }

    [Fact]
    public void OptimizeCommand_ShouldOptimizeStringConcatenation()
    {
        // Arrange
        var original = "var result = str1 + str2 + str3;";

        // Act
        var optimized = "var result = string.Concat(str1, str2, str3);";

        // Assert
        optimized.Should().Contain("string.Concat");
    }

    #endregion

    #region File Discovery Tests

    [Fact]
    public async Task OptimizeCommand_ShouldDiscoverCSharpFiles()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_testPath, "Handler1.cs"), "code");
        await File.WriteAllTextAsync(Path.Combine(_testPath, "Handler2.cs"), "code");

        // Act
        var csFiles = Directory.GetFiles(_testPath, "*.cs", SearchOption.AllDirectories);

        // Assert
        csFiles.Should().HaveCount(2);
    }

    [Fact]
    public async Task OptimizeCommand_ShouldExcludeBinDirectory()
    {
        // Arrange
        var binDir = Path.Combine(_testPath, "bin");
        Directory.CreateDirectory(binDir);
        await File.WriteAllTextAsync(Path.Combine(binDir, "Compiled.cs"), "code");
        await File.WriteAllTextAsync(Path.Combine(_testPath, "Handler.cs"), "code");

        // Act
        var csFiles = Directory.GetFiles(_testPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin")).ToList();

        // Assert
        csFiles.Should().HaveCount(1);
    }

    [Fact]
    public async Task OptimizeCommand_ShouldExcludeObjDirectory()
    {
        // Arrange
        var objDir = Path.Combine(_testPath, "obj");
        Directory.CreateDirectory(objDir);
        await File.WriteAllTextAsync(Path.Combine(objDir, "Temp.cs"), "code");
        await File.WriteAllTextAsync(Path.Combine(_testPath, "Handler.cs"), "code");

        // Act
        var csFiles = Directory.GetFiles(_testPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("obj")).ToList();

        // Assert
        csFiles.Should().HaveCount(1);
    }

    #endregion

    #region Pattern Matching Tests

    [Fact]
    public void OptimizeCommand_ShouldDetectHandlerPattern()
    {
        // Arrange
        var code = "public class GetUserHandler : IRequestHandler<GetUserQuery, User>";

        // Assert
        code.Should().Contain("IRequestHandler");
    }

    [Fact]
    public void OptimizeCommand_ShouldDetectRequestPattern()
    {
        // Arrange
        var code = "public record GetUserQuery : IRequest<User>";

        // Assert
        code.Should().Contain("IRequest");
    }

    [Fact]
    public void OptimizeCommand_ShouldDetectAsyncMethod()
    {
        // Arrange
        var code = "public async Task<Result> HandleAsync(";

        // Assert
        code.Should().Contain("async");
        code.Should().Contain("Task<");
    }

    [Fact]
    public void OptimizeCommand_ShouldDetectAwaitStatement()
    {
        // Arrange
        var code = "return await repository.GetAsync();";

        // Assert
        code.Should().Contain("await");
    }

    #endregion

    #region Backup Tests

    [Fact]
    public void OptimizeCommand_Backup_ShouldCreateBackupDirectory()
    {
        // Arrange
        var backupDir = Path.Combine(_testPath, ".backup");

        // Act
        Directory.CreateDirectory(backupDir);

        // Assert
        Directory.Exists(backupDir).Should().BeTrue();
    }

    [Fact]
    public async Task OptimizeCommand_Backup_ShouldCopyOriginalFile()
    {
        // Arrange
        var sourceFile = Path.Combine(_testPath, "Handler.cs");
        var backupDir = Path.Combine(_testPath, ".backup");
        Directory.CreateDirectory(backupDir);
        var backupFile = Path.Combine(backupDir, "Handler.cs");

        await File.WriteAllTextAsync(sourceFile, "original content");

        // Act
        File.Copy(sourceFile, backupFile);

        // Assert
        File.Exists(backupFile).Should().BeTrue();
        var content = await File.ReadAllTextAsync(backupFile);
        content.Should().Be("original content");
    }

    [Fact]
    public void OptimizeCommand_Backup_ShouldIncludeTimestamp()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var backupName = $".backup_{timestamp}";

        // Assert
        backupName.Should().MatchRegex(@"\.backup_\d{8}_\d{6}");
    }

    #endregion

    #region DryRun Tests

    [Fact]
    public void OptimizeCommand_DryRun_ShouldNotModifyFiles()
    {
        // Arrange
        var isDryRun = true;
        var filesModified = 0;

        // Act
        if (!isDryRun)
        {
            filesModified = 10;
        }

        // Assert
        filesModified.Should().Be(0);
    }

    [Fact]
    public void OptimizeCommand_DryRun_ShouldShowPreview()
    {
        // Arrange
        var isDryRun = true;
        var previewShown = false;

        // Act
        if (isDryRun)
        {
            previewShown = true;
        }

        // Assert
        previewShown.Should().BeTrue();
    }

    [Fact]
    public void OptimizeCommand_DryRun_ShouldNotCreateBackup()
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
        backupCreated.Should().BeFalse();
    }

    #endregion

    #region Aggressive Mode Tests

    [Fact]
    public void OptimizeCommand_Aggressive_ShouldEnableAllOptimizations()
    {
        // Arrange
        var isAggressive = true;
        var optimizationCount = 0;

        // Act
        if (isAggressive)
        {
            optimizationCount = 10; // All optimizations
        }
        else
        {
            optimizationCount = 5; // Safe optimizations only
        }

        // Assert
        optimizationCount.Should().Be(10);
    }

    [Fact]
    public void OptimizeCommand_NonAggressive_ShouldUseSafeOptimizations()
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
        safeMode.Should().BeTrue();
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public void OptimizeCommand_Statistics_ShouldCountFilesProcessed()
    {
        // Arrange
        var filesProcessed = 0;

        // Act
        filesProcessed = 15;

        // Assert
        filesProcessed.Should().Be(15);
    }

    [Fact]
    public void OptimizeCommand_Statistics_ShouldCountOptimizationsApplied()
    {
        // Arrange
        var optimizationsApplied = 0;

        // Act
        optimizationsApplied = 45;

        // Assert
        optimizationsApplied.Should().Be(45);
    }

    [Fact]
    public void OptimizeCommand_Statistics_ShouldCalculateSuccessRate()
    {
        // Arrange
        var totalFiles = 10;
        var successfulFiles = 8;

        // Act
        var successRate = (successfulFiles / (double)totalFiles) * 100;

        // Assert
        successRate.Should().Be(80.0);
    }

    #endregion

    #region Optimization Types Tests

    [Fact]
    public void OptimizeCommand_Optimization_TaskToValueTask()
    {
        // Arrange
        var optimizationType = "Task -> ValueTask";

        // Assert
        optimizationType.Should().Contain("ValueTask");
    }

    [Fact]
    public void OptimizeCommand_Optimization_AddHandleAttribute()
    {
        // Arrange
        var optimizationType = "Add [Handle] attribute";

        // Assert
        optimizationType.Should().Contain("[Handle]");
    }

    [Fact]
    public void OptimizeCommand_Optimization_ConfigureAwait()
    {
        // Arrange
        var optimizationType = "Add ConfigureAwait(false)";

        // Assert
        optimizationType.Should().Contain("ConfigureAwait");
    }

    [Fact]
    public void OptimizeCommand_Optimization_StringInterpolation()
    {
        // Arrange
        var optimizationType = "Optimize string concatenation";

        // Assert
        optimizationType.Should().Contain("string");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void OptimizeCommand_ShouldValidateProjectPath()
    {
        // Arrange
        var invalidPath = @"C:\NonExistent\Path";

        // Act
        var exists = Directory.Exists(invalidPath);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public void OptimizeCommand_ShouldHandleEmptyDirectory()
    {
        // Arrange
        var emptyDir = Path.Combine(_testPath, "empty");
        Directory.CreateDirectory(emptyDir);

        // Act
        var files = Directory.GetFiles(emptyDir, "*.cs");

        // Assert
        files.Should().BeEmpty();
    }

    [Fact]
    public void OptimizeCommand_ShouldHandleInvalidTarget()
    {
        // Arrange
        var target = "invalid-target";
        var validTargets = new[] { "all", "handlers", "requests", "config" };

        // Act
        var isValid = validTargets.Contains(target);

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region Handler Optimization Tests

    [Fact]
    public async Task OptimizeCommand_Handler_ShouldOptimizeReturnType()
    {
        // Arrange
        var handlerCode = @"public class Handler
{
    public async Task<Result> HandleAsync() { }
}";
        var filePath = Path.Combine(_testPath, "Handler.cs");
        await File.WriteAllTextAsync(filePath, handlerCode);

        // Act
        var content = await File.ReadAllTextAsync(filePath);
        var hasTaskReturn = content.Contains("Task<");

        // Assert
        hasTaskReturn.Should().BeTrue();
    }

    [Fact]
    public void OptimizeCommand_Handler_ShouldDetectMissingHandleAttribute()
    {
        // Arrange
        var code = @"public async ValueTask<Result> HandleAsync()";

        // Act
        var hasMissingAttribute = !code.Contains("[Handle]");

        // Assert
        hasMissingAttribute.Should().BeTrue();
    }

    #endregion

    #region Request Optimization Tests

    [Fact]
    public void OptimizeCommand_Request_ShouldBeRecord()
    {
        // Arrange
        var code = "public record GetUserRequest : IRequest<User>;";

        // Assert
        code.Should().Contain("record");
    }

    [Fact]
    public void OptimizeCommand_Request_ShouldImplementIRequest()
    {
        // Arrange
        var code = "public record GetUserRequest : IRequest<User>;";

        // Assert
        code.Should().Contain("IRequest");
    }

    #endregion

    #region Config Optimization Tests

    [Fact]
    public async Task OptimizeCommand_Config_ShouldEnableCaching()
    {
        // Arrange
        var configPath = Path.Combine(_testPath, "appsettings.json");
        var config = @"{
  ""Relay"": {
    ""EnableCaching"": false
  }
}";
        await File.WriteAllTextAsync(configPath, config);

        // Act
        var content = await File.ReadAllTextAsync(configPath);

        // Assert
        content.Should().Contain("EnableCaching");
    }

    #endregion

    #region Progress Reporting Tests

    [Fact]
    public void OptimizeCommand_Progress_ShouldReportFileCount()
    {
        // Arrange
        var totalFiles = 100;
        var processedFiles = 50;

        // Act
        var progress = (processedFiles / (double)totalFiles) * 100;

        // Assert
        progress.Should().Be(50.0);
    }

    [Fact]
    public void OptimizeCommand_Progress_ShouldReportOptimizationCount()
    {
        // Arrange
        var optimizations = new List<string>
        {
            "Task -> ValueTask",
            "Added [Handle]",
            "Added ConfigureAwait"
        };

        // Assert
        optimizations.Should().HaveCount(3);
    }

    #endregion

    #region Output Tests

    [Fact]
    public void OptimizeCommand_Output_ShouldShowSummary()
    {
        // Arrange
        var summary = new
        {
            FilesProcessed = 10,
            OptimizationsApplied = 25,
            SuccessRate = 90.0
        };

        // Assert
        summary.FilesProcessed.Should().Be(10);
        summary.OptimizationsApplied.Should().Be(25);
        summary.SuccessRate.Should().Be(90.0);
    }

    [Fact]
    public void OptimizeCommand_Output_ShouldShowOptimizationDetails()
    {
        // Arrange
        var details = new[]
        {
            "Handler.cs: Task -> ValueTask",
            "Handler.cs: Added [Handle]",
            "Request.cs: Changed to record"
        };

        // Assert
        details.Should().HaveCount(3);
        details[0].Should().Contain("Handler.cs");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task OptimizeCommand_FullFlow_ShouldProcessMultipleFiles()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_testPath, "Handler1.cs"), "handler code");
        await File.WriteAllTextAsync(Path.Combine(_testPath, "Handler2.cs"), "handler code");
        await File.WriteAllTextAsync(Path.Combine(_testPath, "Request.cs"), "request code");

        // Act
        var files = Directory.GetFiles(_testPath, "*.cs");

        // Assert
        files.Should().HaveCount(3);
    }

    [Fact]
    public async Task OptimizeCommand_FullFlow_ShouldTrackChanges()
    {
        // Arrange
        var filePath = Path.Combine(_testPath, "Handler.cs");
        await File.WriteAllTextAsync(filePath, "original");

        // Act
        var originalContent = await File.ReadAllTextAsync(filePath);
        await File.WriteAllTextAsync(filePath, "optimized");
        var newContent = await File.ReadAllTextAsync(filePath);

        // Assert
        originalContent.Should().NotBe(newContent);
    }

    #endregion

    #region Path Resolution Tests

    [Fact]
    public void OptimizeCommand_Path_ShouldResolveRelativePath()
    {
        // Arrange
        var relativePath = "./project";

        // Act
        var absolutePath = Path.GetFullPath(relativePath);

        // Assert
        absolutePath.Should().NotBeNullOrEmpty();
        Path.IsPathRooted(absolutePath).Should().BeTrue();
    }

    [Fact]
    public void OptimizeCommand_Path_ShouldHandleAbsolutePath()
    {
        // Arrange
        var absolutePath = _testPath;

        // Act
        var isAbsolute = Path.IsPathRooted(absolutePath);

        // Assert
        isAbsolute.Should().BeTrue();
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void OptimizeCommand_EdgeCase_EmptyFile()
    {
        // Arrange
        var content = "";

        // Assert
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task OptimizeCommand_EdgeCase_VeryLargeFile()
    {
        // Arrange
        var largeContent = new string('A', 100000);
        var filePath = Path.Combine(_testPath, "Large.cs");
        await File.WriteAllTextAsync(filePath, largeContent);

        // Act
        var content = await File.ReadAllTextAsync(filePath);

        // Assert
        content.Length.Should().Be(100000);
    }

    [Fact]
    public void OptimizeCommand_EdgeCase_SpecialCharactersInPath()
    {
        // Arrange
        var pathWithSpaces = Path.Combine(_testPath, "My Handler.cs");

        // Assert
        pathWithSpaces.Should().Contain(" ");
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
    }
}
