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
        DisplayAnalysisResults(analysis, AnsiConsole.Console);
    }

    private static void DisplayAnalysisResults(RefactoringResult analysis, IAnsiConsole console)
    {
        console.WriteLine();
        console.MarkupLine("[bold cyan]📊 Analysis Results[/]");
        console.MarkupLine($"[bold]Files Analyzed:[/] {analysis.FilesAnalyzed}");
        console.MarkupLine($"[bold]Suggestions Found:[/] {analysis.SuggestionsCount}");
        console.MarkupLine($"[bold]Files with Issues:[/] {analysis.FileResults.Count}");
        console.MarkupLine($"[bold]Analysis Duration:[/] {analysis.Duration.TotalSeconds:F2}s");
        console.WriteLine();

        if (analysis.SuggestionsCount > 0)
        {
            console.MarkupLine("[bold cyan]🔍 Refactoring Suggestions:[/]");

            foreach (var fileResult in analysis.FileResults.Take(10))
            {
                var fileName = Path.GetFileName(fileResult.FilePath);
                console.MarkupLine($"[yellow]{fileName}[/] ({fileResult.Suggestions.Count} suggestion(s))");

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
                    console.MarkupLine($"  {icon} Line {suggestion.LineNumber}: {suggestion.Description} {categoryBadge}");
                }

                if (fileResult.Suggestions.Count > 5)
                {
                    console.MarkupLine($"  [dim]... and {fileResult.Suggestions.Count - 5} more[/]");
                }
            }

            if (analysis.FileResults.Count > 10)
            {
                console.MarkupLine($"[dim]... and {analysis.FileResults.Count - 10} more files[/]");
            }

            console.WriteLine();
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
        var sb = new StringBuilder();

        var statusColor = applyResult?.Status switch
        {
            RefactoringStatus.Success => "#4CAF50",
            RefactoringStatus.Partial => "#FFC107",
            _ => "#F44336"
        };

        var statusIcon = applyResult?.Status switch
        {
            RefactoringStatus.Success => "✅",
            RefactoringStatus.Partial => "⚠️",
            _ => "❌"
        };

        var statusText = applyResult?.Status.ToString() ?? "Analysis Only";

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"UTF-8\">");
        sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("    <title>Refactoring Report</title>");
        sb.AppendLine("    <style>");
        sb.AppendLine("        * { margin: 0; padding: 0; box-sizing: border-box; }");
        sb.AppendLine("        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif; background: #f5f5f5; color: #333; line-height: 1.6; }");
        sb.AppendLine("        .container { max-width: 1200px; margin: 0 auto; padding: 40px 20px; }");
        sb.AppendLine("        .header { background: white; border-radius: 8px; padding: 30px; margin-bottom: 30px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
        sb.AppendLine("        .header h1 { font-size: 32px; margin-bottom: 20px; }");
        sb.AppendLine($"        .status {{ display: inline-block; padding: 8px 16px; border-radius: 4px; background: {statusColor}; color: white; font-weight: 600; }}");
        sb.AppendLine("        .meta { margin-top: 15px; color: #666; font-size: 14px; }");
        sb.AppendLine("        .section { background: white; border-radius: 8px; padding: 25px; margin-bottom: 20px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
        sb.AppendLine("        .section h2 { font-size: 24px; margin-bottom: 20px; color: #1976D2; border-bottom: 2px solid #1976D2; padding-bottom: 10px; }");
        sb.AppendLine("        .section h3 { font-size: 18px; margin-top: 20px; margin-bottom: 10px; color: #424242; }");
        sb.AppendLine("        table { width: 100%; border-collapse: collapse; margin-top: 15px; }");
        sb.AppendLine("        th, td { padding: 12px; text-align: left; border-bottom: 1px solid #e0e0e0; }");
        sb.AppendLine("        th { background: #f5f5f5; font-weight: 600; color: #424242; }");
        sb.AppendLine("        tr:hover { background: #fafafa; }");
        sb.AppendLine("        .suggestion-item { padding: 15px; margin: 10px 0; border-left: 4px solid #e0e0e0; background: #fafafa; border-radius: 4px; }");
        sb.AppendLine("        .suggestion-error { border-left-color: #F44336; }");
        sb.AppendLine("        .suggestion-warning { border-left-color: #FFC107; }");
        sb.AppendLine("        .suggestion-suggestion { border-left-color: #2196F3; }");
        sb.AppendLine("        .suggestion-info { border-left-color: #9E9E9E; }");
        sb.AppendLine("        .icon-error { color: #F44336; }");
        sb.AppendLine("        .icon-warning { color: #FFC107; }");
        sb.AppendLine("        .icon-suggestion { color: #2196F3; }");
        sb.AppendLine("        .icon-info { color: #9E9E9E; }");
        sb.AppendLine("        .file-section { margin-top: 20px; }");
        sb.AppendLine("        .file-header { background: #f0f0f0; padding: 10px; border-radius: 4px; margin-bottom: 10px; }");
        sb.AppendLine("        code { background: #f4f4f4; padding: 2px 6px; border-radius: 3px; font-family: 'Courier New', monospace; font-size: 14px; }");
        sb.AppendLine("        pre { background: #263238; color: #aed581; padding: 15px; border-radius: 4px; overflow-x: auto; margin-top: 10px; }");
        sb.AppendLine("        pre code { background: none; color: inherit; }");
        sb.AppendLine("        .footer { text-align: center; color: #999; margin-top: 40px; padding-top: 20px; border-top: 1px solid #e0e0e0; }");
        sb.AppendLine("        .badge { display: inline-block; padding: 2px 8px; border-radius: 12px; font-size: 12px; font-weight: 600; margin-left: 8px; }");
        sb.AppendLine("        .badge-performance { background: #E91E63; color: white; }");
        sb.AppendLine("        .badge-readability { background: #9C27B0; color: white; }");
        sb.AppendLine("        .badge-modernization { background: #3F51B5; color: white; }");
        sb.AppendLine("        .badge-bestpractices { background: #009688; color: white; }");
        sb.AppendLine("        .badge-maintainability { background: #FF9800; color: white; }");
        sb.AppendLine("        .badge-security { background: #F44336; color: white; }");
        sb.AppendLine("        .badge-asyncawait { background: #4CAF50; color: white; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <div class=\"container\">");

        // Header
        sb.AppendLine("        <div class=\"header\">");
        sb.AppendLine("            <h1>🔧 Code Refactoring Report</h1>");
        sb.AppendLine($"            <div class=\"status\">{statusIcon} {statusText}</div>");
        sb.AppendLine($"            <div class=\"meta\">Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Analysis Duration: {analysis.Duration.TotalSeconds:F2}s</div>");
        if (applyResult != null)
        {
            sb.AppendLine($"            <div class=\"meta\">Apply Duration: {applyResult.Duration.TotalSeconds:F2}s</div>");
        }
        sb.AppendLine("        </div>");

        // Summary
        sb.AppendLine("        <div class=\"section\">");
        sb.AppendLine("            <h2>📊 Summary</h2>");
        sb.AppendLine("            <table>");
        sb.AppendLine("                <thead>");
        sb.AppendLine("                    <tr><th>Metric</th><th>Value</th></tr>");
        sb.AppendLine("                </thead>");
        sb.AppendLine("                <tbody>");
        sb.AppendLine($"                    <tr><td>Files Analyzed</td><td>{analysis.FilesAnalyzed}</td></tr>");
        sb.AppendLine($"                    <tr><td>Suggestions Found</td><td>{analysis.SuggestionsCount}</td></tr>");
        sb.AppendLine($"                    <tr><td>Files with Issues</td><td>{analysis.FileResults.Count}</td></tr>");

        if (applyResult != null)
        {
            sb.AppendLine($"                    <tr><td>Files Modified</td><td>{applyResult.FilesModified}</td></tr>");
            sb.AppendLine($"                    <tr><td>Refactorings Applied</td><td>{applyResult.RefactoringsApplied}</td></tr>");
        }

        sb.AppendLine("                </tbody>");
        sb.AppendLine("            </table>");
        sb.AppendLine("        </div>");

        // Refactoring Suggestions
        if (analysis.SuggestionsCount > 0)
        {
            sb.AppendLine("        <div class=\"section\">");
            sb.AppendLine("            <h2>💡 Refactoring Suggestions</h2>");

            foreach (var fileResult in analysis.FileResults)
            {
                sb.AppendLine("            <div class=\"file-section\">");
                sb.AppendLine($"                <div class=\"file-header\"><strong>{System.Web.HttpUtility.HtmlEncode(Path.GetFileName(fileResult.FilePath))}</strong> ({fileResult.Suggestions.Count} suggestion(s))</div>");

                foreach (var suggestion in fileResult.Suggestions)
                {
                    var cssClass = suggestion.Severity switch
                    {
                        RefactoringSeverity.Error => "suggestion-error",
                        RefactoringSeverity.Warning => "suggestion-warning",
                        RefactoringSeverity.Suggestion => "suggestion-suggestion",
                        _ => "suggestion-info"
                    };

                    var icon = suggestion.Severity switch
                    {
                        RefactoringSeverity.Error => "<span class='icon-error'>❌</span>",
                        RefactoringSeverity.Warning => "<span class='icon-warning'>⚠️</span>",
                        RefactoringSeverity.Suggestion => "<span class='icon-suggestion'>💡</span>",
                        _ => "<span class='icon-info'>ℹ️</span>"
                    };

                    var badgeClass = $"badge-{suggestion.Category.ToString().ToLower()}";

                    sb.AppendLine($"                <div class=\"suggestion-item {cssClass}\">");
                    sb.AppendLine($"                    {icon} <strong>Line {suggestion.LineNumber}:</strong> {System.Web.HttpUtility.HtmlEncode(suggestion.Description)}");
                    sb.AppendLine($"                    <span class=\"badge {badgeClass}\">{suggestion.Category}</span>");
                    sb.AppendLine("                    <br><small><strong>Rule:</strong> " + System.Web.HttpUtility.HtmlEncode(suggestion.RuleName) + "</small>");
                    sb.AppendLine("                    <br><small><strong>Rationale:</strong> " + System.Web.HttpUtility.HtmlEncode(suggestion.Rationale) + "</small>");

                    if (!string.IsNullOrEmpty(suggestion.OriginalCode))
                    {
                        sb.AppendLine("                    <br><br><strong>Original Code:</strong>");
                        sb.AppendLine("                    <pre><code>" + System.Web.HttpUtility.HtmlEncode(suggestion.OriginalCode) + "</code></pre>");
                    }

                    if (!string.IsNullOrEmpty(suggestion.SuggestedCode))
                    {
                        sb.AppendLine("                    <br><strong>Suggested Code:</strong>");
                        sb.AppendLine("                    <pre><code>" + System.Web.HttpUtility.HtmlEncode(suggestion.SuggestedCode) + "</code></pre>");
                    }

                    sb.AppendLine("                </div>");
                }

                sb.AppendLine("            </div>");
            }

            sb.AppendLine("        </div>");
        }

        // Error Information
        if (applyResult != null && !string.IsNullOrEmpty(applyResult.Error))
        {
            sb.AppendLine("        <div class=\"section\">");
            sb.AppendLine("            <h2>❌ Error Information</h2>");
            sb.AppendLine("            <div style=\"background: #FFEBEE; border-left: 4px solid #F44336; padding: 15px; border-radius: 4px;\">");
            sb.AppendLine($"                <strong>Error:</strong> {System.Web.HttpUtility.HtmlEncode(applyResult.Error)}");
            sb.AppendLine("            </div>");
            sb.AppendLine("        </div>");
        }

        // Footer
        sb.AppendLine("        <div class=\"footer\">");
        sb.AppendLine("            <p>Generated by <strong>Relay CLI Refactoring Tool</strong></p>");
        sb.AppendLine("        </div>");

        sb.AppendLine("    </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }
}
