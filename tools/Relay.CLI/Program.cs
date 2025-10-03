using System.CommandLine;
using System.Reflection;
using Relay.CLI.Commands;
using Spectre.Console;

namespace Relay.CLI;

class Program
{
    private const string Version = "2.1.0";

    static async Task<int> Main(string[] args)
    {
        // Handle version flag early
        if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v"))
        {
            DisplayVersionInfo();
            return 0;
        }

        // Display banner
        DisplayBanner();

        var rootCommand = new RootCommand("🚀 Relay CLI - High-Performance Mediator Framework Developer Tools")
        {
            Name = "relay"
        };

        // Add commands
        rootCommand.AddCommand(InitCommand.Create());           // 🆕 NEW: Initialize projects
        rootCommand.AddCommand(DoctorCommand.Create());         // 🆕 NEW: Health checks
        rootCommand.AddCommand(MigrateCommand.Create());        // 🆕 NEW: Migration from MediatR
        rootCommand.AddCommand(PluginCommand.Create());         // 🆕 NEW: Plugin management
        rootCommand.AddCommand(ScaffoldCommand.Create());
        rootCommand.AddCommand(BenchmarkCommand.Create());
        rootCommand.AddCommand(AnalyzeCommand.Create());
        rootCommand.AddCommand(OptimizeCommand.Create());
        rootCommand.AddCommand(ValidateCommand.Create());       // ✨ IMPROVED
        rootCommand.AddCommand(GenerateCommand.Create());
        rootCommand.AddCommand(PerformanceCommand.Create());
        rootCommand.AddCommand(AICommand.CreateCommand());

        try
        {
            return await rootCommand.InvokeAsync(args);
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]⚠️  Operation cancelled by user[/]");
            return 130; // Standard exit code for SIGINT
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[red]❌ An error occurred:[/]");
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Run with --help for usage information[/]");
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
        AnsiConsole.MarkupLine($"[dim]Version {Version} - Modern .NET Development[/]");
        AnsiConsole.WriteLine();
    }

    private static void DisplayVersionInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? Version;

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Component[/]")
            .AddColumn("[bold]Version[/]");

        table.AddRow("Relay CLI", Version);
        table.AddRow("Framework", $".NET {Environment.Version.Major}.{Environment.Version.Minor}");
        table.AddRow("Platform", Environment.OSVersion.Platform.ToString());
        table.AddRow("Architecture", System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString());

        AnsiConsole.Write(new Panel(table)
            .Header("[cyan]Version Information[/]")
            .BorderColor(Color.Cyan1));

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]For more info: https://github.com/genc-murat/relay[/]");
    }
}