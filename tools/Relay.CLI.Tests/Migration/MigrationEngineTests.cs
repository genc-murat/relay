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
        result.Should().NotBeNull();
        result.CanMigrate.Should().BeFalse();
        result.Issues.Should().Contain(i => i.Code == "NO_PROJECT");
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
        result.Should().NotBeNull();
        result.ProjectPath.Should().Be(_testProjectPath);
        result.AnalysisDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
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
        result.PackageReferences.Should().NotBeEmpty();
        result.PackageReferences.Should().Contain(p => p.Name == "MediatR");
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
        result.HandlersFound.Should().BeGreaterThan(0);
        result.RequestsFound.Should().BeGreaterThan(0);
        result.FilesAffected.Should().BeGreaterThan(0);
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
        backupPath.Should().NotBeNullOrEmpty();
        Directory.Exists(backupPath).Should().BeTrue();
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
        backupPath.Should().Contain("backup_");
        backupPath.Should().Match(p => System.Text.RegularExpressions.Regex.IsMatch(p, @"backup_\d{8}_\d{6}"));
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
        File.Exists(backupFile).Should().BeTrue();
        var content = await File.ReadAllTextAsync(backupFile);
        content.Should().Be("// test content");
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
        result.Status.Should().Be(MigrationStatus.Failed);
        result.Issues.Should().NotBeEmpty();
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
        result.Status.Should().BeOneOf(MigrationStatus.Success, MigrationStatus.Partial);
        result.Changes.Should().NotBeEmpty();

        var currentContent = await File.ReadAllTextAsync(Path.Combine(_testProjectPath, "Handler.cs"));
        currentContent.Should().Be(originalContent);
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
        result.CreatedBackup.Should().BeTrue();
        result.BackupPath.Should().NotBeNullOrEmpty();
        Directory.Exists(result.BackupPath).Should().BeTrue();
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
        result.CreatedBackup.Should().BeFalse();
        result.BackupPath.Should().BeNullOrEmpty();
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
        result.Status.Should().BeOneOf(MigrationStatus.Success, MigrationStatus.Partial);
        result.HandlersMigrated.Should().BeGreaterThan(0);
        result.FilesModified.Should().BeGreaterThan(0);
        result.Changes.Should().Contain(c => c.Category == "Using Directives");
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
        result.Changes.Should().Contain(c => c.Category == "Package References");
        result.Changes.Should().Contain(c => c.Type == ChangeType.Remove && c.Description.Contains("MediatR"));
        result.Changes.Should().Contain(c => c.Type == ChangeType.Add && c.Description.Contains("Relay.Core"));
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
        result.StartTime.Should().BeBefore(result.EndTime);
        result.Duration.Should().BePositive();
        result.Duration.Should().BeLessThan(TimeSpan.FromMinutes(1)); // Reasonable timeout
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
        result.ManualSteps.Should().Contain(s => s.Contains("custom IMediator"));
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
        result.ManualSteps.Should().Contain(s => s.Contains("pipeline behaviors"));
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
        result.Changes.Should().NotContain(c => c.FilePath.Contains("\\bin\\"));
        result.Changes.Should().NotContain(c => c.FilePath.Contains("\\obj\\"));
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
        result.Status.Should().Be(MigrationStatus.Failed);
        result.Issues.Should().Contain(i => i.Contains("failed"));
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
        result.Should().BeTrue();
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().NotContain("Modified");
    }

    [Fact]
    public async Task RollbackAsync_WithInvalidBackup_ReturnsFalse()
    {
        // Arrange
        var invalidPath = Path.Combine(_testProjectPath, "nonexistent-backup");

        // Act
        var result = await _engine.RollbackAsync(invalidPath);

        // Assert
        result.Should().BeFalse();
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
}
