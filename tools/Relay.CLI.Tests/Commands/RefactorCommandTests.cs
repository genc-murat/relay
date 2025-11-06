using Relay.CLI.Commands;
using Spectre.Console;
using System.CommandLine;
using System.Reflection;

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
        Assert.Equal(10, command.Options.Count);
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
        Assert.Contains("Generated by", result);
        Assert.Contains("Relay CLI Refactoring Tool", result);
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

    [Fact]
    public void RefactorCommand_GenerateHtmlReport_ShouldReturnValidHtml()
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
            StartTime = DateTime.UtcNow.AddSeconds(-1),
            EndTime = DateTime.UtcNow,
            Duration = TimeSpan.FromSeconds(1),
            FilesModified = 1,
            RefactoringsApplied = 1,
            Status = Relay.CLI.Refactoring.RefactoringStatus.Success
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateHtmlReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, applyResult });

        // Assert
        Assert.Contains("<!DOCTYPE html>", result);
        Assert.Contains("<html lang=\"en\">", result);
        Assert.Contains("</html>", result);
        Assert.Contains("<title>Refactoring Report</title>", result);
        Assert.Contains("🔧 Code Refactoring Report", result);
        Assert.Contains("Use async/await", result);
        Assert.Contains("AsyncRule", result);
        Assert.Contains("Generated by <strong>Relay CLI Refactoring Tool</strong></p>", result);
    }

    [Fact]
    public void RefactorCommand_GenerateHtmlReport_ShouldDisplaySuccessStatus()
    {
        // Arrange
        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(1),
            Duration = TimeSpan.FromSeconds(1),
            FilesAnalyzed = 1,
            SuggestionsCount = 0,
            FileResults = new List<Relay.CLI.Refactoring.FileRefactoringResult>()
        };

        var applyResult = new Relay.CLI.Refactoring.ApplyResult
        {
            Status = Relay.CLI.Refactoring.RefactoringStatus.Success,
            Duration = TimeSpan.FromSeconds(0.5),
            FilesModified = 1,
            RefactoringsApplied = 1
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateHtmlReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, applyResult });

        // Assert
        Assert.Contains("✅", result);
        Assert.Contains("Success", result);
        Assert.Contains("#4CAF50", result); // Green color for success
    }

    [Fact]
    public void RefactorCommand_GenerateHtmlReport_ShouldDisplayPartialStatus()
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
            Status = Relay.CLI.Refactoring.RefactoringStatus.Partial,
            Duration = TimeSpan.FromSeconds(0.5),
            FilesModified = 0,
            RefactoringsApplied = 0
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateHtmlReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, applyResult });

        // Assert
        Assert.Contains("⚠️", result);
        Assert.Contains("Partial", result);
        Assert.Contains("#FFC107", result); // Yellow color for partial
    }

    [Fact]
    public void RefactorCommand_GenerateHtmlReport_ShouldDisplayFailedStatus()
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
            Status = Relay.CLI.Refactoring.RefactoringStatus.Failed,
            Duration = TimeSpan.FromSeconds(0.1),
            FilesModified = 0,
            RefactoringsApplied = 0,
            Error = "Test error"
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateHtmlReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, applyResult });

        // Assert
        Assert.Contains("❌", result);
        Assert.Contains("Failed", result);
        Assert.Contains("#F44336", result); // Red color for failed
        Assert.Contains("Test error", result);
    }

    [Fact]
    public void RefactorCommand_GenerateHtmlReport_ShouldDisplayAnalysisOnlyStatus()
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

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateHtmlReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, null });

        // Assert
        Assert.Contains("Analysis Only", result);
        Assert.DoesNotContain("Apply Duration:", result);
    }

    [Fact]
    public void RefactorCommand_GenerateHtmlReport_ShouldDisplaySummaryMetrics()
    {
        // Arrange
        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(2.5),
            Duration = TimeSpan.FromSeconds(2.5),
            FilesAnalyzed = 10,
            SuggestionsCount = 5,
            FileResults = new List<Relay.CLI.Refactoring.FileRefactoringResult>()
        };

        var applyResult = new Relay.CLI.Refactoring.ApplyResult
        {
            Status = Relay.CLI.Refactoring.RefactoringStatus.Success,
            Duration = TimeSpan.FromSeconds(1.2),
            FilesModified = 3,
            RefactoringsApplied = 4
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateHtmlReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, applyResult });

        // Assert
        Assert.Contains("📊 Summary", result);
        Assert.Contains("<td>Files Analyzed</td><td>10</td>", result);
        Assert.Contains("<td>Suggestions Found</td><td>5</td>", result);
        Assert.Contains("<td>Files with Issues</td><td>0</td>", result);
        Assert.Contains("<td>Files Modified</td><td>3</td>", result);
        Assert.Contains("<td>Refactorings Applied</td><td>4</td>", result);
        Assert.Matches(@"Analysis Duration:\s*2[.,]50s", result);
        Assert.Matches(@"Apply Duration:\s*1[.,]20s", result);
    }

    [Fact]
    public void RefactorCommand_GenerateHtmlReport_ShouldIncludeSuggestionsSection_WhenSuggestionsExist()
    {
        // Arrange
        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(1),
            Duration = TimeSpan.FromSeconds(1),
            FilesAnalyzed = 1,
            SuggestionsCount = 2,
            FileResults = new List<Relay.CLI.Refactoring.FileRefactoringResult>
            {
                new Relay.CLI.Refactoring.FileRefactoringResult
                {
                    FilePath = "Test.cs",
                    Suggestions = new List<Relay.CLI.Refactoring.RefactoringSuggestion>
                    {
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "TestRule1",
                            Description = "First suggestion",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.Readability,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Suggestion,
                            FilePath = "Test.cs",
                            LineNumber = 5,
                            Rationale = "Improves readability"
                        },
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "TestRule2",
                            Description = "Second suggestion",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.Performance,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Warning,
                            FilePath = "Test.cs",
                            LineNumber = 10,
                            Rationale = "Improves performance",
                            OriginalCode = "var x = 1;",
                            SuggestedCode = "const x = 1;"
                        }
                    }
                }
            }
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateHtmlReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, null });

        // Assert
        Assert.Contains("💡 Refactoring Suggestions", result);
        Assert.Contains("Test.cs", result);
        Assert.Contains("First suggestion", result);
        Assert.Contains("Second suggestion", result);
        Assert.Contains("TestRule1", result);
        Assert.Contains("TestRule2", result);
        Assert.Contains("Improves readability", result);
        Assert.Contains("Improves performance", result);
        Assert.Contains("var x = 1;", result);
        Assert.Contains("const x = 1;", result);
    }

    [Fact]
    public void RefactorCommand_GenerateHtmlReport_ShouldNotIncludeSuggestionsSection_WhenNoSuggestions()
    {
        // Arrange
        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(1),
            Duration = TimeSpan.FromSeconds(1),
            FilesAnalyzed = 1,
            SuggestionsCount = 0,
            FileResults = new List<Relay.CLI.Refactoring.FileRefactoringResult>()
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateHtmlReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, null });

        // Assert
        Assert.DoesNotContain("💡 Refactoring Suggestions", result);
    }

    [Fact]
    public void RefactorCommand_GenerateHtmlReport_ShouldStyleSuggestionsBySeverity()
    {
        // Arrange
        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(1),
            Duration = TimeSpan.FromSeconds(1),
            FilesAnalyzed = 1,
            SuggestionsCount = 4,
            FileResults = new List<Relay.CLI.Refactoring.FileRefactoringResult>
            {
                new Relay.CLI.Refactoring.FileRefactoringResult
                {
                    FilePath = "Test.cs",
                    Suggestions = new List<Relay.CLI.Refactoring.RefactoringSuggestion>
                    {
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "ErrorRule",
                            Description = "Error suggestion",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.Security,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Error,
                            FilePath = "Test.cs",
                            LineNumber = 1,
                            Rationale = "Security issue"
                        },
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "WarningRule",
                            Description = "Warning suggestion",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.Performance,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Warning,
                            FilePath = "Test.cs",
                            LineNumber = 2,
                            Rationale = "Performance issue"
                        },
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "SuggestionRule",
                            Description = "Suggestion",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.Readability,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Suggestion,
                            FilePath = "Test.cs",
                            LineNumber = 3,
                            Rationale = "Readability improvement"
                        },
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "InfoRule",
                            Description = "Info suggestion",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.BestPractices,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Info,
                            FilePath = "Test.cs",
                            LineNumber = 4,
                            Rationale = "Best practice"
                        }
                    }
                }
            }
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateHtmlReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, null });

        // Assert
        Assert.Contains("suggestion-error", result);
        Assert.Contains("suggestion-warning", result);
        Assert.Contains("suggestion-suggestion", result);
        Assert.Contains("suggestion-info", result);
        Assert.Contains("icon-error", result);
        Assert.Contains("icon-warning", result);
        Assert.Contains("icon-suggestion", result);
        Assert.Contains("icon-info", result);
        Assert.Contains("❌", result);
        Assert.Contains("⚠️", result);
        Assert.Contains("💡", result);
        Assert.Contains("ℹ️", result);
    }

    [Fact]
    public void RefactorCommand_GenerateHtmlReport_ShouldIncludeCategoryBadges()
    {
        // Arrange
        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(1),
            Duration = TimeSpan.FromSeconds(1),
            FilesAnalyzed = 1,
            SuggestionsCount = 7,
            FileResults = new List<Relay.CLI.Refactoring.FileRefactoringResult>
            {
                new Relay.CLI.Refactoring.FileRefactoringResult
                {
                    FilePath = "Test.cs",
                    Suggestions = new List<Relay.CLI.Refactoring.RefactoringSuggestion>
                    {
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "PerformanceRule",
                            Description = "Performance suggestion",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.Performance,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Suggestion,
                            FilePath = "Test.cs",
                            LineNumber = 1,
                            Rationale = "Performance"
                        },
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "ReadabilityRule",
                            Description = "Readability suggestion",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.Readability,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Suggestion,
                            FilePath = "Test.cs",
                            LineNumber = 2,
                            Rationale = "Readability"
                        },
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "ModernizationRule",
                            Description = "Modernization suggestion",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.Modernization,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Suggestion,
                            FilePath = "Test.cs",
                            LineNumber = 3,
                            Rationale = "Modernization"
                        },
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "BestPracticesRule",
                            Description = "Best practices suggestion",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.BestPractices,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Suggestion,
                            FilePath = "Test.cs",
                            LineNumber = 4,
                            Rationale = "Best practices"
                        },
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "MaintainabilityRule",
                            Description = "Maintainability suggestion",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.Maintainability,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Suggestion,
                            FilePath = "Test.cs",
                            LineNumber = 5,
                            Rationale = "Maintainability"
                        },
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "SecurityRule",
                            Description = "Security suggestion",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.Security,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Suggestion,
                            FilePath = "Test.cs",
                            LineNumber = 6,
                            Rationale = "Security"
                        },
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "AsyncRule",
                            Description = "Async suggestion",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.AsyncAwait,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Suggestion,
                            FilePath = "Test.cs",
                            LineNumber = 7,
                            Rationale = "Async"
                        }
                    }
                }
            }
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateHtmlReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, null });

        // Assert
        Assert.Contains("badge-performance", result);
        Assert.Contains("badge-readability", result);
        Assert.Contains("badge-modernization", result);
        Assert.Contains("badge-bestpractices", result);
        Assert.Contains("badge-maintainability", result);
        Assert.Contains("badge-security", result);
        Assert.Contains("badge-asyncawait", result);
    }

    [Fact]
    public void RefactorCommand_GenerateHtmlReport_ShouldIncludeErrorSection_WhenErrorExists()
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
            Status = Relay.CLI.Refactoring.RefactoringStatus.Failed,
            Duration = TimeSpan.FromSeconds(0.1),
            FilesModified = 0,
            RefactoringsApplied = 0,
            Error = "Application failed with error code 123"
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateHtmlReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, applyResult });

        // Assert
        Assert.Contains("❌ Error Information", result);
        Assert.Contains("Application failed with error code 123", result);
    }

    [Fact]
    public void RefactorCommand_GenerateHtmlReport_ShouldNotIncludeErrorSection_WhenNoError()
    {
        // Arrange
        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(1),
            Duration = TimeSpan.FromSeconds(1),
            FilesAnalyzed = 1,
            SuggestionsCount = 0,
            FileResults = new List<Relay.CLI.Refactoring.FileRefactoringResult>()
        };

        var applyResult = new Relay.CLI.Refactoring.ApplyResult
        {
            Status = Relay.CLI.Refactoring.RefactoringStatus.Success,
            Duration = TimeSpan.FromSeconds(1),
            FilesModified = 0,
            RefactoringsApplied = 0
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateHtmlReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, applyResult });

        // Assert
        Assert.DoesNotContain("❌ Error Information", result);
    }

    [Fact]
    public void RefactorCommand_GenerateHtmlReport_ShouldEscapeHtmlInContent()
    {
        // Arrange
        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(1),
            Duration = TimeSpan.FromSeconds(1),
            FilesAnalyzed = 1,
            SuggestionsCount = 1,
            FileResults = new List<Relay.CLI.Refactoring.FileRefactoringResult>
            {
                new Relay.CLI.Refactoring.FileRefactoringResult
                {
                    FilePath = "Test.cs",
                    Suggestions = new List<Relay.CLI.Refactoring.RefactoringSuggestion>
                    {
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "Test<Rule>",
                            Description = "Use <strong> instead of <b>",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.Readability,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Suggestion,
                            FilePath = "Test.cs",
                            LineNumber = 1,
                            Rationale = "HTML & special chars",
                            OriginalCode = "var x = <b>test</b>;",
                            SuggestedCode = "var x = <strong>test</strong>;"
                        }
                    }
                }
            }
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateHtmlReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, null });

        // Assert
        Assert.Contains("&lt;strong&gt;", result);
        Assert.Contains("&lt;b&gt;", result);
        Assert.Contains("&amp;", result);
        Assert.Contains("Test&lt;Rule&gt;", result);
    }

    [Fact]
    public void RefactorCommand_GenerateHtmlReport_ShouldIncludeResponsiveMetaTag()
    {
        // Arrange
        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(1),
            Duration = TimeSpan.FromSeconds(1),
            FilesAnalyzed = 1,
            SuggestionsCount = 0,
            FileResults = new List<Relay.CLI.Refactoring.FileRefactoringResult>()
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateHtmlReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, null });

        // Assert
        Assert.Contains("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">", result);
        Assert.Contains("<meta charset=\"UTF-8\">", result);
    }

    [Fact]
    public void RefactorCommand_GenerateHtmlReport_ShouldIncludeCSS()
    {
        // Arrange
        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(1),
            Duration = TimeSpan.FromSeconds(1),
            FilesAnalyzed = 1,
            SuggestionsCount = 0,
            FileResults = new List<Relay.CLI.Refactoring.FileRefactoringResult>()
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateHtmlReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, null });

        // Assert
        Assert.Contains("<style>", result);
        Assert.Contains("</style>", result);
        Assert.Contains("font-family:", result);
        Assert.Contains(".container", result);
        Assert.Contains(".header", result);
        Assert.Contains(".section", result);
        Assert.Contains(".suggestion-item", result);
        Assert.Contains(".badge", result);
    }

    [Fact]
    public void RefactorCommand_GenerateHtmlReport_ShouldIncludeFooter()
    {
        // Arrange
        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(1),
            Duration = TimeSpan.FromSeconds(1),
            FilesAnalyzed = 1,
            SuggestionsCount = 0,
            FileResults = new List<Relay.CLI.Refactoring.FileRefactoringResult>()
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateHtmlReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, null });

        // Assert
        Assert.Contains("Generated by", result);
        Assert.Contains("Relay CLI Refactoring Tool", result);
    }

    [Fact]
    public void RefactorCommand_GenerateHtmlReport_ShouldIncludeTimestamp()
    {
        // Arrange
        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(1),
            Duration = TimeSpan.FromSeconds(1),
            FilesAnalyzed = 1,
            SuggestionsCount = 0,
            FileResults = new List<Relay.CLI.Refactoring.FileRefactoringResult>()
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateHtmlReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, null });

        // Assert
        Assert.Contains("Generated:", result);
        Assert.Matches(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}", result); // yyyy-MM-dd HH:mm:ss pattern
    }

    [Fact]
    public void RefactorCommand_GenerateHtmlReport_ShouldBeWellFormedXml()
    {
        // Arrange
        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(1),
            Duration = TimeSpan.FromSeconds(1),
            FilesAnalyzed = 1,
            SuggestionsCount = 0,
            FileResults = new List<Relay.CLI.Refactoring.FileRefactoringResult>()
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateHtmlReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, null });

        // Assert
        Assert.Equal(result.Count(c => c == '<'), result.Count(c => c == '>'));
    }

    [Fact]
    public async Task RefactorCommand_ExecuteRefactor_WithSpecificRules_ShouldExecuteWithoutException()
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
                rules: new[] { "TestRule" },
                categories: new[] { "Performance", "Readability" },
                severityStr: "Warning",
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
    public async Task RefactorCommand_ExecuteRefactor_WithOutputFile_ShouldCreateReport()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var outputFile = Path.Combine(tempDir, "report.md");

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
                outputFile: outputFile,
                format: "markdown",
                createBackup: false);

            AnsiConsole.Console = originalConsole;

            // Assert - Report file should be created
            Assert.True(File.Exists(outputFile));
            var content = await File.ReadAllTextAsync(outputFile);
            Assert.Contains("# Code Refactoring Report", content);
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
    public async Task RefactorCommand_SaveRefactoringReport_Json_ShouldCreateFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var outputFile = Path.Combine(tempDir, "report.json");

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

        try
        {
            // Act
            var task = (Task)typeof(Relay.CLI.Commands.RefactorCommand)
                .GetMethod("SaveRefactoringReport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .Invoke(null, new object[] { analysis, null, outputFile, "json" });
            await task;

            // Assert
            Assert.True(File.Exists(outputFile));
            var content = await File.ReadAllTextAsync(outputFile);
            Assert.Contains("\"Analysis\"", content);
            Assert.Contains("\"TestRule\"", content);
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
    public async Task RefactorCommand_SaveRefactoringReport_Html_ShouldCreateFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var outputFile = Path.Combine(tempDir, "report.html");

        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow.AddSeconds(-1),
            EndTime = DateTime.UtcNow,
            Duration = TimeSpan.FromSeconds(1),
            FilesAnalyzed = 1,
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

        try
        {
            // Act
            var task = (Task)typeof(Relay.CLI.Commands.RefactorCommand)
                .GetMethod("SaveRefactoringReport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .Invoke(null, new object[] { analysis, null, outputFile, "html" });
            await task;

            // Assert
            Assert.True(File.Exists(outputFile));
            var content = await File.ReadAllTextAsync(outputFile);
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html lang=\"en\">", content);
            Assert.Contains("Use async/await", content);
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
    public async Task RefactorCommand_ExecuteRefactor_ExceptionHandling_ShouldSetExitCode()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();
        var invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent");

        // Act
        var originalConsole = AnsiConsole.Console;
        var originalExitCode = Environment.ExitCode;

        try
        {
            AnsiConsole.Console = testConsole;

            await RefactorCommand.ExecuteRefactor(
                invalidPath,
                analyzeOnly: false,
                dryRun: false,
                interactive: false,
                rules: Array.Empty<string>(),
                categories: Array.Empty<string>(),
                severityStr: "Info",
                outputFile: null,
                format: "markdown",
                createBackup: false);

            // Assert - Should set exit code to 0 for handled non-existent project
            Assert.Equal(0, Environment.ExitCode);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
            Environment.ExitCode = originalExitCode;
        }
    }

    [Fact]
    public void RefactorCommand_ShouldHaveAnalyzeOnlyOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "analyze-only");

        // Assert
        Assert.NotNull(option);
        Assert.Equal("analyze-only", option.Name);
        Assert.Equal("Only analyze without applying changes", option.Description);
        Assert.IsType<Option<bool>>(option);
    }

    [Fact]
    public void RefactorCommand_ShouldHaveDryRunOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "dry-run");

        // Assert
        Assert.NotNull(option);
        Assert.Equal("dry-run", option.Name);
        Assert.Equal("Show changes without applying them", option.Description);
        Assert.IsType<Option<bool>>(option);
    }

    [Fact]
    public void RefactorCommand_ShouldHaveInteractiveOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "interactive");

        // Assert
        Assert.NotNull(option);
        Assert.Equal("interactive", option.Name);
        Assert.Equal("Prompt for each refactoring", option.Description);
        Assert.IsType<Option<bool>>(option);
    }

    [Fact]
    public void RefactorCommand_ShouldHaveRulesOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "rules");

        // Assert
        Assert.NotNull(option);
        Assert.Equal("rules", option.Name);
        Assert.Equal("Specific rules to apply", option.Description);
        Assert.IsType<Option<string[]>>(option);
    }

    [Fact]
    public void RefactorCommand_ShouldHaveCategoryOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "category");

        // Assert
        Assert.NotNull(option);
        Assert.Equal("category", option.Name);
        Assert.Equal("Refactoring categories (Performance, Readability, etc.)", option.Description);
        Assert.IsType<Option<string[]>>(option);
    }

    [Fact]
    public void RefactorCommand_ShouldHaveSeverityOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "min-severity");

        // Assert
        Assert.NotNull(option);
        Assert.Equal("min-severity", option.Name);
        Assert.Equal("Minimum severity (Info, Suggestion, Warning, Error)", option.Description);
        Assert.IsType<Option<string>>(option);
    }

    [Fact]
    public async Task RefactorCommand_ExecuteRefactor_WithInvalidCategories_ShouldFilterInvalidOnes()
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
                categories: new[] { "Performance", "InvalidCategory", "Readability", "AnotherInvalid" },
                severityStr: "Info",
                outputFile: null,
                format: "markdown",
                createBackup: false);

            AnsiConsole.Console = originalConsole;

            // Assert - Method executed without exception, invalid categories filtered out
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
    public async Task RefactorCommand_ExecuteRefactor_WithErrorSeverity_ShouldExecute()
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
                severityStr: "Error",
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
    public async Task RefactorCommand_ExecuteRefactor_WithWarningSeverity_ShouldExecute()
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
                severityStr: "Warning",
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
    public async Task RefactorCommand_ExecuteRefactor_WithSuggestionSeverity_ShouldExecute()
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
                severityStr: "Suggestion",
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
    public async Task RefactorCommand_ExecuteRefactor_WithInfoSeverity_ShouldExecute()
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
    public async Task RefactorCommand_ExecuteRefactor_WithJsonFormat_ShouldCreateJsonReport()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var outputFile = Path.Combine(tempDir, "report.json");

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
                outputFile: outputFile,
                format: "json",
                createBackup: false);

            AnsiConsole.Console = originalConsole;

            // Assert - JSON report file should be created
            Assert.True(File.Exists(outputFile));
            var content = await File.ReadAllTextAsync(outputFile);
            Assert.Contains("\"Analysis\"", content);
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
    public async Task RefactorCommand_ExecuteRefactor_WithHtmlFormat_ShouldCreateHtmlReport()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var outputFile = Path.Combine(tempDir, "report.html");

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
                outputFile: outputFile,
                format: "html",
                createBackup: false);

            AnsiConsole.Console = originalConsole;

            // Assert - HTML report file should be created
            Assert.True(File.Exists(outputFile));
            var content = await File.ReadAllTextAsync(outputFile);
            Assert.Contains("<!DOCTYPE html>", content);
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
    public async Task RefactorCommand_ExecuteRefactor_WithUnknownFormat_ShouldDefaultToMarkdown()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var outputFile = Path.Combine(tempDir, "report.unknown");

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
                outputFile: outputFile,
                format: "unknown",
                createBackup: false);

            AnsiConsole.Console = originalConsole;

            // Assert - Should default to markdown format
            Assert.True(File.Exists(outputFile));
            var content = await File.ReadAllTextAsync(outputFile);
            Assert.Contains("# Code Refactoring Report", content);
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
    public void RefactorCommand_DisplayAnalysisResults_WithNoFiles_ShouldDisplayEmptyResults()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();
        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(1),
            Duration = TimeSpan.FromSeconds(1),
            FilesAnalyzed = 0,
            SuggestionsCount = 0,
            FileResults = new List<Relay.CLI.Refactoring.FileRefactoringResult>()
        };

        // Act
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = testConsole;

        typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("DisplayAnalysisResults", BindingFlags.NonPublic | BindingFlags.Static, null, [typeof(Relay.CLI.Refactoring.RefactoringResult)], null)!
            .Invoke(null, new object[] { analysis });

        AnsiConsole.Console = originalConsole;

        // Assert - Method executed without exception
        var output = testConsole.Output;
        Assert.Contains("Files Analyzed", output);
        Assert.Contains("0", output);
    }

    [Fact]
    public void RefactorCommand_DisplayAnalysisResults_WithManyFiles_ShouldTruncateDisplay()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();
        var fileResults = new List<Relay.CLI.Refactoring.FileRefactoringResult>();

        // Create 15 files with suggestions
        for (int i = 0; i < 15; i++)
        {
            fileResults.Add(new Relay.CLI.Refactoring.FileRefactoringResult
            {
                FilePath = $"File{i}.cs",
                Suggestions = new List<Relay.CLI.Refactoring.RefactoringSuggestion>
                {
                    new Relay.CLI.Refactoring.RefactoringSuggestion
                    {
                        RuleName = $"Rule{i}",
                        Description = $"Suggestion {i}",
                        Category = Relay.CLI.Refactoring.RefactoringCategory.Readability,
                        Severity = Relay.CLI.Refactoring.RefactoringSeverity.Suggestion,
                        FilePath = $"File{i}.cs",
                        LineNumber = 1,
                        Rationale = $"Rationale {i}"
                    }
                }
            });
        }

        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(1),
            Duration = TimeSpan.FromSeconds(1),
            FilesAnalyzed = 15,
            SuggestionsCount = 15,
            FileResults = fileResults
        };

        // Act
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = testConsole;

        typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("DisplayAnalysisResults", BindingFlags.NonPublic | BindingFlags.Static, null, [typeof(Relay.CLI.Refactoring.RefactoringResult)], null)!
            .Invoke(null, new object[] { analysis });

        AnsiConsole.Console = originalConsole;

        // Assert - Should show truncation message
        var output = testConsole.Output;
        Assert.Contains("... and 5 more files", output);
    }

    [Fact]
    public void RefactorCommand_DisplayAnalysisResults_WithAllSeverities_ShouldShowCorrectIcons()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();
        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(1),
            Duration = TimeSpan.FromSeconds(1),
            FilesAnalyzed = 1,
            SuggestionsCount = 4,
            FileResults = new List<Relay.CLI.Refactoring.FileRefactoringResult>
            {
                new Relay.CLI.Refactoring.FileRefactoringResult
                {
                    FilePath = "Test.cs",
                    Suggestions = new List<Relay.CLI.Refactoring.RefactoringSuggestion>
                    {
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "ErrorRule",
                            Description = "Error suggestion",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.Security,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Error,
                            FilePath = "Test.cs",
                            LineNumber = 1,
                            Rationale = "Security issue"
                        },
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "WarningRule",
                            Description = "Warning suggestion",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.Performance,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Warning,
                            FilePath = "Test.cs",
                            LineNumber = 2,
                            Rationale = "Performance issue"
                        },
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "SuggestionRule",
                            Description = "Suggestion",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.Readability,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Suggestion,
                            FilePath = "Test.cs",
                            LineNumber = 3,
                            Rationale = "Readability improvement"
                        },
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "InfoRule",
                            Description = "Info suggestion",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.BestPractices,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Info,
                            FilePath = "Test.cs",
                            LineNumber = 4,
                            Rationale = "Best practice"
                        }
                    }
                }
            }
        };

        // Act
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = testConsole;

        typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("DisplayAnalysisResults", BindingFlags.NonPublic | BindingFlags.Static, null, [typeof(Relay.CLI.Refactoring.RefactoringResult)], null)!
            .Invoke(null, new object[] { analysis });

        AnsiConsole.Console = originalConsole;

        // Assert - Should show all severity icons
        var output = testConsole.Output;
        Assert.Contains("❌", output); // Error
        Assert.Contains("⚠️", output); // Warning
        Assert.Contains("💡", output); // Suggestion
        Assert.Contains("ℹ️", output); // Info
    }

    [Fact]
    public void RefactorCommand_DisplayApplyResults_WithSuccessStatus_ShouldShowGreen()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();
        var applyResult = new Relay.CLI.Refactoring.ApplyResult
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(1),
            Duration = TimeSpan.FromSeconds(1),
            FilesModified = 2,
            RefactoringsApplied = 3,
            Status = Relay.CLI.Refactoring.RefactoringStatus.Success
        };

        // Act
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = testConsole;

        typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("DisplayApplyResults", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { applyResult, false });

        AnsiConsole.Console = originalConsole;

        // Assert
        var output = testConsole.Output;
        Assert.Contains("✅ Success", output);
        Assert.Contains("Files Modified", output);
        Assert.Contains("2", output);
        Assert.Contains("Refactorings Applied", output);
        Assert.Contains("3", output);
    }

    [Fact]
    public void RefactorCommand_DisplayApplyResults_WithPartialStatus_ShouldShowYellow()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();
        var applyResult = new Relay.CLI.Refactoring.ApplyResult
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(1),
            Duration = TimeSpan.FromSeconds(1),
            FilesModified = 1,
            RefactoringsApplied = 1,
            Status = Relay.CLI.Refactoring.RefactoringStatus.Partial
        };

        // Act
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = testConsole;

        typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("DisplayApplyResults", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { applyResult, false });

        AnsiConsole.Console = originalConsole;

        // Assert
        var output = testConsole.Output;
        Assert.Contains("⚠️  Partial Success", output);
    }

    [Fact]
    public void RefactorCommand_DisplayApplyResults_WithFailedStatus_ShouldShowRed()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();
        var applyResult = new Relay.CLI.Refactoring.ApplyResult
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(1),
            Duration = TimeSpan.FromSeconds(1),
            FilesModified = 0,
            RefactoringsApplied = 0,
            Status = Relay.CLI.Refactoring.RefactoringStatus.Failed,
            Error = "Test error message"
        };

        // Act
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = testConsole;

        typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("DisplayApplyResults", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { applyResult, false });

        AnsiConsole.Console = originalConsole;

        // Assert
        var output = testConsole.Output;
        Assert.Contains("❌ Failed", output);
        Assert.Contains("Test error message", output);
    }

    [Fact]
    public void RefactorCommand_DisplayApplyResults_WithDryRun_ShouldShowDryRunTitle()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();
        var applyResult = new Relay.CLI.Refactoring.ApplyResult
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(1),
            Duration = TimeSpan.FromSeconds(1),
            FilesModified = 1,
            RefactoringsApplied = 2,
            Status = Relay.CLI.Refactoring.RefactoringStatus.Success
        };

        // Act
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = testConsole;

        typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("DisplayApplyResults", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { applyResult, true });

        AnsiConsole.Console = originalConsole;

        // Assert
        var output = testConsole.Output;
        Assert.Contains("🔍 Dry Run Results", output);
    }

    [Fact]
    public void RefactorCommand_DisplayApplyResults_WithoutDryRun_ShouldShowRefactoringTitle()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();
        var applyResult = new Relay.CLI.Refactoring.ApplyResult
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(1),
            Duration = TimeSpan.FromSeconds(1),
            FilesModified = 1,
            RefactoringsApplied = 2,
            Status = Relay.CLI.Refactoring.RefactoringStatus.Success
        };

        // Act
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = testConsole;

        typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("DisplayApplyResults", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { applyResult, false });

        AnsiConsole.Console = originalConsole;

        // Assert
        var output = testConsole.Output;
        Assert.Contains("🔧 Refactoring Results", output);
    }

    [Fact]
    public void RefactorCommand_GenerateMarkdownReport_WithApplyResult_ShouldIncludeApplyMetrics()
    {
        // Arrange
        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow.AddSeconds(-2),
            EndTime = DateTime.UtcNow.AddSeconds(-1),
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
            FilesModified = 2,
            RefactoringsApplied = 2,
            Status = Relay.CLI.Refactoring.RefactoringStatus.Success
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateMarkdownReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, applyResult });

        // Assert
        Assert.Contains("| Files Modified | 2 |", result);
        Assert.Contains("| Refactorings Applied | 2 |", result);
        Assert.Contains("| Status | Success |", result);
    }

    [Fact]
    public void RefactorCommand_GenerateMarkdownReport_WithOriginalAndSuggestedCode_ShouldIncludeCodeBlocks()
    {
        // Arrange
        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow.AddSeconds(-1),
            EndTime = DateTime.UtcNow,
            Duration = TimeSpan.FromSeconds(1),
            FilesAnalyzed = 1,
            SuggestionsCount = 1,
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
                            Rationale = "Test rationale",
                            OriginalCode = "var x = 1;",
                            SuggestedCode = "const x = 1;"
                        }
                    }
                }
            }
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateMarkdownReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, null });

        // Assert
        Assert.Contains("**Original Code:**", result);
        Assert.Contains("```csharp", result);
        Assert.Contains("var x = 1;", result);
        Assert.Contains("**Suggested Code:**", result);
        Assert.Contains("const x = 1;", result);
    }

    [Fact]
    public void RefactorCommand_GenerateHtmlReport_WithNullApplyResult_ShouldShowAnalysisOnly()
    {
        // Arrange
        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow.AddSeconds(-1),
            EndTime = DateTime.UtcNow,
            Duration = TimeSpan.FromSeconds(1),
            FilesAnalyzed = 1,
            SuggestionsCount = 0,
            FileResults = new List<Relay.CLI.Refactoring.FileRefactoringResult>()
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateHtmlReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, null });

        // Assert
        Assert.Contains("Analysis Only", result);
        Assert.DoesNotContain("Apply Duration:", result);
    }

    [Fact]
    public void RefactorCommand_GenerateHtmlReport_WithManySuggestions_ShouldGroupByFile()
    {
        // Arrange
        var analysis = new Relay.CLI.Refactoring.RefactoringResult
        {
            StartTime = DateTime.UtcNow.AddSeconds(-1),
            EndTime = DateTime.UtcNow,
            Duration = TimeSpan.FromSeconds(1),
            FilesAnalyzed = 2,
            SuggestionsCount = 3,
            FileResults = new List<Relay.CLI.Refactoring.FileRefactoringResult>
            {
                new Relay.CLI.Refactoring.FileRefactoringResult
                {
                    FilePath = "File1.cs",
                    Suggestions = new List<Relay.CLI.Refactoring.RefactoringSuggestion>
                    {
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "Rule1",
                            Description = "Suggestion 1",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.Readability,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Suggestion,
                            FilePath = "File1.cs",
                            LineNumber = 1,
                            Rationale = "Rationale 1"
                        }
                    }
                },
                new Relay.CLI.Refactoring.FileRefactoringResult
                {
                    FilePath = "File2.cs",
                    Suggestions = new List<Relay.CLI.Refactoring.RefactoringSuggestion>
                    {
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "Rule2",
                            Description = "Suggestion 2",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.Performance,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Warning,
                            FilePath = "File2.cs",
                            LineNumber = 1,
                            Rationale = "Rationale 2"
                        },
                        new Relay.CLI.Refactoring.RefactoringSuggestion
                        {
                            RuleName = "Rule3",
                            Description = "Suggestion 3",
                            Category = Relay.CLI.Refactoring.RefactoringCategory.Security,
                            Severity = Relay.CLI.Refactoring.RefactoringSeverity.Error,
                            FilePath = "File2.cs",
                            LineNumber = 2,
                            Rationale = "Rationale 3"
                        }
                    }
                }
            }
        };

        // Act
        var result = (string)typeof(Relay.CLI.Commands.RefactorCommand)
            .GetMethod("GenerateHtmlReport", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { analysis, null });

        // Assert
        Assert.Contains("File1.cs", result);
        Assert.Contains("File2.cs", result);
        Assert.Contains("Suggestion 1", result);
        Assert.Contains("Suggestion 2", result);
        Assert.Contains("Suggestion 3", result);
        Assert.Contains("(1 suggestion(s))", result);
        Assert.Contains("(2 suggestion(s))", result);
    }
}

