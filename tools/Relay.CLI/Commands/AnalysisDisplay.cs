using Spectre.Console;
using Relay.CLI.Commands.Models;

namespace Relay.CLI.Commands;

internal static class AnalysisDisplay
{
    internal static void DisplayAnalysisResults(ProjectAnalysis analysis, string format)
    {
        // Project overview
        var overviewTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Cyan1)
            .Title("[cyan]📊 Project Overview[/]");

        overviewTable.AddColumn("Metric");
        overviewTable.AddColumn("Count");

        overviewTable.AddRow("Project Files", analysis.ProjectFiles.Count.ToString());
        overviewTable.AddRow("Source Files", analysis.SourceFiles.Count.ToString());
        overviewTable.AddRow("Handlers Found", analysis.Handlers.Count.ToString());
        overviewTable.AddRow("Requests Found", analysis.Requests.Count.ToString());
        overviewTable.AddRow("Performance Issues", analysis.PerformanceIssues.Count.ToString());
        overviewTable.AddRow("Reliability Issues", analysis.ReliabilityIssues.Count.ToString());

        AnsiConsole.Write(overviewTable);
        AnsiConsole.WriteLine();

        // Performance issues
        if (analysis.PerformanceIssues.Any())
        {
            AnsiConsole.MarkupLine("[yellow]⚡ Performance Opportunities Found:[/]");
            foreach (var issue in analysis.PerformanceIssues)
            {
                var color = issue.Severity switch
                {
                    "High" => "red",
                    "Medium" => "yellow",
                    _ => "white"
                };

                AnsiConsole.MarkupLine($"[{color}]• {issue.Description}[/]");
                AnsiConsole.MarkupLine($"[dim]  Recommendation: {issue.Recommendation}[/]");
                if (!string.IsNullOrEmpty(issue.PotentialImprovement))
                {
                    AnsiConsole.MarkupLine($"[dim]  Impact: {issue.PotentialImprovement}[/]");
                }
                AnsiConsole.WriteLine();
            }
        }

        // Reliability issues
        if (analysis.ReliabilityIssues.Any())
        {
            AnsiConsole.MarkupLine("[yellow]🛡️ Reliability Improvements:[/]");
            foreach (var issue in analysis.ReliabilityIssues)
            {
                var color = issue.Severity switch
                {
                    "High" => "red",
                    "Medium" => "yellow",
                    _ => "white"
                };

                AnsiConsole.MarkupLine($"[{color}]• {issue.Description}[/]");
                AnsiConsole.MarkupLine($"[dim]  Recommendation: {issue.Recommendation}[/]");
                AnsiConsole.WriteLine();
            }
        }

        // Recommendations
        if (analysis.Recommendations.Any())
        {
            AnsiConsole.MarkupLine("[green]🎯 Action Plan:[/]");
            var priorityOrder = new[] { "High", "Medium", "Low" };

            foreach (var priority in priorityOrder)
            {
                var recs = analysis.Recommendations.Where(r => r.Priority == priority).ToList();
                if (!recs.Any()) continue;

                var color = priority switch
                {
                    "High" => "red",
                    "Medium" => "yellow",
                    _ => "green"
                };

                AnsiConsole.MarkupLine($"[{color}]{priority} Priority:[/]");
                foreach (var rec in recs)
                {
                    AnsiConsole.MarkupLine($"[{color}]  📋 {rec.Title}[/]");
                    AnsiConsole.MarkupLine($"[dim]     {rec.Description}[/]");
                    foreach (var action in rec.Actions.Take(3))
                    {
                        AnsiConsole.MarkupLine($"[dim]     • {action}[/]");
                    }
                    if (rec.Actions.Count > 3)
                    {
                        AnsiConsole.MarkupLine($"[dim]     • ... and {rec.Actions.Count - 3} more action(s)[/]");
                    }
                    AnsiConsole.WriteLine();
                }
            }
        }

        // Overall score
        var score = AnalysisScorer.CalculateOverallScore(analysis);
        var scoreColor = score >= 8 ? "green" : score >= 6 ? "yellow" : "red";
        var emoji = score >= 8 ? "🏆" : score >= 6 ? "👍" : "⚠️";

        AnsiConsole.MarkupLine($"[{scoreColor}]{emoji} Overall Score: {score:F1}/10[/]");

        var assessment = score switch
        {
            >= 9 => "Excellent - Production ready with great performance patterns!",
            >= 8 => "Very Good - Minor optimizations could provide additional benefits",
            >= 7 => "Good - Some improvements recommended for better performance",
            >= 6 => "Fair - Several optimization opportunities available",
            >= 5 => "Needs Improvement - Multiple issues should be addressed",
            _ => "Poor - Significant improvements needed before production use"
        };

        AnsiConsole.MarkupLine($"[dim]{assessment}[/]");
    }
}