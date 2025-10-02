using System.CommandLine;
using Relay.CLI.Commands;
using Spectre.Console;

namespace Relay.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Display banner
        DisplayBanner();

        var rootCommand = new RootCommand("🚀 Relay CLI - High-Performance Mediator Framework Developer Tools")
        {
            Name = "relay"
        };

        // Add commands
        rootCommand.AddCommand(ScaffoldCommand.Create());
        rootCommand.AddCommand(BenchmarkCommand.Create());
        rootCommand.AddCommand(AnalyzeCommand.Create());
        rootCommand.AddCommand(OptimizeCommand.Create());
        rootCommand.AddCommand(ValidateCommand.Create());
        rootCommand.AddCommand(GenerateCommand.Create());
        rootCommand.AddCommand(PerformanceCommand.Create());
        rootCommand.AddCommand(AICommand.CreateCommand()); // 🤖 AI-Powered Features

        try
        {
            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    private static void DisplayBanner()
    {
        var banner = new FigletText("Relay CLI")
            .Centered()
            .Color(Color.Cyan1);

        AnsiConsole.Write(banner);
        
        AnsiConsole.MarkupLine("[cyan]🚀 High-Performance Mediator Framework Developer Tools[/]");
        AnsiConsole.MarkupLine("[dim]Version 2.0.0 - Modern .NET Development Tools[/]");
        AnsiConsole.WriteLine();
    }
}