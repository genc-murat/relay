using System.CommandLine;

namespace Relay.CLI.Commands;

internal static class MigrateCommandBuilder
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
        var sideBySideOption = new Option<bool>("--side-by-side", () => false, "Use side-by-side diff display");
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
        command.AddOption(sideBySideOption);
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
            var sideBySide = context.ParseResult.GetValueForOption(sideBySideOption);
            var backup = context.ParseResult.GetValueForOption(backupOption);
            var backupPath = context.ParseResult.GetValueForOption(backupPathOption)!;
            var output = context.ParseResult.GetValueForOption(outputOption);
            var format = context.ParseResult.GetValueForOption(formatOption)!;
            var aggressive = context.ParseResult.GetValueForOption(aggressiveOption);
            var interactive = context.ParseResult.GetValueForOption(interactiveOption);

            await MigrateCommandExecutor.ExecuteMigrate(from, to, path, analyzeOnly, dryRun, preview, sideBySide, backup, backupPath, output, format, aggressive, interactive);
        });

        return command;
    }
}