using System.CommandLine;
using System.Text;
using System.Text.RegularExpressions;
using Spectre.Console;
using Relay.CLI.Commands.Models.Optimization;

namespace Relay.CLI.Commands;

public static class OptimizeCommand
{
    public static Command Create()
    {
        var command = new Command("optimize", "Apply automatic optimizations to improve performance");

        var pathOption = new Option<string>("--path", () => ".", "Project path to optimize");
        var dryRunOption = new Option<bool>("--dry-run", () => false, "Show what would be optimized without applying changes");
        var targetOption = new Option<string>("--target", () => "all", "Optimization target (all, handlers, requests, config)");
        var aggressiveOption = new Option<bool>("--aggressive", () => false, "Apply aggressive optimizations");
        var backupOption = new Option<bool>("--backup", () => true, "Create backup before applying changes");

        command.AddOption(pathOption);
        command.AddOption(dryRunOption);
        command.AddOption(targetOption);
        command.AddOption(aggressiveOption);
        command.AddOption(backupOption);

        command.SetHandler(async (path, dryRun, target, aggressive, backup) =>
        {
            await ExecuteOptimize(path, dryRun, target, aggressive, backup);
        }, pathOption, dryRunOption, targetOption, aggressiveOption, backupOption);

        return command;
    }

    internal static async Task ExecuteOptimize(string projectPath, bool dryRun, string target, bool aggressive, bool backup)
    {
        var title = dryRun ? "🔍 DRY RUN: Analyzing potential optimizations..." : "🔧 Optimizing Relay project...";
        SafeMarkupLine($"[cyan]{title}[/]");
        AnsiConsole.WriteLine();

        var optimization = new OptimizationContext
        {
            ProjectPath = Path.GetFullPath(projectPath),
            IsDryRun = dryRun,
            Target = target,
            IsAggressive = aggressive,
            CreateBackup = backup,
            Timestamp = DateTime.UtcNow
        };

        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var overallTask = ctx.AddTask("[cyan]Running optimizations[/]", maxValue: GetOptimizationSteps(target));

                // Discover files
                await DiscoverFiles(optimization, ctx, overallTask);
                overallTask.Increment(1);

                // Default to "all" if target is invalid
                var effectiveTarget = target;
                if (target != "all" && target != "handlers")
                {
                    effectiveTarget = "all";
                }

                if (effectiveTarget == "all" || effectiveTarget == "handlers")
                {
                    await OptimizeHandlers(optimization, ctx, overallTask);
                    overallTask.Increment(1);
                }

                overallTask.Value = overallTask.MaxValue;
            });

        // Display results
        DisplayOptimizationResults(optimization);
    }

    private static void SafeMarkupLine(string markup)
    {
        try
        {
            AnsiConsole.MarkupLine(markup);
        }
        catch (ArgumentException)
        {
            // Fallback for test environments where markup might fail due to internal buffer issues
            AnsiConsole.WriteLine(System.Text.RegularExpressions.Regex.Replace(markup, @"\[.*?\]", ""));
        }
    }

    internal static async Task DiscoverFiles(OptimizationContext context, ProgressContext ctx, ProgressTask overallTask)
    {
        try
        {
            if (!Directory.Exists(context.ProjectPath))
            {
                // Directory doesn't exist, just return empty list
                if (ctx != null)
                {
                    SafeMarkupLine($"[dim]Directory {context.ProjectPath} does not exist[/]");
                }
                await Task.CompletedTask;
                return;
            }

            var csFiles = Directory.GetFiles(context.ProjectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj")).ToList();
            context.SourceFiles.AddRange(csFiles);

            if (ctx != null)
            {
                var discoveryTask = ctx.AddTask("[green]Discovering files[/]");
                discoveryTask.Value = discoveryTask.MaxValue;
                SafeMarkupLine($"[dim]Found {csFiles.Count} source files[/]");
            }
        }
        catch (Exception ex)
        {
            if (ctx != null)
            {
                SafeMarkupLine($"[red]Error discovering files: {ex.Message}[/]");
            }
        }

        await Task.CompletedTask;
    }

    internal static async Task OptimizeHandlers(OptimizationContext context, ProgressContext ctx, ProgressTask overallTask)
    {
        ProgressTask handlerTask = null;
        if (ctx != null)
        {
            handlerTask = ctx.AddTask("[yellow]Optimizing handlers[/]");
        }
        var optimizedCount = 0;

        foreach (var file in context.SourceFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            var originalContent = content;
            var modifications = new List<string>();

            // Only optimize handler files (contain IRequestHandler or [Handle])
            if (content.Contains("IRequestHandler") || content.Contains("[Handle]"))
            {
                // Replace Task with ValueTask
                if (content.Contains("Task<") && !content.Contains("ValueTask<"))
                {
                    var taskPattern = @"public\s+async\s+Task<([^>]+)>\s+(\w+)\s*\(";
                    if (Regex.IsMatch(content, taskPattern))
                    {
                        content = Regex.Replace(content, taskPattern, "public async ValueTask<$1> $2(");
                        modifications.Add("Replaced Task<T> with ValueTask<T> for better performance");
                    }
                }
            }

            if (content != originalContent)
            {
                context.OptimizationActions.Add(new OptimizationAction
                {
                    FilePath = file,
                    Type = "Handler Optimization",
                    Modifications = modifications,
                    OriginalContent = originalContent,
                    OptimizedContent = content
                });

                if (!context.IsDryRun)
                {
                    try
                    {
                        if (context.CreateBackup)
                        {
                            var backupPath = file + ".bak";
                            File.Copy(file, backupPath, true);
                        }
                        await File.WriteAllTextAsync(file, content);
                    }
                    catch (Exception ex)
                    {
                        if (ctx != null)
                        {
                            SafeMarkupLine($"[red]Error writing file {Path.GetFileName(file)}: {ex.Message}[/]");
                        }
                        // Continue with other files
                    }
                }
                optimizedCount++;
            }
        }

        if (handlerTask != null)
        {
            handlerTask.Value = handlerTask.MaxValue;
            SafeMarkupLine($"[dim]Optimized {optimizedCount} handler file(s)[/]");
        }
    }

    internal static void DisplayOptimizationResults(OptimizationContext context)
    {
        if (!context.OptimizationActions.Any())
        {
            SafeMarkupLine("[green]✅ No optimizations needed - your project is already well optimized![/]");
            return;
        }

        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Cyan1)
            .Title($"[cyan]{(context.IsDryRun ? "🔍 Potential Optimizations" : "✅ Applied Optimizations")}[/]");

        table.AddColumn("File");
        table.AddColumn("Type");
        table.AddColumn("Modifications");

        foreach (var action in context.OptimizationActions)
        {
            var fileName = Path.GetFileName(action.FilePath);
            var modifications = string.Join("\n", action.Modifications.Take(3));
            table.AddRow(fileName, action.Type, modifications);
        }

        AnsiConsole.Write(table);

        var totalModifications = context.OptimizationActions.Sum(a => a.Modifications.Count);
        AnsiConsole.WriteLine();
        
        if (context.IsDryRun)
        {
            SafeMarkupLine($"[yellow]📊 Summary: {totalModifications} optimization(s) available[/]");
            SafeMarkupLine("[yellow]💡 Run without --dry-run to apply these optimizations[/]");
        }
        else
        {
            SafeMarkupLine($"[green]🚀 Applied {totalModifications} optimization(s)[/]");
            SafeMarkupLine("[green]✨ Your Relay implementation is now optimized![/]");
        }
    }

    internal static int GetOptimizationSteps(string target) => target switch
    {
        "all" => 2,
        "handlers" => 2,
        _ => 1
    };
}
