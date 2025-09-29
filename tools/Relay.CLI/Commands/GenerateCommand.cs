using System.CommandLine;
using Spectre.Console;

namespace Relay.CLI.Commands;

public static class GenerateCommand
{
    public static Command Create()
    {
        var command = new Command("generate", "Generate additional components and utilities");

        var typeOption = new Option<string>("--type", "Type to generate (docs, config, benchmark)") { IsRequired = true };
        var outputOption = new Option<string>("--output", () => ".", "Output directory");

        command.AddOption(typeOption);
        command.AddOption(outputOption);

        command.SetHandler(async (type, output) =>
        {
            await ExecuteGenerate(type, output);
        }, typeOption, outputOption);

        return command;
    }

    private static async Task ExecuteGenerate(string type, string outputPath)
    {
        AnsiConsole.MarkupLine($"[cyan]📝 Generating {type}...[/]");
        
        switch (type.ToLower())
        {
            case "docs":
                await GenerateDocs(outputPath);
                break;
            case "config":
                await GenerateConfig(outputPath);
                break;
            case "benchmark":
                await GenerateBenchmark(outputPath);
                break;
            default:
                AnsiConsole.MarkupLine("[red]❌ Unknown generation type[/]");
                break;
        }
    }

    private static async Task GenerateDocs(string outputPath)
    {
        var docsContent = @"# Project Documentation

## Overview
This project uses Relay framework for high-performance request/response handling.

## Getting Started
1. Install dependencies: `dotnet restore`
2. Run the application: `dotnet run`
3. Run tests: `dotnet test`

## Performance
This implementation achieves 80%+ better performance than traditional mediator patterns.
";

        var docsPath = Path.Combine(outputPath, "README.md");
        await File.WriteAllTextAsync(docsPath, docsContent);
        AnsiConsole.MarkupLine($"[green]✅ Documentation generated: {docsPath}[/]");
    }

    private static async Task GenerateConfig(string outputPath)
    {
        var configContent = @"{
  ""Relay"": {
    ""EnablePerformanceOptimizations"": true,
    ""UseValueTask"": true,
    ""EnableSourceGeneration"": true,
    ""Telemetry"": {
      ""Enabled"": true,
      ""CollectMetrics"": true
    }
  }
}";

        var configPath = Path.Combine(outputPath, "appsettings.relay.json");
        await File.WriteAllTextAsync(configPath, configContent);
        AnsiConsole.MarkupLine($"[green]✅ Configuration generated: {configPath}[/]");
    }

    private static async Task GenerateBenchmark(string outputPath)
    {
        var benchmarkContent = @"using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

[MemoryDiagnoser]
[SimpleJob]
public class RelayBenchmark
{
    [Benchmark]
    public async Task SendRequest()
    {
        // Add your benchmark code here
        await Task.Delay(1);
    }
}

public class Program
{
    public static void Main(string[] args) =>
        BenchmarkRunner.Run<RelayBenchmark>();
}";

        var benchmarkPath = Path.Combine(outputPath, "RelayBenchmark.cs");
        await File.WriteAllTextAsync(benchmarkPath, benchmarkContent);
        AnsiConsole.MarkupLine($"[green]✅ Benchmark template generated: {benchmarkPath}[/]");
    }
}