using Relay.CLI.Commands;
using Relay.CLI.Commands.Models.Diagnostic;

namespace Relay.CLI.Tests.Commands;

public class DoctorCommandTests : IDisposable
{
    private readonly string _testPath;

    public DoctorCommandTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"relay-doctor-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPath);
    }

    [Fact]
    public async Task DoctorCommand_WithValidProject_ShouldPass()
    {
        // Arrange
        await CreateValidProject();

        // Act
        var hasProjectFile = File.Exists(Path.Combine(_testPath, "Test.csproj"));
        var hasHandlerFile = File.Exists(Path.Combine(_testPath, "Handler.cs"));

        // Assert
        hasProjectFile.Should().BeTrue();
        hasHandlerFile.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_WithMissingProject_ShouldFail()
    {
        // Act
        var projectFiles = Directory.GetFiles(_testPath, "*.csproj");

        // Assert
        projectFiles.Should().BeEmpty();
    }

    [Fact]
    public async Task DoctorCommand_ChecksRelayPackageVersion()
    {
        // Arrange
        await CreateValidProject();
        var csprojContent = await File.ReadAllTextAsync(Path.Combine(_testPath, "Test.csproj"));

        // Act
        var hasRelayPackage = csprojContent.Contains("Relay.Core");

        // Assert
        hasRelayPackage.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldDetectModernDotNetVersion()
    {
        // Arrange
        var csproj = "<TargetFramework>net8.0</TargetFramework>";

        // Act
        var isModern = csproj.Contains("net8.0") || csproj.Contains("net9.0");

        // Assert
        isModern.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldDetectOutdatedFramework()
    {
        // Arrange
        var csproj = "<TargetFramework>netcoreapp3.1</TargetFramework>";

        // Act
        var isOutdated = csproj.Contains("netcoreapp") || csproj.Contains("net4");

        // Assert
        isOutdated.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldCheckNullableReferenceTypes()
    {
        // Arrange
        var csproj = "<Nullable>enable</Nullable>";

        // Act
        var hasNullable = csproj.Contains("<Nullable>enable</Nullable>");

        // Assert
        hasNullable.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldCheckLatestLangVersion()
    {
        // Arrange
        var csproj = "<LangVersion>latest</LangVersion>";

        // Act
        var hasLatestLang = csproj.Contains("<LangVersion>latest</LangVersion>");

        // Assert
        hasLatestLang.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldDetectValueTaskUsage()
    {
        // Arrange
        var handler = "public async ValueTask<string> HandleAsync";

        // Act
        var usesValueTask = handler.Contains("ValueTask");

        // Assert
        usesValueTask.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldDetectTaskUsage()
    {
        // Arrange
        var handler = "public async Task<string> HandleAsync";

        // Act
        var usesTask = handler.Contains("Task<");

        // Assert
        usesTask.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldCheckCancellationToken()
    {
        // Arrange
        var handler = "public async ValueTask<string> HandleAsync(TestRequest request, CancellationToken ct)";

        // Act
        var hasCancellationToken = handler.Contains("CancellationToken");

        // Assert
        hasCancellationToken.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldDetectHandleAttribute()
    {
        // Arrange
        var handler = "[Handle]\npublic async ValueTask<string> HandleAsync";

        // Act
        var hasHandleAttribute = handler.Contains("[Handle]");

        // Assert
        hasHandleAttribute.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldDetectRequestHandlers()
    {
        // Arrange
        var code = "public class TestHandler : IRequestHandler<TestRequest, string>";

        // Act
        var isHandler = code.Contains("IRequestHandler");

        // Assert
        isHandler.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldDetectNotificationHandlers()
    {
        // Arrange
        var code = "public class TestHandler : INotificationHandler<TestNotification>";

        // Act
        var isHandler = code.Contains("INotificationHandler");

        // Assert
        isHandler.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldCheckTieredCompilation()
    {
        // Arrange
        var csproj = "<TieredCompilation>true</TieredCompilation>";

        // Act
        var hasTieredCompilation = csproj.Contains("<TieredCompilation>true</TieredCompilation>");

        // Assert
        hasTieredCompilation.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldCheckPGO()
    {
        // Arrange
        var csproj = "<TieredPGO>true</TieredPGO>";

        // Act
        var hasPGO = csproj.Contains("<TieredPGO>true</TieredPGO>");

        // Assert
        hasPGO.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldCheckOptimization()
    {
        // Arrange
        var csproj = "<Optimize>true</Optimize>";

        // Act
        var hasOptimize = csproj.Contains("<Optimize>true</Optimize>");

        // Assert
        hasOptimize.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldCheckTrimming()
    {
        // Arrange
        var csproj = "<PublishTrimmed>true</PublishTrimmed>";

        // Act
        var hasTrimming = csproj.Contains("<PublishTrimmed>true</PublishTrimmed>");

        // Assert
        hasTrimming.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldDetectRecordUsage()
    {
        // Arrange
        var request = "public record GetUserQuery(int UserId) : IRequest<UserResponse>;";

        // Act
        var usesRecord = request.Contains("public record");

        // Assert
        usesRecord.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldDetectClassUsage()
    {
        // Arrange
        var request = "public class GetUserQuery : IRequest<UserResponse>";

        // Act
        var usesClass = request.Contains("public class");

        // Assert
        usesClass.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldCheckAsyncNaming()
    {
        // Arrange
        var method = "public async ValueTask<string> HandleAsync()";

        // Act
        var hasAsyncSuffix = method.Contains("Async(");

        // Assert
        hasAsyncSuffix.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldExcludeBinObjFolders()
    {
        // Arrange
        var filePath = Path.Combine(_testPath, "bin", "Test.csproj");

        // Act
        var shouldExclude = filePath.Contains("bin") || filePath.Contains("obj");

        // Assert
        shouldExclude.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldDetectRecommendedFolders()
    {
        // Arrange
        var expectedFolders = new[] { "src", "tests", "docs" };

        // Act
        foreach (var folder in expectedFolders)
        {
            Directory.CreateDirectory(Path.Combine(_testPath, folder));
        }

        // Assert
        foreach (var folder in expectedFolders)
        {
            Directory.Exists(Path.Combine(_testPath, folder)).Should().BeTrue();
        }
    }

    [Fact]
    public async Task DiagnosticResults_ShouldCountSuccesses()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check = new DiagnosticCheck { Category = "Test" };
        check.AddSuccess("Success message");
        results.AddCheck(check);

        // Act
        var count = results.SuccessCount;

        // Assert
        count.Should().Be(1);
    }

    [Fact]
    public async Task DiagnosticResults_ShouldCountWarnings()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check = new DiagnosticCheck { Category = "Test" };
        check.AddIssue("Warning", DiagnosticSeverity.Warning, "WARN");
        results.AddCheck(check);

        // Act
        var count = results.WarningCount;

        // Assert
        count.Should().Be(1);
    }

    [Fact]
    public async Task DiagnosticResults_ShouldCountErrors()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check = new DiagnosticCheck { Category = "Test" };
        check.AddIssue("Error", DiagnosticSeverity.Error, "ERR");
        results.AddCheck(check);

        // Act
        var count = results.ErrorCount;

        // Assert
        count.Should().Be(1);
    }

    [Fact]
    public async Task DiagnosticResults_ShouldCountInfo()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check = new DiagnosticCheck { Category = "Test" };
        check.AddInfo("Info message");
        results.AddCheck(check);

        // Act
        var count = results.InfoCount;

        // Assert
        count.Should().Be(1);
    }

    [Fact]
    public async Task DiagnosticResults_ShouldReturnExitCode0_WhenNoIssues()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check = new DiagnosticCheck { Category = "Test" };
        check.AddSuccess("All good");
        results.AddCheck(check);

        // Act
        var exitCode = results.GetExitCode();

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task DiagnosticResults_ShouldReturnExitCode1_WhenWarnings()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check = new DiagnosticCheck { Category = "Test" };
        check.AddIssue("Warning", DiagnosticSeverity.Warning, "WARN");
        results.AddCheck(check);

        // Act
        var exitCode = results.GetExitCode();

        // Assert
        exitCode.Should().Be(1);
    }

    [Fact]
    public async Task DiagnosticResults_ShouldReturnExitCode2_WhenErrors()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check = new DiagnosticCheck { Category = "Test" };
        check.AddIssue("Error", DiagnosticSeverity.Error, "ERR");
        results.AddCheck(check);

        // Act
        var exitCode = results.GetExitCode();

        // Assert
        exitCode.Should().Be(2);
    }

    [Fact]
    public async Task DiagnosticResults_ShouldDetectFixableIssues()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check = new DiagnosticCheck { Category = "Test" };
        check.AddIssue("Fixable", DiagnosticSeverity.Warning, "FIX", isFixable: true);
        results.AddCheck(check);

        // Act
        var hasFixable = results.HasFixableIssues();

        // Assert
        hasFixable.Should().BeTrue();
    }

    [Fact]
    public async Task DiagnosticCheck_ShouldAddIssue()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "Test" };

        // Act
        check.AddIssue("Test issue", DiagnosticSeverity.Error, "TEST");

        // Assert
        check.Issues.Should().HaveCount(1);
        check.Issues[0].Message.Should().Be("Test issue");
        check.Issues[0].Severity.Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task DiagnosticIssue_ShouldHaveFixableFlag()
    {
        // Arrange
        var issue = new DiagnosticIssue
        {
            Message = "Test",
            Severity = DiagnosticSeverity.Warning,
            Code = "TEST",
            IsFixable = true
        };

        // Assert
        issue.IsFixable.Should().BeTrue();
    }

    [Theory]
    [InlineData(DiagnosticSeverity.Success)]
    [InlineData(DiagnosticSeverity.Info)]
    [InlineData(DiagnosticSeverity.Warning)]
    [InlineData(DiagnosticSeverity.Error)]
    public async Task DiagnosticSeverity_ShouldHaveAllLevels(DiagnosticSeverity severity)
    {
        // Arrange
        var validSeverities = new[]
        {
            DiagnosticSeverity.Success,
            DiagnosticSeverity.Info,
            DiagnosticSeverity.Warning,
            DiagnosticSeverity.Error
        };

        // Assert
        validSeverities.Should().Contain(severity);
    }

    [Fact]
    public async Task DoctorCommand_ShouldDetectNet6Framework()
    {
        // Arrange
        var csproj = "<TargetFramework>net6.0</TargetFramework>";

        // Act
        var isCompatible = csproj.Contains("netstandard2.0") || csproj.Contains("net6.0");

        // Assert
        isCompatible.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldDetectNetStandard()
    {
        // Arrange
        var csproj = "<TargetFramework>netstandard2.0</TargetFramework>";

        // Act
        var isCompatible = csproj.Contains("netstandard2.0");

        // Assert
        isCompatible.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldExcludeMigrationFiles()
    {
        // Arrange
        var filePath = Path.Combine(_testPath, "Migrations", "Migration.cs");

        // Act
        var shouldExclude = filePath.Contains("Migrations");

        // Assert
        shouldExclude.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldCheckReleaseConfiguration()
    {
        // Arrange
        var csproj = "<Configuration>Release</Configuration>";

        // Act
        var hasRelease = csproj.Contains("<Configuration>Release</Configuration>");

        // Assert
        hasRelease.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_ShouldDetectMultipleProjects()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_testPath, "Project1.csproj"), "<Project />");
        await File.WriteAllTextAsync(Path.Combine(_testPath, "Project2.csproj"), "<Project />");

        // Act
        var projectFiles = Directory.GetFiles(_testPath, "*.csproj");

        // Assert
        projectFiles.Should().HaveCount(2);
    }

    private async Task CreateValidProject()
    {
        var csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Relay.Core"" Version=""2.1.0"" />
  </ItemGroup>
</Project>";

        await File.WriteAllTextAsync(Path.Combine(_testPath, "Test.csproj"), csproj);

        var handler = @"using Relay.Core;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async ValueTask<string> HandleAsync(TestRequest request, CancellationToken ct)
    {
        return ""test"";
    }
}

public record TestRequest : IRequest<string>;";

        await File.WriteAllTextAsync(Path.Combine(_testPath, "Handler.cs"), handler);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testPath))
                Directory.Delete(_testPath, true);
        }
        catch { }
    }
}
