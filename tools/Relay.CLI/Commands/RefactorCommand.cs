using System.CommandLine;
using System.Text;
using Spectre.Console;
using Relay.CLI.Refactoring;

namespace Relay.CLI.Commands;

/// <summary>
/// Refactor command - Automated code refactoring and modernization
/// </summary>
public static class RefactorCommand
{
    public static Command Create()
    {
        var command = new Command("refactor", "Automated code refactoring and modernization");

        var pathOption = new Option<string>("--path", () => ".", "Project path to refactor");
        var analyzeOnlyOption = new Option<bool>("--analyze-only", () => false, "Only analyze without applying changes");
        var dryRunOption = new Option<bool>("--dry-run", () => false, "Show changes without applying them");
        var interactiveOption = new Option<bool>("--interactive", () => false, "Prompt for each refactoring");
        var rulesOption = new Option<string[]>("--rules", () => Array.Empty<string>(), "Specific rules to apply");
        var categoryOption = new Option<string[]>("--category", () => Array.Empty<string>(), "Refactoring categories (Performance, Readability, etc.)");
        var severityOption = new Option<string>("--min-severity", () => "Info", "Minimum severity (Info, Suggestion, Warning, Error)");
        var outputOption = new Option<string?>("--output", "Refactoring report output path");
        var formatOption = new Option<string>("--format", () => "markdown", "Report format (markdown, json, html)");
        var backupOption = new Option<bool>("--backup", () => true, "Create backup before refactoring");

        command.AddOption(pathOption);
        command.AddOption(analyzeOnlyOption);
        command.AddOption(dryRunOption);
        command.AddOption(interactiveOption);
        command.AddOption(rulesOption);
        command.AddOption(categoryOption);
        command.AddOption(severityOption);
        command.AddOption(outputOption);
        command.AddOption(formatOption);
        command.AddOption(backupOption);

        command.SetHandler(async (context) =>
        {
            var path = context.ParseResult.GetValueForOption(pathOption)!;
            var analyzeOnly = context.ParseResult.GetValueForOption(analyzeOnlyOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var interactive = context.ParseResult.GetValueForOption(interactiveOption);
            var rules = context.ParseResult.GetValueForOption(rulesOption)!;
            var categories = context.ParseResult.GetValueForOption(categoryOption)!;
            var severity = context.ParseResult.GetValueForOption(severityOption)!;
            var output = context.ParseResult.GetValueForOption(outputOption);
            var format = context.ParseResult.GetValueForOption(formatOption)!;
            var backup = context.ParseResult.GetValueForOption(backupOption);

            await ExecuteRefactor(path, analyzeOnly, dryRun, interactive, rules, categories, severity, output, format, backup);
        });

        return command;
    }

    internal static async Task ExecuteRefactor(
        string projectPath,
        bool analyzeOnly,
        bool dryRun,
        bool interactive,
        string[] rules,
        string[] categories,
        string severityStr,
        string? outputFile,
        string format,
        bool createBackup)
    {
        var rule = new Rule("[cyan]🔧 Code Refactoring Engine[/]");
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        if (!Enum.TryParse<RefactoringSeverity>(severityStr, true, out var minSeverity))
        {
            minSeverity = RefactoringSeverity.Info;
        }

        var parsedCategories = categories
            .Select(c => Enum.TryParse<RefactoringCategory>(c, true, out var category) ? category : (RefactoringCategory?)null)
            .Where(c => c.HasValue)
            .Select(c => c!.Value)
            .ToList();

        var options = new RefactoringOptions
        {
            ProjectPath = Path.GetFullPath(projectPath),
            DryRun = dryRun || analyzeOnly,
            Interactive = interactive,
            MinimumSeverity = minSeverity,
            SpecificRules = rules.ToList(),
            Categories = parsedCategories,
            CreateBackup = createBackup && !dryRun && !analyzeOnly
        };

        var engine = new RefactoringEngine();
        RefactoringResult? analysis = null;
        ApplyResult? applyResult = null;

        try
        {
            await AnsiConsole.Status()
                .StartAsync("Analyzing code...", async ctx =>
                {
                    ctx.Status("🔍 Scanning for refactoring opportunities...");
                    analysis = await engine.AnalyzeAsync(options);
                    await Task.Delay(300);

                    DisplayAnalysisResults(analysis);

                    if (analyzeOnly)
                    {
                        ctx.Status("✅ Analysis complete");
                        return;
                    }

                    if (analysis.SuggestionsCount == 0)
                    {
                        AnsiConsole.MarkupLine("[green]✅ No refactoring suggestions found. Code looks good![/]");
                        return;
                    }

                    // Confirm refactoring
                    if (!dryRun && !interactive && !AnsiConsole.Confirm($"[yellow]Apply {analysis.SuggestionsCount} refactoring(s)?[/]"))
                    {
                        AnsiConsole.MarkupLine("[yellow]Refactoring cancelled by user[/]");
                        return;
                    }

                    // Apply refactorings
                    ctx.Status("🔧 Applying refactorings...");
                    applyResult = await engine.ApplyRefactoringsAsync(options, analysis);
                    await Task.Delay(300);
                });

            // Display results
            if (applyResult != null)
            {
                DisplayApplyResults(applyResult, dryRun);
            }

            // Save report
            if (!string.IsNullOrEmpty(outputFile) && analysis != null)
            {
                await SaveRefactoringReport(analysis, applyResult, outputFile, format);
                AnsiConsole.MarkupLine($"[green]📄 Report saved to: {outputFile}[/]");
            }

            // Set exit code
            Environment.ExitCode = applyResult?.Status == RefactoringStatus.Success ? 0 :
                                   (analysis?.SuggestionsCount == 0 ? 0 : 1);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[red]❌ Refactoring failed:[/]");
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            Environment.ExitCode = 1;
        }
    }

    private static void DisplayAnalysisResults(RefactoringResult analysis)
    {
        AnsiConsole.WriteLine();

        var summary = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Metric[/]")
            .AddColumn("[bold]Value[/]");

        summary.AddRow("Files Analyzed", analysis.FilesAnalyzed.ToString());
        summary.AddRow("Suggestions Found", analysis.SuggestionsCount.ToString());
        summary.AddRow("Files with Issues", analysis.FileResults.Count.ToString());
        summary.AddRow("Analysis Duration", $"{analysis.Duration.TotalSeconds:F2}s");

        var panel = new Panel(summary)
            .Header("[bold cyan]📊 Analysis Results[/]")
            .BorderColor(Color.Cyan1);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        if (analysis.SuggestionsCount > 0)
        {
            AnsiConsole.MarkupLine("[bold cyan]🔍 Refactoring Suggestions:[/]");

            var tree = new Tree("Suggestions");

            foreach (var fileResult in analysis.FileResults.Take(10))
            {
                var fileName = Path.GetFileName(fileResult.FilePath);
                var fileNode = tree.AddNode($"[yellow]{fileName}[/] ({fileResult.Suggestions.Count} suggestion(s))");

                foreach (var suggestion in fileResult.Suggestions.Take(5))
                {
                    var icon = suggestion.Severity switch
                    {
                        RefactoringSeverity.Error => "[red]❌[/]",
                        RefactoringSeverity.Warning => "[yellow]⚠️[/]",
                        RefactoringSeverity.Suggestion => "[blue]💡[/]",
                        _ => "[gray]ℹ️[/]"
                    };

                    var categoryBadge = $"[dim]({suggestion.Category})[/]";
                    fileNode.AddNode($"{icon} Line {suggestion.LineNumber}: {suggestion.Description} {categoryBadge}");
                }

                if (fileResult.Suggestions.Count > 5)
                {
                    fileNode.AddNode($"[dim]... and {fileResult.Suggestions.Count - 5} more[/]");
                }
            }

            if (analysis.FileResults.Count > 10)
            {
                tree.AddNode($"[dim]... and {analysis.FileResults.Count - 10} more files[/]");
            }

            AnsiConsole.Write(tree);
            AnsiConsole.WriteLine();
        }
    }

    private static void DisplayApplyResults(ApplyResult result, bool isDryRun)
    {
        AnsiConsole.WriteLine();

        var statusColor = result.Status switch
        {
            RefactoringStatus.Success => "green",
            RefactoringStatus.Partial => "yellow",
            _ => "red"
        };

        var statusText = result.Status switch
        {
            RefactoringStatus.Success => "✅ Success",
            RefactoringStatus.Partial => "⚠️  Partial Success",
            _ => "❌ Failed"
        };

        var summary = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Metric[/]")
            .AddColumn("[bold]Value[/]");

        summary.AddRow("Status", $"[{statusColor}]{statusText}[/]");
        summary.AddRow("Duration", $"{result.Duration.TotalSeconds:F2}s");
        summary.AddRow("Files Modified", result.FilesModified.ToString());
        summary.AddRow("Refactorings Applied", result.RefactoringsApplied.ToString());

        var panel = new Panel(summary)
            .Header($"[bold]{(isDryRun ? "🔍 Dry Run Results" : "🔧 Refactoring Results")}[/]")
            .BorderColor(statusColor == "green" ? Color.Green : (statusColor == "yellow" ? Color.Yellow : Color.Red));

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        if (!string.IsNullOrEmpty(result.Error))
        {
            AnsiConsole.MarkupLine($"[red]Error: {result.Error.Replace("[", "[[").Replace("]", "]]")}[/]");
        }
    }

    private static async Task SaveRefactoringReport(RefactoringResult analysis, ApplyResult? applyResult, string outputPath, string format)
    {
        var content = format.ToLower() switch
        {
            "json" => GenerateJsonReport(analysis, applyResult),
            "html" => GenerateHtmlReport(analysis, applyResult),
            _ => GenerateMarkdownReport(analysis, applyResult)
        };

        await File.WriteAllTextAsync(outputPath, content);
    }

    private static string GenerateMarkdownReport(RefactoringResult analysis, ApplyResult? applyResult)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Code Refactoring Report");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**Analysis Duration:** {analysis.Duration.TotalSeconds:F2}s");
        sb.AppendLine();

        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine($"| Files Analyzed | {analysis.FilesAnalyzed} |");
        sb.AppendLine($"| Suggestions Found | {analysis.SuggestionsCount} |");
        sb.AppendLine($"| Files with Issues | {analysis.FileResults.Count} |");

        if (applyResult != null)
        {
            sb.AppendLine($"| Files Modified | {applyResult.FilesModified} |");
            sb.AppendLine($"| Refactorings Applied | {applyResult.RefactoringsApplied} |");
            sb.AppendLine($"| Status | {applyResult.Status} |");
        }

        sb.AppendLine();

        if (analysis.SuggestionsCount > 0)
        {
            sb.AppendLine("## Refactoring Suggestions");
            sb.AppendLine();

            foreach (var fileResult in analysis.FileResults)
            {
                sb.AppendLine($"### {Path.GetFileName(fileResult.FilePath)}");
                sb.AppendLine();

                foreach (var suggestion in fileResult.Suggestions)
                {
                    var icon = suggestion.Severity switch
                    {
                        RefactoringSeverity.Error => "❌",
                        RefactoringSeverity.Warning => "⚠️",
                        RefactoringSeverity.Suggestion => "💡",
                        _ => "ℹ️"
                    };

                    sb.AppendLine($"#### {icon} Line {suggestion.LineNumber}: {suggestion.Description}");
                    sb.AppendLine();
                    sb.AppendLine($"**Category:** {suggestion.Category}  ");
                    sb.AppendLine($"**Severity:** {suggestion.Severity}  ");
                    sb.AppendLine($"**Rule:** {suggestion.RuleName}");
                    sb.AppendLine();
                    sb.AppendLine($"**Rationale:** {suggestion.Rationale}");
                    sb.AppendLine();

                    if (!string.IsNullOrEmpty(suggestion.OriginalCode))
                    {
                        sb.AppendLine("**Original Code:**");
                        sb.AppendLine("```csharp");
                        sb.AppendLine(suggestion.OriginalCode);
                        sb.AppendLine("```");
                        sb.AppendLine();
                    }

                    if (!string.IsNullOrEmpty(suggestion.SuggestedCode))
                    {
                        sb.AppendLine("**Suggested Code:**");
                        sb.AppendLine("```csharp");
                        sb.AppendLine(suggestion.SuggestedCode);
                        sb.AppendLine("```");
                        sb.AppendLine();
                    }
                }
            }
        }

        sb.AppendLine("---");
        sb.AppendLine("*Generated by Relay CLI Refactoring Tool*");

        return sb.ToString();
    }

    private static string GenerateJsonReport(RefactoringResult analysis, ApplyResult? applyResult)
    {
        var report = new
        {
            GeneratedAt = DateTime.Now,
            Analysis = analysis,
            ApplyResult = applyResult
        };

        return System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static string GenerateHtmlReport(RefactoringResult analysis, ApplyResult? applyResult)
    {
        // Similar to migration HTML report
        return GenerateMarkdownReport(analysis, applyResult); // Placeholder
    }
}
