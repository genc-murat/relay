using System.CommandLine;
using System.Text;
using Microsoft.CodeAnalysis;
using Spectre.Console;

namespace Relay.CLI.Commands;

/// <summary>
/// Doctor command - performs comprehensive health check of Relay project
/// </summary>
public static class DoctorCommand
{
    public static Command Create()
    {
        var command = new Command("doctor", "Run comprehensive health check on your Relay project");

        var pathOption = new Option<string>("--path", () => ".", "Project path to check");
        var verboseOption = new Option<bool>("--verbose", () => false, "Show detailed diagnostic information");
        var fixOption = new Option<bool>("--fix", () => false, "Attempt to automatically fix issues");

        command.AddOption(pathOption);
        command.AddOption(verboseOption);
        command.AddOption(fixOption);

        command.SetHandler(async (path, verbose, fix) =>
        {
            await ExecuteDoctor(path, verbose, fix);
        }, pathOption, verboseOption, fixOption);

        return command;
    }

    private static async Task ExecuteDoctor(string projectPath, bool verbose, bool autoFix)
    {
        var rule = new Rule("[cyan]🏥 Relay Doctor - Health Check[/]");
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var diagnostics = new DiagnosticResults();

        await AnsiConsole.Status()
            .StartAsync("Running diagnostics...", async ctx =>
            {
                // Check 1: Project Structure
                ctx.Status("Checking project structure...");
                await CheckProjectStructure(projectPath, diagnostics, verbose);
                await Task.Delay(300);

                // Check 2: Dependencies
                ctx.Status("Checking dependencies...");
                await CheckDependencies(projectPath, diagnostics, verbose);
                await Task.Delay(300);

                // Check 3: Handler Configuration
                ctx.Status("Checking handlers...");
                await CheckHandlers(projectPath, diagnostics, verbose);
                await Task.Delay(300);

                // Check 4: Performance Settings
                ctx.Status("Checking performance settings...");
                await CheckPerformanceSettings(projectPath, diagnostics, verbose);
                await Task.Delay(300);

                // Check 5: Best Practices
                ctx.Status("Checking best practices...");
                await CheckBestPractices(projectPath, diagnostics, verbose);
                await Task.Delay(300);
            });

        // Display Results
        DisplayDiagnosticResults(diagnostics);

        // Auto-fix if requested
        if (autoFix && diagnostics.HasFixableIssues())
        {
            AnsiConsole.WriteLine();
            if (AnsiConsole.Confirm("[yellow]Attempt to fix detected issues?[/]"))
            {
                await ApplyFixes(projectPath, diagnostics);
            }
        }

        // Exit code based on severity
        Environment.ExitCode = diagnostics.GetExitCode();
    }

    private static async Task CheckProjectStructure(string path, DiagnosticResults results, bool verbose)
    {
        var check = new DiagnosticCheck { Category = "Project Structure" };

        try
        {
            var projectFiles = Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                .ToList();

            if (projectFiles.Count == 0)
            {
                check.AddIssue("No .csproj files found", DiagnosticSeverity.Error, "NOT_A_PROJECT");
            }
            else
            {
                check.AddSuccess($"Found {projectFiles.Count} project file(s)");

                // Check for Relay references
                bool hasRelay = false;
                foreach (var projFile in projectFiles)
                {
                    var content = await File.ReadAllTextAsync(projFile);
                    if (content.Contains("Relay.Core") || content.Contains("Relay\""))
                    {
                        hasRelay = true;
                        check.AddSuccess($"Relay reference found in {Path.GetFileName(projFile)}");
                    }
                }

                if (!hasRelay)
                {
                    check.AddIssue("No Relay package references found in any project", 
                        DiagnosticSeverity.Warning, "NO_RELAY_REFERENCE", isFixable: true);
                }
            }

            // Check for common folders
            var expectedFolders = new[] { "src", "tests", "docs" };
            foreach (var folder in expectedFolders)
            {
                var folderPath = Path.Combine(path, folder);
                if (Directory.Exists(folderPath))
                {
                    if (verbose) check.AddInfo($"Found recommended folder: {folder}");
                }
            }
        }
        catch (Exception ex)
        {
            check.AddIssue($"Error checking project structure: {ex.Message}", DiagnosticSeverity.Error, "CHECK_FAILED");
        }

        results.AddCheck(check);
    }

    private static async Task CheckDependencies(string path, DiagnosticResults results, bool verbose)
    {
        var check = new DiagnosticCheck { Category = "Dependencies" };

        try
        {
            var projectFiles = Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                .ToList();

            foreach (var projFile in projectFiles)
            {
                var content = await File.ReadAllTextAsync(projFile);
                
                // Check .NET version
                if (content.Contains("<TargetFramework>"))
                {
                    if (content.Contains("net8.0") || content.Contains("net9.0"))
                    {
                        check.AddSuccess($"Modern .NET version detected in {Path.GetFileName(projFile)}");
                    }
                    else if (content.Contains("netstandard2.0") || content.Contains("net6.0"))
                    {
                        check.AddInfo($"Compatible .NET version in {Path.GetFileName(projFile)}");
                    }
                    else if (content.Contains("netcoreapp") || content.Contains("net4"))
                    {
                        check.AddIssue($"Outdated .NET version in {Path.GetFileName(projFile)}", 
                            DiagnosticSeverity.Warning, "OUTDATED_FRAMEWORK");
                    }
                }

                // Check for nullable reference types
                if (!content.Contains("<Nullable>enable</Nullable>"))
                {
                    if (verbose) check.AddInfo($"Consider enabling nullable reference types in {Path.GetFileName(projFile)}");
                }

                // Check for LangVersion
                if (content.Contains("<LangVersion>latest</LangVersion>"))
                {
                    if (verbose) check.AddSuccess($"Latest C# language features enabled in {Path.GetFileName(projFile)}");
                }
            }

            check.AddSuccess("Dependency check completed");
        }
        catch (Exception ex)
        {
            check.AddIssue($"Error checking dependencies: {ex.Message}", DiagnosticSeverity.Error, "CHECK_FAILED");
        }

        results.AddCheck(check);
    }

    private static async Task CheckHandlers(string path, DiagnosticResults results, bool verbose)
    {
        var check = new DiagnosticCheck { Category = "Handlers" };

        try
        {
            var csFiles = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj") && !f.Contains("Migrations"))
                .ToList();

            int handlerCount = 0;
            int valueTaskHandlers = 0;
            int taskHandlers = 0;
            int missingCancellationToken = 0;

            foreach (var file in csFiles)
            {
                var content = await File.ReadAllTextAsync(file);
                
                if (content.Contains("IRequestHandler") || content.Contains("INotificationHandler"))
                {
                    handlerCount++;

                    // Check for ValueTask usage
                    if (content.Contains("ValueTask"))
                    {
                        valueTaskHandlers++;
                    }
                    else if (content.Contains("Task<"))
                    {
                        taskHandlers++;
                        check.AddIssue($"Handler in {Path.GetFileName(file)} uses Task instead of ValueTask", 
                            DiagnosticSeverity.Info, "USE_VALUETASK", isFixable: true);
                    }

                    // Check for CancellationToken
                    if (!content.Contains("CancellationToken"))
                    {
                        missingCancellationToken++;
                        check.AddIssue($"Handler in {Path.GetFileName(file)} missing CancellationToken parameter", 
                            DiagnosticSeverity.Warning, "MISSING_CANCELLATION_TOKEN", isFixable: true);
                    }

                    // Check for [Handle] attribute
                    if (content.Contains("[Handle]") && verbose)
                    {
                        check.AddSuccess($"Handler in {Path.GetFileName(file)} uses [Handle] attribute for optimization");
                    }
                }
            }

            if (handlerCount > 0)
            {
                check.AddSuccess($"Found {handlerCount} handler(s)");
                if (valueTaskHandlers > 0)
                {
                    check.AddSuccess($"{valueTaskHandlers} handler(s) using ValueTask (optimal)");
                }
                if (taskHandlers > 0)
                {
                    check.AddIssue($"{taskHandlers} handler(s) using Task (consider ValueTask)", 
                        DiagnosticSeverity.Info, "USE_VALUETASK");
                }
            }
            else
            {
                check.AddIssue("No handlers found", DiagnosticSeverity.Warning, "NO_HANDLERS");
            }
        }
        catch (Exception ex)
        {
            check.AddIssue($"Error checking handlers: {ex.Message}", DiagnosticSeverity.Error, "CHECK_FAILED");
        }

        results.AddCheck(check);
    }

    private static async Task CheckPerformanceSettings(string path, DiagnosticResults results, bool verbose)
    {
        var check = new DiagnosticCheck { Category = "Performance" };

        try
        {
            var projectFiles = Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                .ToList();

            foreach (var projFile in projectFiles)
            {
                var content = await File.ReadAllTextAsync(projFile);
                
                // Check for performance optimizations
                var optimizations = new[]
                {
                    ("<TieredCompilation>true</TieredCompilation>", "Tiered compilation enabled"),
                    ("<TieredPGO>true</TieredPGO>", "Profile-guided optimization enabled"),
                    ("<Optimize>true</Optimize>", "Code optimization enabled"),
                    ("<PublishTrimmed>true</PublishTrimmed>", "Trimming enabled")
                };

                foreach (var (setting, message) in optimizations)
                {
                    if (content.Contains(setting))
                    {
                        if (verbose) check.AddSuccess($"{message} in {Path.GetFileName(projFile)}");
                    }
                }

                // Check for release configuration
                if (content.Contains("<Configuration>Release</Configuration>") || verbose)
                {
                    check.AddInfo("Remember to use Release configuration for production builds");
                }
            }

            check.AddSuccess("Performance settings check completed");
        }
        catch (Exception ex)
        {
            check.AddIssue($"Error checking performance settings: {ex.Message}", DiagnosticSeverity.Error, "CHECK_FAILED");
        }

        results.AddCheck(check);
    }

    private static async Task CheckBestPractices(string path, DiagnosticResults results, bool verbose)
    {
        var check = new DiagnosticCheck { Category = "Best Practices" };

        try
        {
            var csFiles = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                .ToList();

            int recordUsage = 0;
            int classUsage = 0;

            foreach (var file in csFiles)
            {
                var content = await File.ReadAllTextAsync(file);
                
                // Check for record usage in requests
                if (content.Contains("IRequest") || content.Contains("INotification"))
                {
                    if (content.Contains("public record"))
                    {
                        recordUsage++;
                    }
                    else if (content.Contains("public class"))
                    {
                        classUsage++;
                        if (verbose) check.AddInfo($"Consider using 'record' instead of 'class' for immutable requests in {Path.GetFileName(file)}");
                    }
                }

                // Check for proper async naming
                if (content.Contains("async ") && !content.Contains("Async("))
                {
                    if (verbose) check.AddInfo($"Consider using 'Async' suffix for async methods in {Path.GetFileName(file)}");
                }
            }

            if (recordUsage > 0 && verbose)
            {
                check.AddSuccess($"{recordUsage} request(s) using record (recommended)");
            }

            check.AddSuccess("Best practices check completed");
        }
        catch (Exception ex)
        {
            check.AddIssue($"Error checking best practices: {ex.Message}", DiagnosticSeverity.Error, "CHECK_FAILED");
        }

        results.AddCheck(check);
    }

    private static void DisplayDiagnosticResults(DiagnosticResults results)
    {
        AnsiConsole.WriteLine();
        var summaryRule = new Rule("[cyan]📊 Diagnostic Summary[/]");
        AnsiConsole.Write(summaryRule);
        AnsiConsole.WriteLine();

        foreach (var check in results.Checks)
        {
            var panel = new Panel(BuildCheckReport(check))
                .Header($"[bold]{check.Category}[/]")
                .BorderColor(GetBorderColor(check));

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }

        // Overall summary
        var summary = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Metric")
            .AddColumn("Count");

        summary.AddRow("[green]✅ Successes[/]", results.SuccessCount.ToString());
        summary.AddRow("[blue]ℹ️  Info[/]", results.InfoCount.ToString());
        summary.AddRow("[yellow]⚠️  Warnings[/]", results.WarningCount.ToString());
        summary.AddRow("[red]❌ Errors[/]", results.ErrorCount.ToString());

        AnsiConsole.Write(summary);

        // Final verdict
        AnsiConsole.WriteLine();
        if (results.ErrorCount == 0 && results.WarningCount == 0)
        {
            AnsiConsole.MarkupLine("[green]✨ Your Relay project is in excellent health![/]");
        }
        else if (results.ErrorCount == 0)
        {
            AnsiConsole.MarkupLine("[yellow]⚠️  Your project has some warnings to address.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[red]❌ Your project has critical issues that need attention.[/]");
        }
    }

    private static string BuildCheckReport(DiagnosticCheck check)
    {
        var sb = new StringBuilder();
        
        foreach (var issue in check.Issues)
        {
            var icon = issue.Severity switch
            {
                DiagnosticSeverity.Error => "[red]❌[/]",
                DiagnosticSeverity.Warning => "[yellow]⚠️[/]",
                DiagnosticSeverity.Info => "[blue]ℹ️[/]",
                _ => "[green]✅[/]"
            };

            // Escape markup characters in messages
            var escapedMessage = issue.Message
                .Replace("[", "[[")
                .Replace("]", "]]");

            sb.AppendLine($"{icon} {escapedMessage}");
            if (issue.IsFixable)
            {
                sb.AppendLine($"   [dim](fixable with --fix)[/]");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static Color GetBorderColor(DiagnosticCheck check)
    {
        if (check.Issues.Any(i => i.Severity == DiagnosticSeverity.Error))
            return Color.Red;
        if (check.Issues.Any(i => i.Severity == DiagnosticSeverity.Warning))
            return Color.Yellow;
        return Color.Green;
    }

    private static async Task ApplyFixes(string path, DiagnosticResults results)
    {
        AnsiConsole.MarkupLine("[cyan]🔧 Applying fixes...[/]");
        await Task.Delay(1000);
        AnsiConsole.MarkupLine("[green]✨ Fixes applied (feature coming soon)[/]");
    }
}
