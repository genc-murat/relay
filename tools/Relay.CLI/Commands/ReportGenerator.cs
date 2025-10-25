using System.Text;
using Spectre.Console;
using Relay.CLI.Commands.Models;

namespace Relay.CLI.Commands;

internal static class ReportGenerator
{
    internal static string GenerateHtmlAnalysisReport(ProjectAnalysis analysis)
    {
        try
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Relay Project Analysis Report</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 20px; }}
        .container {{ max-width: 1200px; margin: 0 auto; }}
        .section {{ margin: 20px 0; padding: 20px; border-radius: 8px; }}
        .overview {{ background: #f8f9fa; }}
        .performance {{ background: #fff3cd; }}
        .reliability {{ background: #d1ecf1; }}
        .recommendations {{ background: #d4edda; }}
        .score {{ text-align: center; font-size: 2em; margin: 20px 0; }}
        .issue {{ margin: 10px 0; padding: 10px; border-left: 4px solid #ffc107; }}
        .high {{ border-left-color: #dc3545; }}
        .medium {{ border-left-color: #ffc107; }}
        .low {{ border-left-color: #28a745; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>🔍 Relay Project Analysis Report</h1>
        <p>Generated: {analysis.Timestamp:yyyy-MM-dd HH:mm:ss} UTC</p>

        <div class='score'>
            Overall Score: {AnalysisScorer.CalculateOverallScore(analysis):F1}/10
        </div>

        <div class='section overview'>
            <h2>📊 Project Overview</h2>
            <ul>
                <li>Project Files: {analysis.ProjectFiles.Count}</li>
                <li>Source Files: {analysis.SourceFiles.Count}</li>
                <li>Handlers Found: {analysis.Handlers.Count}</li>
                <li>Requests Found: {analysis.Requests.Count}</li>
            </ul>
        </div>

        <div class='section performance'>
            <h2>⚡ Performance Issues</h2>
            {string.Join("", analysis.PerformanceIssues.Select(i => $@"
            <div class='issue {i.Severity.ToLower()}'>
                <h4>{i.Description}</h4>
                <p><strong>Recommendation:</strong> {i.Recommendation}</p>
                <p><strong>Impact:</strong> {i.PotentialImprovement}</p>
            </div>"))}
        </div>

        <div class='section recommendations'>
            <h2>🎯 Recommendations</h2>
            {string.Join("", analysis.Recommendations.Select(r => $@"
            <div class='issue'>
                <h4>{r.Title}</h4>
                <p>{r.Description}</p>
                <ul>
                    {string.Join("", r.Actions.Select(a => $"<li>{a}</li>"))}
                </ul>
                <p><strong>Estimated Impact:</strong> {r.EstimatedImpact}</p>
            </div>"))}
        </div>
    </div>
</body>
</html>";
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error generating HTML report: {ex.Message}[/]");
            // Return a minimal HTML to ensure command still succeeds
            return $"<html><body><h1>Relay Project Analysis Report</h1><p>Generated: {analysis.Timestamp:yyyy-MM-dd HH:mm:ss} UTC</p><p>Report generation failed: {ex.Message}</p></body></html>";
        }
    }

    internal static string GenerateMarkdownReport(ProjectAnalysis analysis)
    {
        try
        {
            var md = new StringBuilder();
            md.AppendLine("# 🔍 Relay Project Analysis Report");
            md.AppendLine($"Generated: {analysis.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
            md.AppendLine();

            md.AppendLine($"## Overall Score: {AnalysisScorer.CalculateOverallScore(analysis):F1}/10");
            md.AppendLine();

            md.AppendLine("## 📊 Project Overview");
            md.AppendLine($"- Project Files: {analysis.ProjectFiles.Count}");
            md.AppendLine($"- Source Files: {analysis.SourceFiles.Count}");
            md.AppendLine($"- Handlers Found: {analysis.Handlers.Count}");
            md.AppendLine($"- Requests Found: {analysis.Requests.Count}");
            md.AppendLine();

            if (analysis.PerformanceIssues.Any())
            {
                md.AppendLine("## ⚡ Performance Issues");
                foreach (var issue in analysis.PerformanceIssues)
                {
                    md.AppendLine($"### {issue.Description}");
                    md.AppendLine($"**Severity:** {issue.Severity}");
                    md.AppendLine($"**Recommendation:** {issue.Recommendation}");
                    md.AppendLine($"**Impact:** {issue.PotentialImprovement}");
                    md.AppendLine();
                }
            }

            if (analysis.Recommendations.Any())
            {
                md.AppendLine("## 🎯 Recommendations");
                foreach (var rec in analysis.Recommendations)
                {
                    md.AppendLine($"### {rec.Title}");
                    md.AppendLine($"**Priority:** {rec.Priority}");
                    md.AppendLine($"**Description:** {rec.Description}");
                    md.AppendLine("**Actions:**");
                    foreach (var action in rec.Actions)
                    {
                        md.AppendLine($"- {action}");
                    }
                    md.AppendLine($"**Estimated Impact:** {rec.EstimatedImpact}");
                    md.AppendLine();
                }
            }

            return md.ToString();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error generating markdown report: {ex.Message}[/]");
            return $"# Relay Project Analysis Report\nGenerated: {analysis.Timestamp:yyyy-MM-dd HH:mm:ss} UTC\n\nReport generation failed: {ex.Message}";
        }
    }
}