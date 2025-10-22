using Relay.CLI.Commands;
using System.CommandLine;
using Spectre.Console.Testing;
using Spectre.Console;
using System.Reflection;
using Xunit;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type
#pragma warning disable CS8602 // Dereference of a possibly null reference

namespace Relay.CLI.Tests.Commands;

public class RefactorCommandTests
{
    [Fact]
    public void RefactorCommand_Create_ShouldReturnCommand()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();

        // Assert
        Assert.NotNull(command);
        Assert.IsType<Command>(command);
    }

    [Fact]
    public void RefactorCommand_ShouldHaveCorrectName()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();

        // Assert
        Assert.Equal("refactor", command.Name);
    }

    [Fact]
    public void RefactorCommand_ShouldHaveDescription()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();

        // Assert
        Assert.Equal("Automated code refactoring and modernization", command.Description);
    }

    [Fact]
    public void RefactorCommand_ShouldHavePathOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var pathOption = command.Options.FirstOrDefault(o => o.Name == "path");

        // Assert
        Assert.NotNull(pathOption);
        Assert.Equal("path", pathOption.Name);
        Assert.Equal("Project path to refactor", pathOption.Description);
        Assert.IsType<Option<string>>(pathOption);
    }

    [Fact]
    public void RefactorCommand_ShouldHaveOutputOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "output");

        // Assert
        Assert.NotNull(option);
        Assert.Equal("output", option.Name);
        Assert.Equal("Refactoring report output path", option.Description);
        Assert.IsType<Option<string?>>(option);
    }

    [Fact]
    public void RefactorCommand_ShouldHaveFormatOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "format");

        // Assert
        Assert.NotNull(option);
        Assert.Equal("format", option.Name);
        Assert.Equal("Report format (markdown, json, html)", option.Description);
        Assert.IsType<Option<string>>(option);
    }

    [Fact]
    public void RefactorCommand_ShouldHaveBackupOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "backup");

        // Assert
        Assert.NotNull(option);
        Assert.Equal("backup", option.Name);
        Assert.Equal("Create backup before refactoring", option.Description);
        Assert.IsType<Option<bool>>(option);
    }

    [Fact]
    public void RefactorCommand_ShouldHaveTenOptions()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();

        // Assert
        Assert.Equal(10, command.Options.Count());
    }

    [Fact]
    public void RefactorCommand_ShouldHaveHandler()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();

        // Assert
        Assert.NotNull(command.Handler);
    }

    [Fact]
    public async Task RefactorCommand_ExecuteRefactor_AnalyzeOnly_ShouldExecuteWithoutException()
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
            // Act
            var originalConsole = AnsiConsole.Console;
            AnsiConsole.Console = testConsole;

            await RefactorCommand.ExecuteRefactor(
                tempDir,
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

            // Assert - Method executed without exception
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
    public async Task RefactorCommand_ExecuteRefactor_DryRun_ShouldExecuteWithoutException()
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
            // Act
            var originalConsole = AnsiConsole.Console;
            AnsiConsole.Console = testConsole;

            await RefactorCommand.ExecuteRefactor(
                tempDir,
                analyzeOnly: false,
                dryRun: true,
                interactive: false,
                rules: Array.Empty<string>(),
                categories: Array.Empty<string>(),
                severityStr: "Info",
                outputFile: null,
                format: "markdown",
                createBackup: false);

            AnsiConsole.Console = originalConsole;

            // Assert - Method executed without exception
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
    public async Task RefactorCommand_ExecuteRefactor_InvalidSeverity_ShouldUseDefault()
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
            // Act
            var originalConsole = AnsiConsole.Console;
            AnsiConsole.Console = testConsole;

            await RefactorCommand.ExecuteRefactor(
                tempDir,
                analyzeOnly: true,
                dryRun: false,
                interactive: false,
                rules: Array.Empty<string>(),
                categories: Array.Empty<string>(),
                severityStr: "InvalidSeverity",
                outputFile: null,
                format: "markdown",
                createBackup: false);

            AnsiConsole.Console = originalConsole;

            // Assert - should not throw and use default severity
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
    public async Task RefactorCommand_SaveRefactoringReport_Markdown_ShouldCreateFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var outputFile = Path.Combine(tempDir, "report.md");

        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow.AddSeconds(-1),
            EndTime = DateTime.UtcNow,
            Duration = TimeSpan.FromSeconds(1),
            FilesAnalyzed = 5,
            SuggestionsCount = 3,
            FileResults = new List<Relay.CLI.Refactoring.FileRefactoringResult>
            {
                new Relay.CLI.Refactoring.FileRefactoringResult
                {
                    FilePath = "Test.cs",
                    Suggestions = new List<Relay.CLI.Refactoring.RefactoringSuggestion>
                    {
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "TestRule",
                            Description = "Test suggestion",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.Readability,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Suggestion,
                            FilePath = "Test.cs",
                            LineNumber = 1,
                            Rationale = "Test rationale"
                        }
                    }
                }
            }
        };

        var applyResult = new Relay.CLI.Refactoring.ApplyResult
        {
            StartTime = DateTime.UtcNow.AddSeconds(-1),
            EndTime = DateTime.UtcNow,
            Duration = TimeSpan.FromSeconds(1),
            FilesModified = 1,
            RefactoringsApplied = 1,
            Status = Relay.CLI.Refactoring.RefactoringStatus.Success
        };

        try
        {
            // Act
            var task = (Task)typeof(Relay.CLI.Commands.RefactorCommand)
                .GetMethod("SaveRefactoringReport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .Invoke(null, new object[] { analysis, applyResult, outputFile, "markdown" });
            await task;

            // Assert
            Assert.True(File.Exists(outputFile));
            var content = await File.ReadAllTextAsync(outputFile);
            Assert.Contains("# Code Refactoring Report", content);
            Assert.Contains("Test suggestion", content);
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
    public void RefactorCommand_GenerateMarkdownReport_ShouldReturnValidMarkdown()
    {
        // Arrange
        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow.AddSeconds(-1),
            EndTime = DateTime.UtcNow,
            Duration = TimeSpan.FromSeconds(1),
            FilesAnalyzed = 2,
            SuggestionsCount = 1,
            FileResults = new List<Relay.CLI.Refactoring.FileRefactoringResult>
            {
                new Relay.CLI.Refactoring.FileRefactoringResult
                {
                    FilePath = "Program.cs",
                    Suggestions = new List<Relay.CLI.Refactoring.RefactoringSuggestion>
                    {
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "AsyncRule",
                            Description = "Use async/await",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.AsyncAwait,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Suggestion,
                            FilePath = "Program.cs",
                            LineNumber = 10,
                            Rationale = "Improves async programming"
                        }
                    }
                }
            }
        };

        var applyResult = new Relay.CLI.Refactoring.ApplyResult
        {
            FilesModified = 1,
            RefactoringsApplied = 1,
            Status = Relay.CLI.Refactoring.RefactoringStatus.Success
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateMarkdownReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, applyResult });

        // Assert
        Assert.Contains("# Code Refactoring Report", result);
        Assert.Contains("Use async/await", result);
        Assert.Contains("AsyncRule", result);
        Assert.Contains("*Generated by Relay CLI Refactoring Tool*", result);
    }

    [Fact]
    public void RefactorCommand_GenerateJsonReport_ShouldReturnValidJson()
    {
        // Arrange
        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(1),
            Duration = TimeSpan.FromSeconds(1),
            FilesAnalyzed = 1,
            SuggestionsCount = 1,
            FileResults = new List<Relay.CLI.Refactoring.FileRefactoringResult>()
        };

        var applyResult = new Relay.CLI.Refactoring.ApplyResult
        {
            Status = Relay.CLI.Refactoring.RefactoringStatus.Success
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateJsonReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, applyResult });

        // Assert
        Assert.Contains("\"Analysis\"", result);
        Assert.Contains("\"ApplyResult\"", result);
        Assert.Contains("\"Status\": 0", result);
    }
}

