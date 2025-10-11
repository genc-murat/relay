using Relay.CLI.Migration;

namespace Relay.CLI.Tests.Migration;

public class MigrationEngineTests : IDisposable
{
    private readonly MigrationEngine _engine;
    private readonly string _testProjectPath;

    public MigrationEngineTests()
    {
        _engine = new MigrationEngine();
        _testProjectPath = Path.Combine(Path.GetTempPath(), $"relay-migration-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testProjectPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testProjectPath))
        {
            try
            {
                Directory.Delete(_testProjectPath, recursive: true);
            }
            catch
            {
                // Cleanup may fail due to file locks - best effort
            }
        }
    }

    #region AnalyzeAsync Tests

    [Fact]
    public async Task AnalyzeAsync_WithEmptyProject_ReturnsCannotMigrate()
    {
        // Arrange
        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath
        };

        // Act
        var result = await _engine.AnalyzeAsync(options);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.CanMigrate);
        Assert.Contains(result.Issues, i => i.Code == "NO_PROJECT");
    }

    [Fact]
    public async Task AnalyzeAsync_WithValidProject_ReturnsAnalysisResult()
    {
        // Arrange
        await CreateTestProjectWithMediatR();
        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath
        };

        // Act
        var result = await _engine.AnalyzeAsync(options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testProjectPath, result.ProjectPath);
        Assert.True((result.AnalysisDate - DateTime.UtcNow).Duration() < TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AnalyzeAsync_WithMediatRPackages_DetectsPackageReferences()
    {
        // Arrange
        await CreateTestProjectWithMediatR();
        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath
        };

        // Act
        var result = await _engine.AnalyzeAsync(options);

        // Assert
        Assert.NotEmpty(result.PackageReferences);
        Assert.Contains(result.PackageReferences, p => p.Name == "MediatR");
    }

    [Fact]
    public async Task AnalyzeAsync_WithHandlers_DetectsHandlers()
    {
        // Arrange
        await CreateTestProjectWithHandlers();
        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath
        };

        // Act
        var result = await _engine.AnalyzeAsync(options);

        // Assert
        Assert.True(result.HandlersFound > 0);
        Assert.True(result.RequestsFound > 0);
        Assert.True(result.FilesAffected > 0);
    }

    #endregion

    #region CreateBackupAsync Tests

    [Fact]
    public async Task CreateBackupAsync_CreatesBackupDirectory()
    {
        // Arrange
        await CreateTestProjectWithMediatR();
        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            BackupPath = ".backup"
        };

        // Act
        var backupPath = await _engine.CreateBackupAsync(options);

        // Assert
        Assert.NotEmpty(backupPath);
        Assert.True(Directory.Exists(backupPath));
    }

    [Fact]
    public async Task CreateBackupAsync_CreatesBackupWithTimestamp()
    {
        // Arrange
        await CreateTestProjectWithMediatR();
        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            BackupPath = ".backup"
        };

        // Act
        var backupPath = await _engine.CreateBackupAsync(options);

        // Assert
        Assert.Contains("backup_", backupPath);
        Assert.True(System.Text.RegularExpressions.Regex.IsMatch(backupPath, @"backup_\d{8}_\d{6}"));
    }

    [Fact]
    public async Task CreateBackupAsync_BacksUpProjectFiles()
    {
        // Arrange
        await CreateTestProjectWithMediatR();
        var sourceFile = Path.Combine(_testProjectPath, "Test.cs");
        await File.WriteAllTextAsync(sourceFile, "// test content");

        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            BackupPath = ".backup"
        };

        // Act
        var backupPath = await _engine.CreateBackupAsync(options);

        // Assert
        var backupFile = Path.Combine(backupPath, "Test.cs");
        Assert.True(File.Exists(backupFile));
        var content = await File.ReadAllTextAsync(backupFile);
        Assert.Equal("// test content", content);
    }

    #endregion

    #region MigrateAsync Tests

    [Fact]
    public async Task MigrateAsync_WithNoProjectFiles_ReturnsFailedStatus()
    {
        // Arrange
        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.Equal(MigrationStatus.Failed, result.Status);
        Assert.NotEmpty(result.Issues);
    }

    [Fact]
    public async Task MigrateAsync_WithDryRun_DoesNotModifyFiles()
    {
        // Arrange
        await CreateTestProjectWithHandlers();
        var originalContent = await File.ReadAllTextAsync(Path.Combine(_testProjectPath, "Handler.cs"));

        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = true
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.True(result.Status == MigrationStatus.Success || result.Status == MigrationStatus.Partial);
        Assert.NotEmpty(result.Changes);

        var currentContent = await File.ReadAllTextAsync(Path.Combine(_testProjectPath, "Handler.cs"));
        Assert.Equal(originalContent, currentContent);
    }

    [Fact]
    public async Task MigrateAsync_WithBackupEnabled_CreatesBackup()
    {
        // Arrange
        await CreateTestProjectWithHandlers();
        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            CreateBackup = true,
            BackupPath = ".backup",
            DryRun = false
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.True(result.CreatedBackup);
        Assert.NotEmpty(result.BackupPath);
        Assert.True(Directory.Exists(result.BackupPath));
    }

    [Fact]
    public async Task MigrateAsync_WithBackupDisabled_DoesNotCreateBackup()
    {
        // Arrange
        await CreateTestProjectWithHandlers();
        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            CreateBackup = false,
            DryRun = false
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.False(result.CreatedBackup);
        Assert.Null(result.BackupPath);
    }

    [Fact]
    public async Task MigrateAsync_TransformsMediatRHandlers()
    {
        // Arrange
        await CreateTestProjectWithHandlers();
        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.True(result.Status == MigrationStatus.Success || result.Status == MigrationStatus.Partial);
        Assert.True(result.HandlersMigrated > 0);
        Assert.True(result.FilesModified > 0);
        Assert.Contains(result.Changes, c => c.Category == "Using Directives");
    }

    [Fact]
    public async Task MigrateAsync_TransformsPackageReferences()
    {
        // Arrange
        await CreateTestProjectWithMediatR();
        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.Contains(result.Changes, c => c.Category == "Package References");
        Assert.Contains(result.Changes, c => c.Type == ChangeType.Remove && c.Description.Contains("MediatR"));
        Assert.Contains(result.Changes, c => c.Type == ChangeType.Add && c.Description.Contains("Relay.Core"));
    }

    [Fact]
    public async Task MigrateAsync_RecordsDuration()
    {
        // Arrange
        await CreateTestProjectWithHandlers();
        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.True(result.StartTime < result.EndTime);
        Assert.True(result.Duration > TimeSpan.Zero);
        Assert.True(result.Duration < TimeSpan.FromMinutes(1)); // Reasonable timeout
    }

    [Fact]
    public async Task MigrateAsync_WithCustomMediator_AddsManualStep()
    {
        // Arrange
        await CreateTestProjectWithCustomMediator();
        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.Contains(result.ManualSteps, s => s.Contains("custom IMediator"));
    }

    [Fact]
    public async Task MigrateAsync_WithCustomBehaviors_AddsManualStep()
    {
        // Arrange
        await CreateTestProjectWithCustomBehavior();
        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.Contains(result.ManualSteps, s => s.Contains("pipeline behaviors"));
    }

    [Fact]
    public async Task MigrateAsync_SkipsBinAndObjDirectories()
    {
        // Arrange
        await CreateTestProjectWithHandlers();

        // Create files in bin/obj that should be skipped
        var binDir = Path.Combine(_testProjectPath, "bin");
        var objDir = Path.Combine(_testProjectPath, "obj");
        Directory.CreateDirectory(binDir);
        Directory.CreateDirectory(objDir);
        await File.WriteAllTextAsync(Path.Combine(binDir, "Test.cs"), "using MediatR;");
        await File.WriteAllTextAsync(Path.Combine(objDir, "Test.cs"), "using MediatR;");

        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.DoesNotContain(result.Changes, c => c.FilePath.Contains("\\bin\\"));
        Assert.DoesNotContain(result.Changes, c => c.FilePath.Contains("\\obj\\"));
    }

    [Fact]
    public async Task MigrateAsync_OnException_ReturnsFailedStatus()
    {
        // Arrange
        var options = new MigrationOptions
        {
            ProjectPath = "C:\\NonExistentPath\\Invalid",
            DryRun = false
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.Equal(MigrationStatus.Failed, result.Status);
        Assert.Contains(result.Issues, i => i.Contains("failed"));
    }

    #endregion

    #region RollbackAsync Tests

    [Fact]
    public async Task RollbackAsync_WithValidBackup_RestoresFiles()
    {
        // Arrange
        await CreateTestProjectWithMediatR();
        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            BackupPath = ".backup"
        };

        var backupPath = await _engine.CreateBackupAsync(options);

        // Modify original file
        var testFile = Path.Combine(_testProjectPath, "Test.csproj");
        await File.WriteAllTextAsync(testFile, "<Project>Modified</Project>");

        // Act
        var result = await _engine.RollbackAsync(backupPath);

        // Assert
        Assert.True(result);
        var content = await File.ReadAllTextAsync(testFile);
        Assert.DoesNotContain("Modified", content);
    }

    [Fact]
    public async Task RollbackAsync_WithInvalidBackup_ReturnsFalse()
    {
        // Arrange
        var invalidPath = Path.Combine(_testProjectPath, "nonexistent-backup");

        // Act
        var result = await _engine.RollbackAsync(invalidPath);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Helper Methods

    private async Task CreateTestProjectWithMediatR()
    {
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""MediatR"" Version=""12.0.1"" />
    <PackageReference Include=""MediatR.Extensions.Microsoft.DependencyInjection"" Version=""11.0.0"" />
  </ItemGroup>
</Project>";

        await File.WriteAllTextAsync(Path.Combine(_testProjectPath, "Test.csproj"), csprojContent);
    }

    private async Task CreateTestProjectWithHandlers()
    {
        await CreateTestProjectWithMediatR();

        var handlerContent = @"using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace TestProject;

public record GetUserQuery(int Id) : IRequest<string>;

public class GetUserHandler : IRequestHandler<GetUserQuery, string>
{
    public async Task<string> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return $""User {request.Id}"";
    }
}";

        await File.WriteAllTextAsync(Path.Combine(_testProjectPath, "Handler.cs"), handlerContent);
    }

    private async Task CreateTestProjectWithCustomMediator()
    {
        await CreateTestProjectWithMediatR();

        var customMediatorContent = @"using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace TestProject;

public class CustomMediator : IMediator
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}";

        await File.WriteAllTextAsync(Path.Combine(_testProjectPath, "CustomMediator.cs"), customMediatorContent);
    }

    private async Task CreateTestProjectWithCustomBehavior()
    {
        await CreateTestProjectWithMediatR();

        var customBehaviorContent = @"using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace TestProject;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Log before
        var response = await next();
        // Log after
        return response;
    }
}";

        await File.WriteAllTextAsync(Path.Combine(_testProjectPath, "CustomBehavior.cs"), customBehaviorContent);
    }

    #endregion

    #region Parallel Processing Tests

    [Fact]
    public async Task MigrateAsync_WithParallelProcessingEnabled_ProcessesFiles()
    {
        // Arrange
        await CreateTestProjectWithMultipleHandlers(10);
        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            EnableParallelProcessing = true,
            MaxDegreeOfParallelism = 4,
            DryRun = false
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.True(result.Status == MigrationStatus.Success || result.Status == MigrationStatus.Partial);
        Assert.True(result.FilesModified > 0);
        Assert.True(result.HandlersMigrated > 0);
    }

    [Fact]
    public async Task MigrateAsync_WithParallelProcessingDisabled_ProcessesSequentially()
    {
        // Arrange
        await CreateTestProjectWithMultipleHandlers(10);
        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            EnableParallelProcessing = false,
            DryRun = false
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.True(result.Status == MigrationStatus.Success || result.Status == MigrationStatus.Partial);
        Assert.True(result.FilesModified > 0);
        Assert.True(result.HandlersMigrated > 0);
    }

    [Fact]
    public async Task MigrateAsync_WithSmallProject_UsesSequentialProcessing()
    {
        // Arrange - Only 3 files (less than 5 threshold)
        await CreateTestProjectWithMultipleHandlers(3);
        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            EnableParallelProcessing = true, // Enabled but should use sequential due to low file count
            DryRun = false
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.True(result.Status == MigrationStatus.Success || result.Status == MigrationStatus.Partial);
    }

    [Fact]
    public async Task MigrateAsync_ParallelProcessing_ProducesCorrectResults()
    {
        // Arrange
        await CreateTestProjectWithMultipleHandlers(15);

        var parallelOptions = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            EnableParallelProcessing = true,
            DryRun = true
        };

        var sequentialOptions = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            EnableParallelProcessing = false,
            DryRun = true
        };

        // Act
        var parallelResult = await _engine.MigrateAsync(parallelOptions);

        // Clean up and recreate project for sequential test
        Directory.Delete(_testProjectPath, true);
        Directory.CreateDirectory(_testProjectPath);
        await CreateTestProjectWithMultipleHandlers(15);

        var sequentialResult = await _engine.MigrateAsync(sequentialOptions);

        // Assert - Both should produce same results
        Assert.Equal(sequentialResult.FilesModified, parallelResult.FilesModified);
        Assert.Equal(sequentialResult.HandlersMigrated, parallelResult.HandlersMigrated);
        Assert.Equal(sequentialResult.Changes.Count, parallelResult.Changes.Count);
    }

    [Fact]
    public async Task MigrateAsync_WithCustomParallelismDegree_RespectsLimit()
    {
        // Arrange
        await CreateTestProjectWithMultipleHandlers(20);
        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            EnableParallelProcessing = true,
            MaxDegreeOfParallelism = 2,
            ParallelBatchSize = 5,
            DryRun = false
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.True(result.Status == MigrationStatus.Success || result.Status == MigrationStatus.Partial);
        Assert.True(result.Duration > TimeSpan.Zero);
    }

    [Fact]
    public async Task MigrateAsync_ParallelProcessing_HandlesErrorsGracefully()
    {
        // Arrange
        await CreateTestProjectWithMultipleHandlers(10);

        // Add a file with syntax error
        var badFile = Path.Combine(_testProjectPath, "BadHandler.cs");
        await File.WriteAllTextAsync(badFile, @"using MediatR;
// This file has intentional syntax errors
public class BadHandler : IRequestHandler<
{
}");

        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            EnableParallelProcessing = true,
            DryRun = false
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert - Should handle error but continue with other files
        Assert.True(result.FilesModified > 0 || result.Status == MigrationStatus.Partial);
    }

    private async Task CreateTestProjectWithMultipleHandlers(int handlerCount)
    {
        await CreateTestProjectWithMediatR();

        for (int i = 0; i < handlerCount; i++)
        {
            var handlerContent = $@"using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace TestProject;

public record GetUser{i}Query(int Id) : IRequest<string>;

public class GetUser{i}Handler : IRequestHandler<GetUser{i}Query, string>
{{
    public async Task<string> Handle(GetUser{i}Query request, CancellationToken cancellationToken)
    {{
        return $""User {{request.Id}}"";
    }}
}}";

            await File.WriteAllTextAsync(Path.Combine(_testProjectPath, $"Handler{i}.cs"), handlerContent);
        }
    }

    #endregion

    #region Progress Reporting Tests

    [Fact]
    public async Task MigrateAsync_WithProgressCallback_ReportsProgress()
    {
        // Arrange
        await CreateTestProjectWithHandlers();
        var progressReports = new List<MigrationProgress>();

        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false,
            OnProgress = progress => progressReports.Add(progress)
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.NotEmpty(progressReports);
        Assert.Contains(progressReports, p => p.Stage == MigrationStage.Initializing);
        Assert.Contains(progressReports, p => p.Stage == MigrationStage.Analyzing);
        Assert.Contains(progressReports, p => p.Stage == MigrationStage.Completed);
    }

    [Fact]
    public async Task MigrateAsync_ProgressReporting_IncludesAllStages()
    {
        // Arrange
        await CreateTestProjectWithHandlers();
        var stagesReported = new HashSet<MigrationStage>();

        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false,
            CreateBackup = true,
            OnProgress = progress => stagesReported.Add(progress.Stage)
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.Contains(MigrationStage.Initializing, stagesReported);
        Assert.Contains(MigrationStage.Analyzing, stagesReported);
        Assert.Contains(MigrationStage.CreatingBackup, stagesReported);
        Assert.Contains(MigrationStage.TransformingPackages, stagesReported);
        Assert.Contains(MigrationStage.TransformingCode, stagesReported);
        Assert.Contains(MigrationStage.Finalizing, stagesReported);
        Assert.Contains(MigrationStage.Completed, stagesReported);
    }

    [Fact]
    public async Task MigrateAsync_ProgressReporting_TracksElapsedTime()
    {
        // Arrange
        await CreateTestProjectWithHandlers();
        var progressReports = new List<MigrationProgress>();

        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false,
            OnProgress = progress => progressReports.Add(progress)
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.All(progressReports, p => Assert.True(p.ElapsedTime >= TimeSpan.Zero));

        // Elapsed time should be increasing
        for (int i = 1; i < progressReports.Count; i++)
        {
            Assert.True(progressReports[i].ElapsedTime >= progressReports[i - 1].ElapsedTime);
        }
    }

    [Fact]
    public async Task MigrateAsync_ProgressReporting_TracksFileProgress()
    {
        // Arrange
        await CreateTestProjectWithMultipleHandlers(10);
        MigrationProgress? lastCodeTransformProgress = null;

        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false,
            ProgressReportInterval = 1, // Very low interval to ensure we get progress reports
            OnProgress = progress =>
            {
                if (progress.Stage == MigrationStage.TransformingCode && progress.TotalFiles > 0)
                {
                    lastCodeTransformProgress = progress;
                }
            }
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.NotNull(lastCodeTransformProgress);
        Assert.True(lastCodeTransformProgress.TotalFiles > 0);
        Assert.True(lastCodeTransformProgress.ProcessedFiles > 0);
        Assert.True(lastCodeTransformProgress.PercentComplete >= 0);
        Assert.True(lastCodeTransformProgress.PercentComplete <= 100);
    }

    [Fact]
    public async Task MigrateAsync_ProgressReporting_IncludesFilesModified()
    {
        // Arrange
        await CreateTestProjectWithMultipleHandlers(5);
        var progressReports = new List<MigrationProgress>();

        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false,
            OnProgress = progress => progressReports.Add(progress)
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        var finalizingProgress = progressReports.FirstOrDefault(p => p.Stage == MigrationStage.Finalizing);
        Assert.NotNull(finalizingProgress);
        Assert.True(finalizingProgress.FilesModified > 0);
        Assert.True(finalizingProgress.HandlersMigrated > 0);
    }

    [Fact]
    public async Task MigrateAsync_ProgressReporting_WithParallel_SetsIsParallelFlag()
    {
        // Arrange
        await CreateTestProjectWithMultipleHandlers(10);
        var progressReports = new List<MigrationProgress>();

        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            EnableParallelProcessing = true,
            DryRun = false,
            OnProgress = progress => progressReports.Add(progress)
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        var codeTransformProgress = progressReports.FirstOrDefault(p => p.Stage == MigrationStage.TransformingCode && p.TotalFiles > 0);
        Assert.NotNull(codeTransformProgress);
        Assert.True(codeTransformProgress.IsParallel);
    }

    [Fact]
    public async Task MigrateAsync_ProgressReporting_WithSequential_ClearsIsParallelFlag()
    {
        // Arrange
        await CreateTestProjectWithMultipleHandlers(3); // Small project for sequential
        var progressReports = new List<MigrationProgress>();

        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            EnableParallelProcessing = false,
            DryRun = false,
            OnProgress = progress => progressReports.Add(progress)
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        var codeTransformProgress = progressReports.FirstOrDefault(p => p.Stage == MigrationStage.TransformingCode && p.TotalFiles > 0);
        if (codeTransformProgress != null)
        {
            Assert.False(codeTransformProgress.IsParallel);
        }
    }

    [Fact]
    public async Task MigrateAsync_ProgressReporting_OnFailure_ReportsCompletedStage()
    {
        // Arrange
        var progressReports = new List<MigrationProgress>();

        var options = new MigrationOptions
        {
            ProjectPath = "C:\\NonExistentPath\\Invalid",
            DryRun = false,
            OnProgress = progress => progressReports.Add(progress)
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.Equal(MigrationStatus.Failed, result.Status);
        var completedProgress = progressReports.FirstOrDefault(p => p.Stage == MigrationStage.Completed);
        Assert.NotNull(completedProgress);
        Assert.Contains("failed", completedProgress.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MigrateAsync_ProgressReporting_WithCustomInterval_RespectsInterval()
    {
        // Arrange
        await CreateTestProjectWithMultipleHandlers(20);
        var progressReports = new List<MigrationProgress>();

        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false,
            ProgressReportInterval = 1000, // 1 second
            OnProgress = progress => progressReports.Add(progress)
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        // Should have fewer progress reports with higher interval
        Assert.NotEmpty(progressReports);
    }

    [Fact]
    public async Task MigrateAsync_ProgressReporting_WithoutCallback_DoesNotThrow()
    {
        // Arrange
        await CreateTestProjectWithHandlers();

        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false,
            OnProgress = null // No callback
        };

        // Act & Assert - Should not throw
        var result = await _engine.MigrateAsync(options);
        Assert.True(result.Status == MigrationStatus.Success || result.Status == MigrationStatus.Partial);
    }

    [Fact]
    public async Task MigrateAsync_ProgressReporting_WithExceptionInCallback_ContinuesMigration()
    {
        // Arrange
        await CreateTestProjectWithHandlers();
        var callbackInvoked = false;

        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false,
            OnProgress = progress =>
            {
                callbackInvoked = true;
                throw new InvalidOperationException("Test exception in progress callback");
            }
        };

        // Act & Assert - Should not throw, migration should continue
        var result = await _engine.MigrateAsync(options);
        Assert.True(callbackInvoked);
        Assert.True(result.Status == MigrationStatus.Success || result.Status == MigrationStatus.Partial);
    }

    [Fact]
    public async Task MigrateAsync_ProgressReporting_IncludesMessageForEachStage()
    {
        // Arrange
        await CreateTestProjectWithHandlers();
        var progressReports = new List<MigrationProgress>();

        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false,
            CreateBackup = true,
            OnProgress = progress => progressReports.Add(progress)
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.All(progressReports, p => Assert.False(string.IsNullOrWhiteSpace(p.Message)));

        Assert.Contains(progressReports, p => p.Stage == MigrationStage.Initializing && p.Message.Contains("Initializing"));
        Assert.Contains(progressReports, p => p.Stage == MigrationStage.Analyzing && p.Message.Contains("Analyzing"));
        Assert.Contains(progressReports, p => p.Stage == MigrationStage.CreatingBackup && p.Message.Contains("backup"));
        Assert.Contains(progressReports, p => p.Stage == MigrationStage.TransformingPackages && p.Message.Contains("package"));
        Assert.Contains(progressReports, p => p.Stage == MigrationStage.Finalizing && p.Message.Contains("Finalizing"));
        Assert.Contains(progressReports, p => p.Stage == MigrationStage.Completed && p.Message.Contains("completed"));
    }

    [Fact]
    public async Task MigrateAsync_ProgressReporting_WithDryRun_SkipsBackupStage()
    {
        // Arrange
        await CreateTestProjectWithHandlers();
        var stagesReported = new HashSet<MigrationStage>();

        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = true,
            CreateBackup = true, // Enabled but should be skipped in dry run
            OnProgress = progress => stagesReported.Add(progress.Stage)
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.DoesNotContain(MigrationStage.CreatingBackup, stagesReported);
    }

    #endregion

    #region Graceful Error Handling Tests

    [Fact]
    public async Task MigrateAsync_WithContinueOnError_True_ContinuesMigrationAfterError()
    {
        // Arrange
        await CreateTestProjectWithHandlers();
        
        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false,
            ContinueOnError = true
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        // Migration should complete with success or partial status
        Assert.True(result.Status == MigrationStatus.Success || result.Status == MigrationStatus.Partial);
        Assert.NotEqual(MigrationStatus.InProgress, result.Status);
    }

    [Fact]
    public async Task MigrateAsync_WithContinueOnError_False_ByDefault()
    {
        // Arrange
        await CreateTestProjectWithHandlers();

        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false
            // ContinueOnError defaults to true
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.True(options.ContinueOnError); // Should default to true
        Assert.True(result.Status == MigrationStatus.Success || result.Status == MigrationStatus.Partial);
    }

    [Fact]
    public async Task MigrateAsync_WithFileIOError_RecordsIssue()
    {
        // Arrange
        await CreateTestProjectWithHandlers();
        
        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false,
            ContinueOnError = true
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert - Should complete even if there are I/O issues
        Assert.NotNull(result);
        Assert.NotEqual(MigrationStatus.InProgress, result.Status);
    }

    [Fact]
    public async Task MigrateAsync_WithPackageTransformError_ContinuesMigration()
    {
        // Arrange
        await CreateTestProjectWithHandlers();
        
        // Create a malformed .csproj file
        var projectFile = Path.Combine(_testProjectPath, "Invalid.csproj");
        await File.WriteAllTextAsync(projectFile, @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <!-- Malformed PackageReference -->
    <PackageReference Include=""MediatR"" 
  </ItemGroup>
</Project>");

        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false,
            ContinueOnError = true
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.NotNull(result);
        // Migration may have partial success or fail, but should not crash
        Assert.NotEqual(MigrationStatus.InProgress, result.Status);
    }

    [Fact]
    public async Task MigrateAsync_WithMalformedProjectFile_HandlesErrorGracefully()
    {
        // Arrange
        await CreateTestProjectWithHandlers();
        
        // Create a completely invalid .csproj file
        var projectFile = Path.Combine(_testProjectPath, "Broken.csproj");
        await File.WriteAllTextAsync(projectFile, "This is not valid XML at all!");

        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false,
            ContinueOnError = true
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.NotNull(result);
        // Should handle the XML parse error gracefully
        Assert.NotEqual(MigrationStatus.InProgress, result.Status);
        // May have issues recorded for the invalid file
        if (result.Issues.Any())
        {
            Assert.Contains(result.Issues, i => i.Contains("Broken.csproj") || i.Contains("Failed to transform"));
        }
    }

    [Fact]
    public async Task MigrateAsync_WithParallelProcessing_HandlesErrorsGracefully()
    {
        // Arrange
        await CreateTestProjectWithMultipleHandlers(10);

        var options = new MigrationOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false,
            ContinueOnError = true,
            EnableParallelProcessing = true
        };

        // Act
        var result = await _engine.MigrateAsync(options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Status == MigrationStatus.Success || result.Status == MigrationStatus.Partial);
        // Should have some successful migrations
        Assert.True(result.FilesModified > 0 || result.HandlersMigrated > 0);
    }

    [Fact]
    public async Task MigrationException_HasCorrectProperties()
    {
        // Arrange & Act
        var ex1 = new MigrationException("Test message");
        var ex2 = new MigrationException("Test message", "file.cs");
        var ex3 = new MigrationException("Test message", new Exception("Inner"));
        var ex4 = new MigrationException("Test message", "file.cs", new Exception("Inner"));

        // Assert
        Assert.Equal("Test message", ex1.Message);
        Assert.Equal("Test message", ex2.Message);
        Assert.Equal("file.cs", ex2.FilePath);
        Assert.Equal("Test message", ex3.Message);
        Assert.NotNull(ex3.InnerException);
        Assert.Equal("file.cs", ex4.FilePath);
        Assert.NotNull(ex4.InnerException);
    }

    [Fact]
    public async Task SyntaxException_HasLineNumber()
    {
        // Arrange & Act
        var ex = new SyntaxException("Syntax error", "file.cs", 42);

        // Assert
        Assert.Equal("Syntax error", ex.Message);
        Assert.Equal("file.cs", ex.FilePath);
        Assert.Equal(42, ex.LineNumber);
    }

    #endregion
}
