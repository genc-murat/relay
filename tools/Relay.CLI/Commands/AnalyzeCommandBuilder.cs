using System.CommandLine;

namespace Relay.CLI.Commands;

internal static class AnalyzeCommandBuilder
{
    public static Command Create()
    {
        var command = new Command("analyze", "Analyze your project for performance optimization opportunities");

        var pathOption = new Option<string>("--path", () => ".", "Project path to analyze");
        var outputOption = new Option<string>("--output", "Output file for analysis report");
        var formatOption = new Option<string>("--format", () => "console", "Output format (console, json, html, markdown)");
        var depthOption = new Option<string>("--depth", () => "full", "Analysis depth (quick, standard, full, deep)");
        var includeTestsOption = new Option<bool>("--include-tests", () => false, "Include test projects in analysis");

        command.AddOption(pathOption);
        command.AddOption(outputOption);
        command.AddOption(formatOption);
        command.AddOption(depthOption);
        command.AddOption(includeTestsOption);

        command.SetHandler(AnalyzeCommand.ExecuteAnalyze, pathOption, outputOption, formatOption, depthOption, includeTestsOption);

        return command;
    }
}