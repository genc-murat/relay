using System.Text;
using Spectre.Console;
using Relay.CLI.Migration;

namespace Relay.CLI.Commands;

internal static class MigrationDisplay
{
    internal static void DisplayAnalysisResults(AnalysisResult analysis)
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

    internal static void DisplayMigrationResults(MigrationResult result, bool isDryRun)
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
}