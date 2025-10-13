using System.CommandLine;
using Relay.CLI.Commands;
using Xunit;
using System.IO;
using System.Threading.Tasks;

namespace Relay.CLI.Tests.Commands;

public class RefactorCommandTests
{
    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();

        // Assert
        Assert.Equal("refactor", command.Name);
        Assert.Equal("Automated code refactoring and modernization", command.Description);
    }

    [Fact]
    public void Create_CommandHasRequiredOptions()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();

        // Assert
        Assert.Contains(command.Options, o => o.Name == "path");
        Assert.Contains(command.Options, o => o.Name == "analyze-only");
        Assert.Contains(command.Options, o => o.Name == "dry-run");
        Assert.Contains(command.Options, o => o.Name == "interactive");
        Assert.Contains(command.Options, o => o.Name == "rules");
        Assert.Contains(command.Options, o => o.Name == "category");
        Assert.Contains(command.Options, o => o.Name == "min-severity");
        Assert.Contains(command.Options, o => o.Name == "output");
        Assert.Contains(command.Options, o => o.Name == "format");
        Assert.Contains(command.Options, o => o.Name == "backup");
    }

    [Fact]
    public async Task ExecuteRefactor_WithAnalyzeOnly_DoesNotModifyFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayRefactorAnalyzeTest");
        Directory.CreateDirectory(tempDir);

        // Create a C# file
        var csContent = @"public class Test { public void Method() { } }";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "Test.cs"), csContent);

        try
        {
            // Act
            await RefactorCommand.ExecuteRefactor(
                tempDir, true, false, false, Array.Empty<string>(),
                Array.Empty<string>(), "Info", null, "markdown", false);

            // Assert - File should remain unchanged
            var contentAfter = await File.ReadAllTextAsync(Path.Combine(tempDir, "Test.cs"));
            Assert.Equal(csContent, contentAfter);
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
    public async Task ExecuteRefactor_WithDryRun_DoesNotModifyFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayRefactorDryRunTest");
        Directory.CreateDirectory(tempDir);

        // Create a C# file
        var csContent = @"public class Test { public void Method() { } }";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "Test.cs"), csContent);

        try
        {
            // Act
            await RefactorCommand.ExecuteRefactor(
                tempDir, false, true, false, Array.Empty<string>(),
                Array.Empty<string>(), "Info", null, "markdown", false);

            // Assert - File should remain unchanged
            var contentAfter = await File.ReadAllTextAsync(Path.Combine(tempDir, "Test.cs"));
            Assert.Equal(csContent, contentAfter);
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
    public async Task ExecuteRefactor_WithOutputFile_CreatesReport()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayRefactorOutputTest");
        Directory.CreateDirectory(tempDir);
        var outputPath = Path.Combine(tempDir, "refactor-report.md");

        // Create a C# file
        var csContent = @"public class Test { public void Method() { } }";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "Test.cs"), csContent);

        try
        {
            // Act
            await RefactorCommand.ExecuteRefactor(
                tempDir, true, false, false, Array.Empty<string>(),
                Array.Empty<string>(), "Info", outputPath, "markdown", false);

            // Assert
            Assert.True(File.Exists(outputPath));
            var reportContent = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("# Code Refactoring Report", reportContent);
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
    public async Task ExecuteRefactor_WithJsonFormat_CreatesJsonReport()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayRefactorJsonTest");
        Directory.CreateDirectory(tempDir);
        var outputPath = Path.Combine(tempDir, "refactor-report.json");

        // Create a C# file
        var csContent = @"public class Test { public void Method() { } }";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "Test.cs"), csContent);

        try
        {
            // Act
            await RefactorCommand.ExecuteRefactor(
                tempDir, true, false, false, Array.Empty<string>(),
                Array.Empty<string>(), "Info", outputPath, "json", false);

            // Assert
            Assert.True(File.Exists(outputPath));
            var reportContent = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("GeneratedAt", reportContent);
            Assert.Contains("Analysis", reportContent);
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
    public async Task ExecuteRefactor_WithSpecificRules_FiltersSuggestions()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayRefactorRulesTest");
        Directory.CreateDirectory(tempDir);

        // Create a C# file
        var csContent = @"public class Test { public void Method() { } }";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "Test.cs"), csContent);

        try
        {
            // Act
            await RefactorCommand.ExecuteRefactor(
                tempDir, true, false, false, new[] { "TestRule" },
                Array.Empty<string>(), "Info", null, "markdown", false);

            // Assert - Should complete without errors
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
    public async Task ExecuteRefactor_WithCategories_FiltersByCategory()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayRefactorCategoryTest");
        Directory.CreateDirectory(tempDir);

        // Create a C# file
        var csContent = @"public class Test { public void Method() { } }";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "Test.cs"), csContent);

        try
        {
            // Act
            await RefactorCommand.ExecuteRefactor(
                tempDir, true, false, false, Array.Empty<string>(),
                new[] { "Performance" }, "Info", null, "markdown", false);

            // Assert - Should complete without errors
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
    public async Task ExecuteRefactor_WithInvalidSeverity_UsesDefault()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayRefactorSeverityTest");
        Directory.CreateDirectory(tempDir);

        // Create a C# file
        var csContent = @"public class Test { public void Method() { } }";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "Test.cs"), csContent);

        try
        {
            // Act
            await RefactorCommand.ExecuteRefactor(
                tempDir, true, false, false, Array.Empty<string>(),
                Array.Empty<string>(), "InvalidSeverity", null, "markdown", false);

            // Assert - Should complete without errors (uses default severity)
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}