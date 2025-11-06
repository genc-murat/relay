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
        try
        {
            console.WriteLine();
            console.MarkupLine("[bold cyan]📊 Analysis Results[/]");
            string report = BuildAnalysisReport(analysis);
            var lines = report.Replace("\r\n", "\n").Split('\n');
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line))
                    console.WriteLine();
                else
                    console.MarkupLine(line);
            }
            console.WriteLine();
        }
        catch
        {
            // Ignore console output errors in test environments
        }
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
        DisplayMigrationResults(result, isDryRun, AnsiConsole.Console);
    }

    internal static void DisplayMigrationResults(MigrationResult result, bool isDryRun, IAnsiConsole console)
    {
        try
        {
            console.WriteLine();
        }
        catch
        {
            // Ignore console output errors in test environments
        }

        try
        {
            console.MarkupLine($"[bold]{(isDryRun ? "🔍 Dry Run Results" : "🔄 Migration Results")}[/]");
        }
        catch
        {
            // Ignore console output errors in test environments
        }

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

        try
        {
            console.MarkupLine($"[bold]Status:[/] [{statusColor}]{statusText}[/]");
        }
        catch
        {
            // Ignore console output errors in test environments
        }

        try
        {
            console.MarkupLine($"[bold]Duration:[/] {result.Duration.TotalSeconds:F2}s");
        }
        catch
        {
            // Ignore console output errors in test environments
        }

        try
        {
            console.MarkupLine($"[bold]Files Modified:[/] {result.FilesModified}");
        }
        catch
        {
            // Ignore console output errors in test environments
        }

        try
        {
            console.MarkupLine($"[bold]Lines Changed:[/] {result.LinesChanged}");
        }
        catch
        {
            // Ignore console output errors in test environments
        }

        try
        {
            console.MarkupLine($"[bold]Handlers Migrated:[/] {result.HandlersMigrated}");
        }
        catch
        {
            // Ignore console output errors in test environments
        }

        if (result.CreatedBackup)
        {
            try
            {
                console.MarkupLine($"[bold]Backup Path:[/] {result.BackupPath ?? "N/A"}");
            }
            catch
            {
                // Ignore console output errors in test environments
            }
        }

        try
        {
            console.WriteLine();
        }
        catch
        {
            // Ignore console output errors in test environments
        }

        // Display changes
        if (result.Changes.Count > 0)
        {
            try
            {
                console.MarkupLine($"[bold cyan]📝 Changes Applied:{(isDryRun ? " (Preview)" : "")}[/]");
            }
            catch
            {
                // Ignore console output errors in test environments
            }

            foreach (var change in result.Changes.GroupBy(c => c.Category))
            {
                try
                {
                    console.MarkupLine($"[yellow]{change.Key}[/]");
                }
                catch
                {
                    // Ignore console output errors in test environments
                }

                foreach (var item in change.Take(10))
                {
                    var icon = item.Type switch
                    {
                        ChangeType.Add => "[green]+[/]",
                        ChangeType.Remove => "[red]-[/]",
                        ChangeType.Modify => "[yellow]~[/]",
                        _ => "[blue]•[/]"
                    };

                    try
                    {
                        console.MarkupLine($"  {icon} {item.Description.Replace("[", "[[").Replace("]", "]]")}");
                    }
                    catch
                    {
                        // Ignore console output errors in test environments
                    }
                }

                if (change.Count() > 10)
                {
                    try
                    {
                        console.MarkupLine($"  [dim]... and {change.Count() - 10} more[/]");
                    }
                    catch
                    {
                        // Ignore console output errors in test environments
                    }
                }
            }

            try
            {
                console.WriteLine();
            }
            catch
            {
                // Ignore console output errors in test environments
            }
        }

        // Display manual steps if any
        if (result.ManualSteps.Count > 0)
        {
            try
            {
                console.MarkupLine("[bold yellow]⚠️  Manual Steps Required:[/]");
            }
            catch
            {
                // Ignore console output errors in test environments
            }

            foreach (var step in result.ManualSteps)
            {
                try
                {
                    console.MarkupLine($"[yellow]•[/] {step.Replace("[", "[[").Replace("]", "]]")}");
                }
                catch
                {
                    // Ignore console output errors in test environments
                }
            }
        }

        // Rollback info
        if (!isDryRun && result.CreatedBackup && result.BackupPath != null)
        {
            try
            {
                console.MarkupLine($"[dim]💡 To rollback: relay migrate rollback --backup {result.BackupPath}[/]");
            }
            catch
            {
                // Ignore console output errors in test environments
            }
        }

        try
        {
            console.WriteLine();
        }
        catch
        {
            // Ignore console output errors in test environments
        }
    }
}