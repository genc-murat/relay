using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Relay.CLI.Commands.Models;

namespace Relay.CLI.Commands;

public static class AnalyzeCommand
{
    public static Command Create() => AnalyzeCommandBuilder.Create();

    [RequiresUnreferencedCode("Calls Relay.CLI.Commands.AnalyzeCommand.SaveAnalysisResults(ProjectAnalysis, String, String)")]
    [RequiresDynamicCode("Calls Relay.CLI.Commands.AnalyzeCommand.SaveAnalysisResults(ProjectAnalysis, String, String)")]
    internal static async Task<int> ExecuteAnalyze(string projectPath, string? outputPath, string format, string depth, bool includeTests)
    {
        // Check if project directory exists
        if (!Directory.Exists(projectPath))
        {
            AnsiConsole.MarkupLine("[red]❌ Error: Project directory does not exist[/]");
            return 1; // Return error exit code
        }

        AnsiConsole.MarkupLine($"[cyan]🔍 Analyzing project at: {projectPath}[/]");
        AnsiConsole.WriteLine();

        var analyzer = new ProjectAnalyzer();
        ProjectAnalysis analysis;

        try
        {
            analysis = await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var overallTask = ctx?.AddTask("[cyan]Analyzing project[/]", maxValue: 7);
                    return await analyzer.AnalyzeProject(projectPath, depth, includeTests, ctx, overallTask);
                });
        }
        catch (Exception ex) when (ex is not DirectoryNotFoundException)
        {
            // Log the progress error but continue execution
            // This can happen in test environments where console features are not available
            Console.WriteLine($"Warning: Could not show progress in console: {ex.Message}");

            // Run analysis without progress UI in test environments
            analysis = await analyzer.AnalyzeProject(projectPath, depth, includeTests);
        }
        catch (DirectoryNotFoundException)
        {
            // Handle invalid project path
            AnsiConsole.MarkupLine("[red]❌ Error: Project directory does not exist[/]");
            return 1; // Return error exit code
        }

        try
        {
            // Display results
            AnalysisDisplay.DisplayAnalysisResults(analysis, format);
        }
        catch (Exception ex)
        {
            // Log the display error but continue execution
            Console.WriteLine($"Warning: Could not display results in console: {ex.Message}");
        }

        // Save results if requested
        if (!string.IsNullOrEmpty(outputPath))
        {
            await SaveAnalysisResults(analysis, outputPath, format);
        }

        return 0;
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    internal static async Task SaveAnalysisResults(ProjectAnalysis analysis, string outputPath, string format)
    {
        var content = format.ToLower() switch
        {
            "json" => System.Text.Json.JsonSerializer.Serialize(analysis, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
            "html" => ReportGenerator.GenerateHtmlAnalysisReport(analysis),
            "markdown" => ReportGenerator.GenerateMarkdownReport(analysis),
            _ => System.Text.Json.JsonSerializer.Serialize(analysis, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })
        };

        try
        {
            await File.WriteAllTextAsync(outputPath, content);
            AnsiConsole.MarkupLine($"[green]✓ Analysis report saved to: {outputPath}[/]");
        }
        catch (UnauthorizedAccessException)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error: Insufficient permissions to write to {outputPath}[/]");
            throw;
        }
        catch (DirectoryNotFoundException)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error: Directory does not exist: {Path.GetDirectoryName(outputPath)}[/]");
            throw;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error saving analysis report: {ex.Message}[/]");
            throw;
        }
    }
}