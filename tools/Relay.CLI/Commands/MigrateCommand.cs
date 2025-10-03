using System.CommandLine;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Spectre.Console;
using Relay.CLI.Migration;

namespace Relay.CLI.Commands;

/// <summary>
/// Migration command - Automated migration from MediatR to Relay
/// </summary>
public static class MigrateCommand
{
    public static Command Create()
    {
        var command = new Command("migrate", "Migrate from MediatR to Relay with automated transformation");

        var fromOption = new Option<string>("--from", () => "MediatR", "Source framework to migrate from");
        var toOption = new Option<string>("--to", () => "Relay", "Target framework to migrate to");
        var pathOption = new Option<string>("--path", () => ".", "Project path to migrate");
        var analyzeOnlyOption = new Option<bool>("--analyze-only", () => false, "Only analyze without migrating");
        var dryRunOption = new Option<bool>("--dry-run", () => false, "Show changes without applying them");
        var previewOption = new Option<bool>("--preview", () => false, "Show detailed diff preview");
        var backupOption = new Option<bool>("--backup", () => true, "Create backup before migration");
        var backupPathOption = new Option<string>("--backup-path", () => ".backup", "Backup directory path");
        var outputOption = new Option<string?>("--output", "Migration report output path");
        var formatOption = new Option<string>("--format", () => "markdown", "Report format (markdown, json, html)");
        var aggressiveOption = new Option<bool>("--aggressive", () => false, "Apply aggressive optimizations");
        var interactiveOption = new Option<bool>("--interactive", () => false, "Prompt for each change");

        command.AddOption(fromOption);
        command.AddOption(toOption);
        command.AddOption(pathOption);
        command.AddOption(analyzeOnlyOption);
        command.AddOption(dryRunOption);
        command.AddOption(previewOption);
        command.AddOption(backupOption);
        command.AddOption(backupPathOption);
        command.AddOption(outputOption);
        command.AddOption(formatOption);
        command.AddOption(aggressiveOption);
        command.AddOption(interactiveOption);

        command.SetHandler(async (context) =>
        {
            var from = context.ParseResult.GetValueForOption(fromOption)!;
            var to = context.ParseResult.GetValueForOption(toOption)!;
            var path = context.ParseResult.GetValueForOption(pathOption)!;
            var analyzeOnly = context.ParseResult.GetValueForOption(analyzeOnlyOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var preview = context.ParseResult.GetValueForOption(previewOption);
            var backup = context.ParseResult.GetValueForOption(backupOption);
            var backupPath = context.ParseResult.GetValueForOption(backupPathOption)!;
            var output = context.ParseResult.GetValueForOption(outputOption);
            var format = context.ParseResult.GetValueForOption(formatOption)!;
            var aggressive = context.ParseResult.GetValueForOption(aggressiveOption);
            var interactive = context.ParseResult.GetValueForOption(interactiveOption);

            await ExecuteMigrate(from, to, path, analyzeOnly, dryRun, preview, backup, backupPath, output, format, aggressive, interactive);
        });

        return command;
    }

    private static async Task ExecuteMigrate(
        string from,
        string to,
        string projectPath,
        bool analyzeOnly,
        bool dryRun,
        bool preview,
        bool createBackup,
        string backupPath,
        string? outputFile,
        string format,
        bool aggressive,
        bool interactive)
    {
        var rule = new Rule($"[cyan]🔄 Migrating from {from} to {to}[/]");
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        if (from.ToLower() != "mediatr")
        {
            AnsiConsole.MarkupLine("[red]❌ Currently only MediatR migration is supported[/]");
            Environment.ExitCode = 1;
            return;
        }

        if (to.ToLower() != "relay")
        {
            AnsiConsole.MarkupLine("[red]❌ Currently only Relay is supported as migration target[/]");
            Environment.ExitCode = 1;
            return;
        }

        var options = new MigrationOptions
        {
            SourceFramework = from,
            TargetFramework = to,
            ProjectPath = Path.GetFullPath(projectPath),
            AnalyzeOnly = analyzeOnly,
            DryRun = dryRun || analyzeOnly,
            ShowPreview = preview,
            CreateBackup = createBackup && !dryRun && !analyzeOnly,
            BackupPath = backupPath,
            Interactive = interactive,
            Aggressive = aggressive
        };

        var engine = new MigrationEngine();
        MigrationResult? result = null;

        try
        {
            await AnsiConsole.Status()
                .StartAsync("Analyzing project...", async ctx =>
                {
                    // Phase 1: Analysis
                    ctx.Status("🔍 Scanning for MediatR usage...");
                    var analysis = await engine.AnalyzeAsync(options);
                    await Task.Delay(500);

                    DisplayAnalysisResults(analysis);

                    if (analyzeOnly)
                    {
                        ctx.Status("✅ Analysis complete");
                        return;
                    }

                    if (!analysis.CanMigrate)
                    {
                        AnsiConsole.MarkupLine("[red]❌ Migration cannot proceed due to critical issues[/]");
                        return;
                    }

                    // Confirm migration
                    if (!dryRun && !AnsiConsole.Confirm($"[yellow]Proceed with migration of {analysis.FilesAffected} file(s)?[/]"))
                    {
                        AnsiConsole.MarkupLine("[yellow]Migration cancelled by user[/]");
                        return;
                    }

                    // Phase 2: Backup
                    if (createBackup && !dryRun)
                    {
                        ctx.Status("💾 Creating backup...");
                        await engine.CreateBackupAsync(options);
                        await Task.Delay(300);
                        AnsiConsole.MarkupLine("[green]✅ Backup created[/]");
                    }

                    // Phase 3: Migration
                    ctx.Status("🔄 Applying migration...");
                    result = await engine.MigrateAsync(options);
                    await Task.Delay(500);
                });

            // Display results
            if (result != null)
            {
                DisplayMigrationResults(result, dryRun);

                // Save report
                if (!string.IsNullOrEmpty(outputFile))
                {
                    await SaveMigrationReport(result, outputFile, format);
                    AnsiConsole.MarkupLine($"[green]📄 Report saved to: {outputFile}[/]");
                }

                // Set exit code
                Environment.ExitCode = result.Status == MigrationStatus.Success ? 0 : 1;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[red]❌ Migration failed:[/]");
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);

            if (createBackup && result?.BackupPath != null)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]⚠️  You can rollback using:[/]");
                AnsiConsole.MarkupLine($"[dim]relay migrate rollback --backup {result.BackupPath}[/]");
            }

            Environment.ExitCode = 1;
        }
    }

    private static void DisplayAnalysisResults(AnalysisResult analysis)
    {
        AnsiConsole.WriteLine();
        var panel = new Panel(BuildAnalysisReport(analysis))
            .Header("[bold cyan]📊 Analysis Results[/]")
            .BorderColor(Color.Cyan1);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    private static string BuildAnalysisReport(AnalysisResult analysis)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"[bold]Project:[/] {Path.GetFileName(analysis.ProjectPath)}");
        sb.AppendLine($"[bold]Files Affected:[/] {analysis.FilesAffected}");
        sb.AppendLine($"[bold]Handlers Found:[/] {analysis.HandlersFound}");
        sb.AppendLine($"[bold]Requests Found:[/] {analysis.RequestsFound}");
        sb.AppendLine($"[bold]Notifications Found:[/] {analysis.NotificationsFound}");
        sb.AppendLine($"[bold]Pipeline Behaviors:[/] {analysis.PipelineBehaviorsFound}");
        sb.AppendLine();

        if (analysis.PackageReferences.Count > 0)
        {
            sb.AppendLine("[bold]📦 Packages to Update:[/]");
            foreach (var pkg in analysis.PackageReferences)
            {
                sb.AppendLine($"  • {pkg.Name} ([red]{pkg.CurrentVersion}[/] → [green]Relay.Core[/])");
            }
            sb.AppendLine();
        }

        if (analysis.Issues.Count > 0)
        {
            sb.AppendLine($"[bold yellow]⚠️  Issues Found: {analysis.Issues.Count}[/]");
            foreach (var issue in analysis.Issues.Take(5))
            {
                var icon = issue.Severity switch
                {
                    IssueSeverity.Error => "[red]❌[/]",
                    IssueSeverity.Warning => "[yellow]⚠️[/]",
                    _ => "[blue]ℹ️[/]"
                };
                sb.AppendLine($"  {icon} {issue.Message.Replace("[", "[[").Replace("]", "]]")}");
            }
            if (analysis.Issues.Count > 5)
            {
                sb.AppendLine($"  [dim]... and {analysis.Issues.Count - 5} more[/]");
            }
            sb.AppendLine();
        }

        var canMigrateText = analysis.CanMigrate 
            ? "[green]✅ Migration can proceed[/]" 
            : "[red]❌ Migration blocked - fix critical issues first[/]";
        sb.AppendLine(canMigrateText);

        return sb.ToString().TrimEnd();
    }

    private static void DisplayMigrationResults(MigrationResult result, bool isDryRun)
    {
        AnsiConsole.WriteLine();

        var statusColor = result.Status switch
        {
            MigrationStatus.Success => "green",
            MigrationStatus.Partial => "yellow",
            _ => "red"
        };

        var statusText = result.Status switch
        {
            MigrationStatus.Success => "✅ Success",
            MigrationStatus.Partial => "⚠️  Partial Success",
            _ => "❌ Failed"
        };

        var summary = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Metric[/]")
            .AddColumn("[bold]Value[/]");

        summary.AddRow("Status", $"[{statusColor}]{statusText}[/]");
        summary.AddRow("Duration", $"{result.Duration.TotalSeconds:F2}s");
        summary.AddRow("Files Modified", result.FilesModified.ToString());
        summary.AddRow("Lines Changed", result.LinesChanged.ToString());
        summary.AddRow("Handlers Migrated", result.HandlersMigrated.ToString());

        if (result.CreatedBackup)
        {
            summary.AddRow("Backup Path", result.BackupPath ?? "N/A");
        }

        var panel = new Panel(summary)
            .Header($"[bold]{(isDryRun ? "🔍 Dry Run Results" : "🔄 Migration Results")}[/]")
            .BorderColor(statusColor == "green" ? Color.Green : (statusColor == "yellow" ? Color.Yellow : Color.Red));

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // Display changes
        if (result.Changes.Count > 0)
        {
            AnsiConsole.MarkupLine($"[bold cyan]📝 Changes Applied:{(isDryRun ? " (Preview)" : "")}[/]");
            
            var tree = new Tree("Changes");
            
            foreach (var change in result.Changes.GroupBy(c => c.Category))
            {
                var categoryNode = tree.AddNode($"[yellow]{change.Key}[/]");
                foreach (var item in change.Take(10))
                {
                    var icon = item.Type switch
                    {
                        ChangeType.Add => "[green]+[/]",
                        ChangeType.Remove => "[red]-[/]",
                        ChangeType.Modify => "[yellow]~[/]",
                        _ => "[blue]•[/]"
                    };
                    categoryNode.AddNode($"{icon} {item.Description.Replace("[", "[[").Replace("]", "]]")}");
                }
                if (change.Count() > 10)
                {
                    categoryNode.AddNode($"[dim]... and {change.Count() - 10} more[/]");
                }
            }

            AnsiConsole.Write(tree);
            AnsiConsole.WriteLine();
        }

        // Display manual steps if any
        if (result.ManualSteps.Count > 0)
        {
            AnsiConsole.MarkupLine("[bold yellow]⚠️  Manual Steps Required:[/]");
            var manualList = new List<string>();
            foreach (var step in result.ManualSteps)
            {
                manualList.Add($"[yellow]•[/] {step.Replace("[", "[[").Replace("]", "]]")}");
            }
            AnsiConsole.Write(new Rows(manualList.Select(s => new Markup(s))));
            AnsiConsole.WriteLine();
        }

        // Rollback info
        if (!isDryRun && result.CreatedBackup && result.BackupPath != null)
        {
            AnsiConsole.MarkupLine("[dim]💡 To rollback: relay migrate rollback --backup {0}[/]", result.BackupPath);
        }
    }

    private static async Task SaveMigrationReport(MigrationResult result, string outputPath, string format)
    {
        var content = format.ToLower() switch
        {
            "json" => GenerateJsonReport(result),
            "html" => GenerateHtmlReport(result),
            _ => GenerateMarkdownReport(result)
        };

        await File.WriteAllTextAsync(outputPath, content);
    }

    private static string GenerateMarkdownReport(MigrationResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Migration Report");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**Status:** {result.Status}");
        sb.AppendLine($"**Duration:** {result.Duration.TotalSeconds:F2}s");
        sb.AppendLine();

        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine($"| Files Modified | {result.FilesModified} |");
        sb.AppendLine($"| Lines Changed | {result.LinesChanged} |");
        sb.AppendLine($"| Handlers Migrated | {result.HandlersMigrated} |");
        sb.AppendLine();

        if (result.Changes.Count > 0)
        {
            sb.AppendLine("## Changes Applied");
            sb.AppendLine();
            foreach (var category in result.Changes.GroupBy(c => c.Category))
            {
                sb.AppendLine($"### {category.Key}");
                sb.AppendLine();
                foreach (var change in category)
                {
                    var icon = change.Type switch
                    {
                        ChangeType.Add => "➕",
                        ChangeType.Remove => "➖",
                        ChangeType.Modify => "✏️",
                        _ => "•"
                    };
                    sb.AppendLine($"- {icon} {change.Description}");
                }
                sb.AppendLine();
            }
        }

        if (result.ManualSteps.Count > 0)
        {
            sb.AppendLine("## Manual Steps Required");
            sb.AppendLine();
            for (int i = 0; i < result.ManualSteps.Count; i++)
            {
                sb.AppendLine($"{i + 1}. {result.ManualSteps[i]}");
            }
            sb.AppendLine();
        }

        if (result.CreatedBackup)
        {
            sb.AppendLine("## Backup Information");
            sb.AppendLine();
            sb.AppendLine($"**Backup Path:** `{result.BackupPath}`");
            sb.AppendLine();
            sb.AppendLine("**Rollback Command:**");
            sb.AppendLine("```bash");
            sb.AppendLine($"relay migrate rollback --backup {result.BackupPath}");
            sb.AppendLine("```");
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine("*Generated by Relay CLI Migration Tool*");

        return sb.ToString();
    }

    private static string GenerateJsonReport(MigrationResult result)
    {
        return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static string GenerateHtmlReport(MigrationResult result)
    {
        var markdown = GenerateMarkdownReport(result);
        // For now, wrap in basic HTML
        // TODO: Use proper markdown to HTML converter
        return $@"<!DOCTYPE html>
<html>
<head>
    <title>Migration Report</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; }}
        table {{ border-collapse: collapse; width: 100%; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #4CAF50; color: white; }}
        code {{ background-color: #f4f4f4; padding: 2px 4px; border-radius: 3px; }}
    </style>
</head>
<body>
    <pre>{System.Web.HttpUtility.HtmlEncode(markdown)}</pre>
</body>
</html>";
    }
}
