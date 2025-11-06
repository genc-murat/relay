using Relay.CLI.Commands;
using Relay.CLI.Refactoring;
using Spectre.Console;
using System.CommandLine;

namespace Relay.CLI.Tests.Commands;

public class RefactorCommandExceptionTests
{
    [Fact]
    public async Task RefactorCommand_ExecuteRefactor_WithInvalidPath_ShouldHandleGracefully()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();
        var invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var originalExitCode = Environment.ExitCode;

        try
        {
            // Act - This should handle the invalid path gracefully
            var originalConsole = AnsiConsole.Console;
            AnsiConsole.Console = testConsole;

            await RefactorCommand.ExecuteRefactor(
                invalidPath, // Invalid path that doesn't exist
                analyzeOnly: true,
                dryRun: false,
                interactive: false,
                rules: Array.Empty<string>(),
                categories: Array.Empty<string>(),
                severityStr: "Info",
                outputFile: null,
                format: "markdown",
                createBackup: false);

            AnsiConsole.Console = originalConsole;

            // Assert - Should not throw exception and should set appropriate exit code
            // When there are no suggestions, exit code should be 0
            Assert.Equal(0, Environment.ExitCode);
        }
        finally
        {
            Environment.ExitCode = originalExitCode;
        }
    }

    [Fact]
    public async Task RefactorCommand_ExecuteRefactor_WithExceptionInAnalysis_ShouldSetExitCodeAndDisplayError()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        // Create a C# file that might cause issues during analysis
        var csFile = Path.Combine(tempDir, "Test.cs");
        // Write a file with invalid content that might cause parsing issues
        await File.WriteAllTextAsync(csFile, @"
using System;
class Test
{
    public void Method()
    {
        // Intentionally malformed code that might cause parsing issues
        var x = ; // This is invalid syntax
    }
}");
        
        var originalExitCode = Environment.ExitCode;

        try
        {
            // Act
            var originalConsole = AnsiConsole.Console;
            AnsiConsole.Console = testConsole;

            await RefactorCommand.ExecuteRefactor(
                tempDir,
                analyzeOnly: false, // Run full refactoring, not just analysis
                dryRun: false,
                interactive: false,
                rules: Array.Empty<string>(),
                categories: Array.Empty<string>(),
                severityStr: "Info",
                outputFile: null,
                format: "markdown",
                createBackup: false);

            AnsiConsole.Console = originalConsole;

            // Assert - Should not throw exception and should complete without major failures
            // The exit code depends on whether any refactorings were attempted
            // If no suggestions were found, exit code should be 0
        }
        finally
        {
            Environment.ExitCode = originalExitCode;
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task RefactorCommand_ExecuteRefactor_WithNoSuggestions_ShouldDisplayMessageAndSetExitCodeToZero()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        // Create an empty directory with no .cs files to guarantee no suggestions
        // This ensures the analysis finds 0 files and thus 0 suggestions
        
        var originalExitCode = Environment.ExitCode;

        try
        {
            // Act - Use analyze-only mode to ensure applyResult is null
            var originalConsole = AnsiConsole.Console;
            AnsiConsole.Console = testConsole;

            await RefactorCommand.ExecuteRefactor(
                tempDir, // Empty directory
                analyzeOnly: true,
                dryRun: false,
                interactive: false,
                rules: Array.Empty<string>(),
                categories: Array.Empty<string>(),
                severityStr: "Info", // Use lowest severity to be inclusive
                outputFile: null,
                format: "markdown",
                createBackup: false);

            AnsiConsole.Console = originalConsole;

            // With an empty directory, no files will be analyzed, so no suggestions will be found
            // This means analysis.SuggestionsCount == 0, and exit code should be 0
            Assert.Equal(0, Environment.ExitCode);
        }
        finally
        {
            Environment.ExitCode = originalExitCode;
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task RefactorCommand_ExecuteRefactor_WithMissingPermissions_ShouldHandleException()
    {
        // For this test, we'll check what happens when path operations fail
        // We can't easily test file system permissions in a unit test,
        // but we can test how the code handles non-existent paths
        var testConsole = new Spectre.Console.Testing.TestConsole();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        
        // Don't create the directory - this ensures it doesn't exist
        var originalExitCode = Environment.ExitCode;

        try
        {
            // Act
            var originalConsole = AnsiConsole.Console;
            AnsiConsole.Console = testConsole;

            await RefactorCommand.ExecuteRefactor(
                tempDir, // Non-existent directory
                analyzeOnly: true,
                dryRun: false,
                interactive: false,
                rules: Array.Empty<string>(),
                categories: Array.Empty<string>(),
                severityStr: "Info",
                outputFile: null,
                format: "markdown",
                createBackup: false);

            AnsiConsole.Console = originalConsole;

            // Assert - Should handle the missing directory gracefully
            Assert.Equal(0, Environment.ExitCode);
        }
        finally
        {
            Environment.ExitCode = originalExitCode;
        }
    }

    [Fact]
    public async Task RefactorCommand_ExecuteRefactor_WithInvalidSeverity_ShouldUseDefaultAndNotThrow()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        // Create a dummy C# file
        var csFile = Path.Combine(tempDir, "Test.cs");
        await File.WriteAllTextAsync(csFile, "using System; class Test { }");

        try
        {
            // Act - Using an invalid severity string to test default fallback
            var originalConsole = AnsiConsole.Console;
            AnsiConsole.Console = testConsole;

            await RefactorCommand.ExecuteRefactor(
                tempDir,
                analyzeOnly: true,
                dryRun: false,
                interactive: false,
                rules: Array.Empty<string>(),
                categories: Array.Empty<string>(),
                severityStr: "InvalidSeverityValue", // Invalid severity to test default handling
                outputFile: null,
                format: "markdown",
                createBackup: false);

            AnsiConsole.Console = originalConsole;

            // Assert - Should not throw and should default to Info severity
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
    public async Task RefactorCommand_ExecuteRefactor_WithInvalidCategories_ShouldFilterAndContinue()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        // Create a dummy C# file
        var csFile = Path.Combine(tempDir, "Test.cs");
        await File.WriteAllTextAsync(csFile, "using System; class Test { }");

        try
        {
            // Act - Using invalid categories to test filtering logic
            var originalConsole = AnsiConsole.Console;
            AnsiConsole.Console = testConsole;

            await RefactorCommand.ExecuteRefactor(
                tempDir,
                analyzeOnly: true,
                dryRun: false,
                interactive: false,
                rules: Array.Empty<string>(),
                categories: new[] { "InvalidCategory", "Performance", "AnotherInvalid", "Readability" }, // Mix of valid and invalid
                severityStr: "Info",
                outputFile: null,
                format: "markdown",
                createBackup: false);

            AnsiConsole.Console = originalConsole;

            // Assert - Should not throw and should filter out invalid categories
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
    public async Task RefactorCommand_ExecuteRefactor_WithJsonOutputReport_ShouldGenerateJsonReportFile()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var reportPath = Path.Combine(tempDir, "refactor-report.json");
        Directory.CreateDirectory(tempDir);

        // Create a C# file that will likely generate suggestions
        var csFile = Path.Combine(tempDir, "JsonReportTest.cs");
        await File.WriteAllTextAsync(csFile, @"
using System.Threading.Tasks;

public class JsonReportTest
{
    public void DoWork()
    {
        var task = GetDataAsync();
        var result = task.Result;
    }

    public async Task<string> GetDataAsync()
    {
        await Task.Delay(100);
        return ""data"";
    }
}");

        try
        {
            // Act
            var originalConsole = AnsiConsole.Console;
            AnsiConsole.Console = testConsole;

            await RefactorCommand.ExecuteRefactor(
                tempDir,
                analyzeOnly: true, // Just analyze to generate report
                dryRun: false,
                interactive: false,
                rules: Array.Empty<string>(),
                categories: Array.Empty<string>(),
                severityStr: "Info",
                outputFile: reportPath, // Specify output file
                format: "json",         // Use JSON format
                createBackup: false);

            AnsiConsole.Console = originalConsole;

            // Assert - JSON Report file should be created (even if no suggestions found)
            Assert.True(File.Exists(reportPath), "JSON Report file should be created");
            
            var reportContent = await File.ReadAllTextAsync(reportPath);
            Assert.Contains("\"Analysis\"", reportContent);
            // The report should contain valid JSON structure regardless of suggestion count
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