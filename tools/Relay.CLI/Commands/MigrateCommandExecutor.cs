using Spectre.Console;
using Relay.CLI.Migration;

namespace Relay.CLI.Commands;

internal static class MigrateCommandExecutor
{
    internal static async Task ExecuteMigrate(
        string from,
        string to,
        string projectPath,
        bool analyzeOnly,
        bool dryRun,
        bool preview,
        bool sideBySide,
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
            UseSideBySideDiff = sideBySide,
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

                    MigrationDisplay.DisplayAnalysisResults(analysis);

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

                    // Phase 3: Migration or Preview
                    if (options.Interactive)
                    {
                        ctx.Status("🔄 Interactive migration...");
                        result = await engine.MigrateInteractiveAsync(options);
                    }
                    else if (options.ShowPreview && options.DryRun)
                    {
                        ctx.Status("🔍 Showing preview...");
                        result = await engine.PreviewAsync(options);
                    }
                    else
                    {
                        ctx.Status("🔄 Applying migration...");
                        result = await engine.MigrateAsync(options);
                    }

                    await Task.Delay(500);
                });

            // Display results
            if (result != null)
            {
                MigrationDisplay.DisplayMigrationResults(result, dryRun);

                // Save report
                if (!string.IsNullOrEmpty(outputFile))
                {
                    await MigrationReportGenerator.SaveMigrationReport(result, outputFile, format);
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
}