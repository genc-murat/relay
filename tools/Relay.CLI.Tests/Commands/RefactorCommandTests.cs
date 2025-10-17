using Relay.CLI.Commands;
using System.CommandLine;
using Spectre.Console.Testing;
using Spectre.Console;
using System.Reflection;

namespace Relay.CLI.Tests.Commands;

public class RefactorCommandTests
{
    [Fact]
    public void RefactorCommand_Create_ShouldReturnCommand()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();

        // Assert
        command.Should().NotBeNull();
        command.Should().BeOfType<Command>();
    }

    [Fact]
    public void RefactorCommand_ShouldHaveCorrectName()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();

        // Assert
        command.Name.Should().Be("refactor");
    }

    [Fact]
    public void RefactorCommand_ShouldHaveDescription()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();

        // Assert
        command.Description.Should().Be("Automated code refactoring and modernization");
    }

    [Fact]
    public void RefactorCommand_ShouldHavePathOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var pathOption = command.Options.FirstOrDefault(o => o.Name == "path");

        // Assert
        pathOption.Should().NotBeNull();
        pathOption!.Name.Should().Be("path");
        pathOption.Description.Should().Be("Project path to refactor");
        pathOption.Should().BeOfType<Option<string>>();
    }

    [Fact]
    public void RefactorCommand_ShouldHaveAnalyzeOnlyOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "analyze-only");

        // Assert
        option.Should().NotBeNull();
        option!.Name.Should().Be("analyze-only");
        option.Description.Should().Be("Only analyze without applying changes");
        option.Should().BeOfType<Option<bool>>();
    }

    [Fact]
    public void RefactorCommand_ShouldHaveDryRunOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "dry-run");

        // Assert
        option.Should().NotBeNull();
        option!.Name.Should().Be("dry-run");
        option.Description.Should().Be("Show changes without applying them");
        option.Should().BeOfType<Option<bool>>();
    }

    [Fact]
    public void RefactorCommand_ShouldHaveInteractiveOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "interactive");

        // Assert
        option.Should().NotBeNull();
        option!.Name.Should().Be("interactive");
        option.Description.Should().Be("Prompt for each refactoring");
        option.Should().BeOfType<Option<bool>>();
    }

    [Fact]
    public void RefactorCommand_ShouldHaveRulesOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "rules");

        // Assert
        option.Should().NotBeNull();
        option!.Name.Should().Be("rules");
        option.Description.Should().Be("Specific rules to apply");
        option.Should().BeOfType<Option<string[]>>();
    }

    [Fact]
    public void RefactorCommand_ShouldHaveCategoryOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "category");

        // Assert
        option.Should().NotBeNull();
        option!.Name.Should().Be("category");
        option.Description.Should().Be("Refactoring categories (Performance, Readability, etc.)");
        option.Should().BeOfType<Option<string[]>>();
    }

    [Fact]
    public void RefactorCommand_ShouldHaveMinSeverityOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "min-severity");

        // Assert
        option.Should().NotBeNull();
        option!.Name.Should().Be("min-severity");
        option.Description.Should().Be("Minimum severity (Info, Suggestion, Warning, Error)");
        option.Should().BeOfType<Option<string>>();
    }

    [Fact]
    public void RefactorCommand_ShouldHaveOutputOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "output");

        // Assert
        option.Should().NotBeNull();
        option!.Name.Should().Be("output");
        option.Description.Should().Be("Refactoring report output path");
        option.Should().BeOfType<Option<string?>>();
    }

    [Fact]
    public void RefactorCommand_ShouldHaveFormatOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "format");

        // Assert
        option.Should().NotBeNull();
        option!.Name.Should().Be("format");
        option.Description.Should().Be("Report format (markdown, json, html)");
        option.Should().BeOfType<Option<string>>();
    }

    [Fact]
    public void RefactorCommand_ShouldHaveBackupOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "backup");

        // Assert
        option.Should().NotBeNull();
        option!.Name.Should().Be("backup");
        option.Description.Should().Be("Create backup before refactoring");
        option.Should().BeOfType<Option<bool>>();
    }

    [Fact]
    public void RefactorCommand_ShouldHaveTenOptions()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();

        // Assert
        command.Options.Should().HaveCount(10);
    }

    [Fact]
    public void RefactorCommand_ShouldHaveHandler()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();

        // Assert
        command.Handler.Should().NotBeNull();
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

            // Assert
            testConsole.Output.Should().Contain("🔧 Code Refactoring Engine");
            testConsole.Output.Should().Contain("✅ Analysis complete");
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

            // Assert
            testConsole.Output.Should().Contain("🔧 Code Refactoring Engine");
            testConsole.Output.Should().Contain("🔍 Scanning for refactoring opportunities...");
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
            testConsole.Output.Should().Contain("🔧 Code Refactoring Engine");
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
            File.Exists(outputFile).Should().BeTrue();
            var content = await File.ReadAllTextAsync(outputFile);
            content.Should().Contain("# Code Refactoring Report");
            content.Should().Contain("Test suggestion");
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
        result.Should().Contain("# Code Refactoring Report");
        result.Should().Contain("Use async/await");
        result.Should().Contain("AsyncRule");
        result.Should().Contain("*Generated by Relay CLI Refactoring Tool*");
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
        result.Should().Contain("\"Analysis\"");
        result.Should().Contain("\"ApplyResult\"");
        result.Should().Contain("\"Status\": 0");
    }
}