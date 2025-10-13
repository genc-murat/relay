using System.CommandLine;
using Relay.CLI.Commands;
using Xunit;
using System.IO;
using System.Threading.Tasks;
using Relay.CLI.Commands.Models.Diagnostic;

namespace Relay.CLI.Tests.Commands;

public class DoctorCommandTests
{
    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        // Arrange & Act
        var command = DoctorCommand.Create();

        // Assert
        Assert.Equal("doctor", command.Name);
        Assert.Equal("Run comprehensive health check on your Relay project", command.Description);
    }

    [Fact]
    public void Create_CommandHasRequiredOptions()
    {
        // Arrange & Act
        var command = DoctorCommand.Create();

        // Assert
        Assert.Contains(command.Options, o => o.Name == "path");
        Assert.Contains(command.Options, o => o.Name == "verbose");
        Assert.Contains(command.Options, o => o.Name == "fix");
    }

    [Fact]
    public async Task ExecuteDoctor_WithValidProjectPath_CompletesSuccessfully()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayDoctorTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create a minimal .csproj file
            var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Relay.Core"" Version=""1.0.0"" />
  </ItemGroup>
</Project>";
            await File.WriteAllTextAsync(Path.Combine(tempDir, "test.csproj"), csprojContent);

            // Create a handler file
            var handlerContent = @"using Relay.Core;
public class TestHandler : IRequestHandler<TestRequest, string>
{
    public ValueTask<string> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}
public record TestRequest : IRequest<string>;";
            await File.WriteAllTextAsync(Path.Combine(tempDir, "TestHandler.cs"), handlerContent);

            // Act & Assert - This will test the basic execution path
            // Note: Since ExecuteDoctor uses console output, we can't easily test the full output
            // but we can ensure it doesn't throw exceptions
            await DoctorCommand.ExecuteDoctor(tempDir, false, false);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteDoctor_WithInvalidPath_HandlesError()
    {
        // Arrange
        var invalidPath = "/nonexistent/path";

        // Act & Assert
        await DoctorCommand.ExecuteDoctor(invalidPath, false, false);

        // The method should complete without throwing unhandled exceptions
        // Environment.ExitCode will be set based on errors found
        Assert.True(Environment.ExitCode >= 0);
    }

    [Fact]
    public void DiagnosticResults_CountsWorkCorrectly()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check1 = new DiagnosticCheck { Category = "Test1" };
        check1.AddSuccess("Success message");
        check1.AddIssue("Warning message", DiagnosticSeverity.Warning, "WARN001");
        check1.AddIssue("Error message", DiagnosticSeverity.Error, "ERR001");

        var check2 = new DiagnosticCheck { Category = "Test2" };
        check2.AddInfo("Info message");

        results.AddCheck(check1);
        results.AddCheck(check2);

        // Assert
        Assert.Equal(1, results.SuccessCount);
        Assert.Equal(1, results.InfoCount);
        Assert.Equal(1, results.WarningCount);
        Assert.Equal(1, results.ErrorCount);
        Assert.Equal(2, results.GetExitCode()); // Has errors
    }

    [Fact]
    public void DiagnosticResults_HasFixableIssues_ReturnsTrueWhenFixableIssuesExist()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check = new DiagnosticCheck { Category = "Test" };
        check.AddIssue("Fixable issue", DiagnosticSeverity.Warning, "FIX001", isFixable: true);

        results.AddCheck(check);

        // Assert
        Assert.True(results.HasFixableIssues());
    }

    [Fact]
    public void DiagnosticResults_HasFixableIssues_ReturnsFalseWhenNoFixableIssues()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check = new DiagnosticCheck { Category = "Test" };
        check.AddIssue("Non-fixable issue", DiagnosticSeverity.Warning, "WARN001", isFixable: false);

        results.AddCheck(check);

        // Assert
        Assert.False(results.HasFixableIssues());
    }

    [Fact]
    public void DiagnosticCheck_AddMethods_WorkCorrectly()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "Test" };

        // Act
        check.AddSuccess("Success");
        check.AddInfo("Info");
        check.AddIssue("Warning", DiagnosticSeverity.Warning, "WARN001");
        check.AddIssue("Error", DiagnosticSeverity.Error, "ERR001");

        // Assert
        Assert.Equal(4, check.Issues.Count);
        Assert.Equal("Success", check.Issues[0].Message);
        Assert.Equal(DiagnosticSeverity.Success, check.Issues[0].Severity);
        Assert.Equal("SUCCESS", check.Issues[0].Code);

        Assert.Equal("Info", check.Issues[1].Message);
        Assert.Equal(DiagnosticSeverity.Info, check.Issues[1].Severity);
        Assert.Equal("INFO", check.Issues[1].Code);

        Assert.Equal("Warning", check.Issues[2].Message);
        Assert.Equal(DiagnosticSeverity.Warning, check.Issues[2].Severity);

        Assert.Equal("Error", check.Issues[3].Message);
        Assert.Equal(DiagnosticSeverity.Error, check.Issues[3].Severity);
    }

    [Fact]
    public async Task CheckProjectStructure_WithValidProject_AddsSuccessIssues()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayProjectTest");
        Directory.CreateDirectory(tempDir);
        var results = new DiagnosticResults();

        try
        {
            // Create a valid .csproj file with Relay reference
            var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Relay.Core"" Version=""1.0.0"" />
  </ItemGroup>
</Project>";
            await File.WriteAllTextAsync(Path.Combine(tempDir, "test.csproj"), csprojContent);

            // Act
            await DoctorCommand.CheckProjectStructure(tempDir, results, false);

            // Assert
            var check = results.Checks.First(c => c.Category == "Project Structure");
            Assert.Contains(check.Issues, i => i.Message.Contains("Found 1 project file"));
            Assert.Contains(check.Issues, i => i.Message.Contains("Relay reference found"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CheckProjectStructure_WithNoProjects_AddsErrorIssue()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayEmptyTest");
        Directory.CreateDirectory(tempDir);
        var results = new DiagnosticResults();

        try
        {
            // Act
            await DoctorCommand.CheckProjectStructure(tempDir, results, false);

            // Assert
            var check = results.Checks.First(c => c.Category == "Project Structure");
            Assert.Contains(check.Issues, i => i.Severity == DiagnosticSeverity.Error && i.Code == "NOT_A_PROJECT");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CheckDependencies_WithModernFramework_AddsSuccessIssues()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayDepsTest");
        Directory.CreateDirectory(tempDir);
        var results = new DiagnosticResults();

        try
        {
            // Create a .csproj file with modern .NET version
            var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
</Project>";
            await File.WriteAllTextAsync(Path.Combine(tempDir, "test.csproj"), csprojContent);

            // Act
            await DoctorCommand.CheckDependencies(tempDir, results, false);

            // Assert
            var check = results.Checks.First(c => c.Category == "Dependencies");
            Assert.Contains(check.Issues, i => i.Message.Contains("Modern .NET version detected"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CheckDependencies_WithOutdatedFramework_AddsWarningIssue()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayOldDepsTest");
        Directory.CreateDirectory(tempDir);
        var results = new DiagnosticResults();

        try
        {
            // Create a .csproj file with old .NET version
            var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netcoreapp3.1</TargetFramework>
  </PropertyGroup>
</Project>";
            await File.WriteAllTextAsync(Path.Combine(tempDir, "test.csproj"), csprojContent);

            // Act
            await DoctorCommand.CheckDependencies(tempDir, results, false);

            // Assert
            var check = results.Checks.First(c => c.Category == "Dependencies");
            Assert.Contains(check.Issues, i => i.Severity == DiagnosticSeverity.Warning && i.Code == "OUTDATED_FRAMEWORK");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CheckHandlers_WithValidHandlers_AddsSuccessIssues()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayHandlersTest");
        Directory.CreateDirectory(tempDir);
        var results = new DiagnosticResults();

        try
        {
            // Create handler files
            var handlerContent = @"using Relay.Core;
public class TestHandler : IRequestHandler<TestRequest, string>
{
    public ValueTask<string> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}
public record TestRequest : IRequest<string>;

[Handle]
public class OptimizedHandler : IRequestHandler<OptimizedRequest, int>
{
    public ValueTask<int> Handle(OptimizedRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(42);
    }
}
public record OptimizedRequest : IRequest<int>;";
            await File.WriteAllTextAsync(Path.Combine(tempDir, "TestHandler.cs"), handlerContent);

            // Act
            await DoctorCommand.CheckHandlers(tempDir, results, true);

            // Assert
            var check = results.Checks.First(c => c.Category == "Handlers");
            Assert.Contains(check.Issues, i => i.Message.Contains("Found 1 handler(s)"));
            Assert.Contains(check.Issues, i => i.Message.Contains("using ValueTask (optimal)"));
            Assert.Contains(check.Issues, i => i.Message.Contains("uses [Handle] attribute"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CheckHandlers_WithTaskInsteadOfValueTask_AddsInfoIssue()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayTaskHandlersTest");
        Directory.CreateDirectory(tempDir);
        var results = new DiagnosticResults();

        try
        {
            // Create handler file using Task instead of ValueTask
            var handlerContent = @"using Relay.Core;
public class TaskHandler : IRequestHandler<TaskRequest, string>
{
    public Task<string> Handle(TaskRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(""test"");
    }
}
public record TaskRequest : IRequest<string>;";
            await File.WriteAllTextAsync(Path.Combine(tempDir, "TaskHandler.cs"), handlerContent);

            // Act
            await DoctorCommand.CheckHandlers(tempDir, results, false);

            // Assert
            var check = results.Checks.First(c => c.Category == "Handlers");
            Assert.Contains(check.Issues, i => i.Severity == DiagnosticSeverity.Info && i.Code == "USE_VALUETASK");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CheckHandlers_WithMissingCancellationToken_AddsWarningIssue()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayNoCancelTest");
        Directory.CreateDirectory(tempDir);
        var results = new DiagnosticResults();

        try
        {
            // Create handler file without CancellationToken
            var handlerContent = @"using Relay.Core;
public class NoCancelHandler : IRequestHandler<NoCancelRequest, string>
{
    public ValueTask<string> Handle(NoCancelRequest request)
    {
        return ValueTask.FromResult(""test"");
    }
}
public record NoCancelRequest : IRequest<string>;";
            await File.WriteAllTextAsync(Path.Combine(tempDir, "NoCancelHandler.cs"), handlerContent);

            // Act
            await DoctorCommand.CheckHandlers(tempDir, results, false);

            // Assert
            var check = results.Checks.First(c => c.Category == "Handlers");
            Assert.Contains(check.Issues, i => i.Severity == DiagnosticSeverity.Warning && i.Code == "MISSING_CANCELLATION_TOKEN");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CheckPerformanceSettings_WithOptimizations_AddsSuccessIssues()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayPerfTest");
        Directory.CreateDirectory(tempDir);
        var results = new DiagnosticResults();

        try
        {
            // Create a .csproj file with performance optimizations
            var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <TieredCompilation>true</TieredCompilation>
    <TieredPGO>true</TieredPGO>
    <Optimize>true</Optimize>
    <PublishTrimmed>true</PublishTrimmed>
  </PropertyGroup>
</Project>";
            await File.WriteAllTextAsync(Path.Combine(tempDir, "test.csproj"), csprojContent);

            // Act
            await DoctorCommand.CheckPerformanceSettings(tempDir, results, true);

            // Assert
            var check = results.Checks.First(c => c.Category == "Performance");
            Assert.Contains(check.Issues, i => i.Message.Contains("Tiered compilation enabled"));
            Assert.Contains(check.Issues, i => i.Message.Contains("Profile-guided optimization enabled"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CheckBestPractices_WithRecords_AddsSuccessIssues()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayBestPracticesTest");
        Directory.CreateDirectory(tempDir);
        var results = new DiagnosticResults();

        try
        {
            // Create request files using records
            var requestContent = @"using Relay.Core;
public record UserRequest(string Name, int Age) : IRequest<UserResponse>;
public record UserResponse(string Message);";
            await File.WriteAllTextAsync(Path.Combine(tempDir, "UserRequest.cs"), requestContent);

            // Act
            await DoctorCommand.CheckBestPractices(tempDir, results, true);

            // Assert
            var check = results.Checks.First(c => c.Category == "Best Practices");
            Assert.Contains(check.Issues, i => i.Message.Contains("using record (recommended)"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CheckBestPractices_WithClassesInsteadOfRecords_AddsInfoIssues()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayClassTest");
        Directory.CreateDirectory(tempDir);
        var results = new DiagnosticResults();

        try
        {
            // Create request files using classes instead of records
            var requestContent = @"using Relay.Core;
public class ClassRequest : IRequest<ClassResponse>
{
    public string Name { get; set; }
    public int Age { get; set; }
}
public class ClassResponse
{
    public string Message { get; set; }
}";
            await File.WriteAllTextAsync(Path.Combine(tempDir, "ClassRequest.cs"), requestContent);

            // Act
            await DoctorCommand.CheckBestPractices(tempDir, results, true);

            // Assert
            var check = results.Checks.First(c => c.Category == "Best Practices");
            Assert.Contains(check.Issues, i => i.Message.Contains("Consider using 'record' instead of 'class'"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}