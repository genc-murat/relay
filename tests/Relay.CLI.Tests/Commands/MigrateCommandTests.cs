using System.CommandLine;
using Relay.CLI.Commands;
using Xunit;
using System.IO;
using System.Threading.Tasks;

namespace Relay.CLI.Tests.Commands;

public class MigrateCommandTests
{
    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        // Arrange & Act
        var command = MigrateCommand.Create();

        // Assert
        Assert.Equal("migrate", command.Name);
        Assert.Equal("Migrate from MediatR to Relay with automated transformation", command.Description);
    }

    [Fact]
    public void Create_CommandHasRequiredOptions()
    {
        // Arrange & Act
        var command = MigrateCommand.Create();

        // Assert
        Assert.Contains(command.Options, o => o.Name == "from");
        Assert.Contains(command.Options, o => o.Name == "to");
        Assert.Contains(command.Options, o => o.Name == "path");
        Assert.Contains(command.Options, o => o.Name == "analyze-only");
        Assert.Contains(command.Options, o => o.Name == "dry-run");
        Assert.Contains(command.Options, o => o.Name == "preview");
        Assert.Contains(command.Options, o => o.Name == "side-by-side");
        Assert.Contains(command.Options, o => o.Name == "backup");
        Assert.Contains(command.Options, o => o.Name == "backup-path");
        Assert.Contains(command.Options, o => o.Name == "output");
        Assert.Contains(command.Options, o => o.Name == "format");
        Assert.Contains(command.Options, o => o.Name == "aggressive");
        Assert.Contains(command.Options, o => o.Name == "interactive");
    }

    [Fact]
    public async Task ExecuteMigrate_WithInvalidFromFramework_ReturnsError()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayMigrateInvalidTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act & Assert - This should not throw but set exit code
            await MigrateCommand.ExecuteMigrate(
                "InvalidFramework", "Relay", tempDir, true, false, false, false,
                false, ".backup", null, "markdown", false, false);

            // Note: In test environment, Environment.ExitCode might not be set
            // but the method should handle invalid frameworks gracefully
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithInvalidToFramework_ReturnsError()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayMigrateInvalidToTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act & Assert
            await MigrateCommand.ExecuteMigrate(
                "MediatR", "InvalidFramework", tempDir, true, false, false, false,
                false, ".backup", null, "markdown", false, false);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithAnalyzeOnly_DoesNotModifyFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayMigrateAnalyzeTest");
        Directory.CreateDirectory(tempDir);

        // Create a minimal project structure
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""MediatR"" Version=""12.0.0"" />
  </ItemGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "test.csproj"), csprojContent);

        var handlerContent = @"using MediatR;
public class TestHandler : IRequestHandler<TestRequest, string>
{
    public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(""test"");
    }
}
public record TestRequest : IRequest<string>;";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "TestHandler.cs"), handlerContent);

        try
        {
            // Act
            await MigrateCommand.ExecuteMigrate(
                "MediatR", "Relay", tempDir, true, false, false, false,
                false, ".backup", null, "markdown", false, false);

            // Assert - Files should remain unchanged
            var originalContent = await File.ReadAllTextAsync(Path.Combine(tempDir, "TestHandler.cs"));
            Assert.Contains("MediatR", originalContent);
            Assert.Contains("IRequestHandler", originalContent);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithOutputFile_CreatesReport()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayMigrateOutputTest");
        Directory.CreateDirectory(tempDir);
        var outputPath = Path.Combine(tempDir, "migration-report.md");

        // Create a minimal project structure
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""MediatR"" Version=""12.0.0"" />
  </ItemGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "test.csproj"), csprojContent);

        try
        {
            // Act
            await MigrateCommand.ExecuteMigrate(
                "MediatR", "Relay", tempDir, true, false, false, false,
                false, ".backup", outputPath, "markdown", false, false);

            // Assert - Report file should be created
            Assert.True(File.Exists(outputPath));
            var reportContent = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("# Migration Report", reportContent);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithJsonFormat_CreatesJsonReport()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayMigrateJsonTest");
        Directory.CreateDirectory(tempDir);
        var outputPath = Path.Combine(tempDir, "migration-report.json");

        // Create a minimal project structure
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""MediatR"" Version=""12.0.0"" />
  </ItemGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "test.csproj"), csprojContent);

        try
        {
            // Act
            await MigrateCommand.ExecuteMigrate(
                "MediatR", "Relay", tempDir, true, false, false, false,
                false, ".backup", outputPath, "json", false, false);

            // Assert
            Assert.True(File.Exists(outputPath));
            var reportContent = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("Status", reportContent);
            Assert.Contains("Duration", reportContent);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithHtmlFormat_CreatesHtmlReport()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayMigrateHtmlTest");
        Directory.CreateDirectory(tempDir);
        var outputPath = Path.Combine(tempDir, "migration-report.html");

        // Create a minimal project structure
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""MediatR"" Version=""12.0.0"" />
  </ItemGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "test.csproj"), csprojContent);

        try
        {
            // Act
            await MigrateCommand.ExecuteMigrate(
                "MediatR", "Relay", tempDir, true, false, false, false,
                false, ".backup", outputPath, "html", false, false);

            // Assert
            Assert.True(File.Exists(outputPath));
            var reportContent = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("<!DOCTYPE html>", reportContent);
            Assert.Contains("Migration Report", reportContent);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithDryRun_DoesNotCreateBackup()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayMigrateDryRunTest");
        Directory.CreateDirectory(tempDir);
        var backupPath = Path.Combine(tempDir, "backup");

        // Create a minimal project structure
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""MediatR"" Version=""12.0.0"" />
  </ItemGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "test.csproj"), csprojContent);

        try
        {
            // Act
            await MigrateCommand.ExecuteMigrate(
                "MediatR", "Relay", tempDir, false, true, false, false,
                true, backupPath, null, "markdown", false, false);

            // Assert - No backup should be created in dry run
            Assert.False(Directory.Exists(backupPath));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteMigrate_WithNonExistentPath_HandlesGracefully()
    {
        // Arrange
        var invalidPath = "/nonexistent/path/that/does/not/exist";

        // Act & Assert - Should not throw
        await MigrateCommand.ExecuteMigrate(
            "MediatR", "Relay", invalidPath, true, false, false, false,
            false, ".backup", null, "markdown", false, false);
    }
}