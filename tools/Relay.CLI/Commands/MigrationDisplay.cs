using System.Text;
using Spectre.Console;
using Relay.CLI.Migration;

namespace Relay.CLI.Commands;

internal static class MigrationDisplay
{
    internal static void DisplayAnalysisResults(AnalysisResult analysis)
    {
        DisplayAnalysisResults(analysis, AnsiConsole.Console);
    }

    internal static void DisplayAnalysisResults(AnalysisResult analysis, IAnsiConsole console)
    {
        console.WriteLine();
        console.MarkupLine("[bold cyan]📊 Analysis Results[/]");
        console.MarkupLine($"[bold]Project:[/] {Path.GetFileName(analysis.ProjectPath)}");
        console.MarkupLine($"[bold]Files Affected:[/] {analysis.FilesAffected}");
        console.MarkupLine($"[bold]Handlers Found:[/] {analysis.HandlersFound}");
        console.MarkupLine($"[bold]Requests Found:[/] {analysis.RequestsFound}");
        console.MarkupLine($"[bold]Notifications Found:[/] {analysis.NotificationsFound}");
        console.MarkupLine($"[bold]Pipeline Behaviors:[/] {analysis.PipelineBehaviorsFound}");
        console.WriteLine();

        if (analysis.PackageReferences.Count > 0)
        {
            console.MarkupLine("[bold]📦 Packages to Update:[/]");
            foreach (var pkg in analysis.PackageReferences)
            {
                console.MarkupLine($"  • {pkg.Name} ([red]{pkg.CurrentVersion}[/] → [green]Relay.Core[/])");
            }
            console.WriteLine();
        }

        if (analysis.Issues.Count > 0)
        {
            console.MarkupLine($"[bold yellow]⚠️  Issues Found: {analysis.Issues.Count}[/]");
            foreach (var issue in analysis.Issues.Take(5))
            {
                var icon = issue.Severity switch
                {
                    IssueSeverity.Error => "[red]❌[/]",
                    IssueSeverity.Warning => "[yellow]⚠️[/]",
                    _ => "[blue]ℹ️[/]"
                };
                console.MarkupLine($"  {icon} {issue.Message.Replace("[", "[[").Replace("]", "]]")}");
            }
            if (analysis.Issues.Count > 5)
            {
                console.MarkupLine($"  [dim]... and {analysis.Issues.Count - 5} more[/]");
            }
            console.WriteLine();
        }

        var canMigrateText = analysis.CanMigrate
            ? "[green]✅ Migration can proceed[/]"
            : "[red]❌ Migration blocked - fix critical issues first[/]";
        console.MarkupLine(canMigrateText);
        console.WriteLine();
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
        AnsiConsole.MarkupLine($"[bold]{(isDryRun ? "🔍 Dry Run Results" : "🔄 Migration Results")}[/]");

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

        AnsiConsole.MarkupLine($"[bold]Status:[/] [{statusColor}]{statusText}[/]");
        AnsiConsole.MarkupLine($"[bold]Duration:[/] {result.Duration.TotalSeconds:F2}s");
        AnsiConsole.MarkupLine($"[bold]Files Modified:[/] {result.FilesModified}");
        AnsiConsole.MarkupLine($"[bold]Lines Changed:[/] {result.LinesChanged}");
        AnsiConsole.MarkupLine($"[bold]Handlers Migrated:[/] {result.HandlersMigrated}");

        if (result.CreatedBackup)
        {
            AnsiConsole.MarkupLine($"[bold]Backup Path:[/] {result.BackupPath ?? "N/A"}");
        }

        AnsiConsole.WriteLine();

        // Display changes
        if (result.Changes.Count > 0)
        {
            AnsiConsole.MarkupLine($"[bold cyan]📝 Changes Applied:{(isDryRun ? " (Preview)" : "")}[/]");

            foreach (var change in result.Changes.GroupBy(c => c.Category))
            {
                AnsiConsole.MarkupLine($"[yellow]{change.Key}[/]");
                foreach (var item in change.Take(10))
                {
                    var icon = item.Type switch
                    {
                        ChangeType.Add => "[green]+[/]",
                        ChangeType.Remove => "[red]-[/]",
                        ChangeType.Modify => "[yellow]~[/]",
                        _ => "[blue]•[/]"
                    };
                    AnsiConsole.MarkupLine($"  {icon} {item.Description.Replace("[", "[[").Replace("]", "]]")}");
                }
                if (change.Count() > 10)
                {
                    AnsiConsole.MarkupLine($"  [dim]... and {change.Count() - 10} more[/]");
                }
            }

            AnsiConsole.WriteLine();
        }

        // Display manual steps if any
        if (result.ManualSteps.Count > 0)
        {
            AnsiConsole.MarkupLine("[bold yellow]⚠️  Manual Steps Required:[/]");
            foreach (var step in result.ManualSteps)
            {
                AnsiConsole.MarkupLine($"[yellow]•[/] {step.Replace("[", "[[").Replace("]", "]]")}");
            }
        }

        // Rollback info
        if (!isDryRun && result.CreatedBackup && result.BackupPath != null)
        {
            AnsiConsole.MarkupLine($"[dim]💡 To rollback: relay migrate rollback --backup {result.BackupPath}[/]");
        }

        AnsiConsole.WriteLine();
    }
}