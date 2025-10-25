using Relay.CLI.Commands.Models;

namespace Relay.CLI.Commands;

internal static class AnalysisScorer
{
    internal static double CalculateOverallScore(ProjectAnalysis analysis)
    {
        try
        {
            double score = 10.0;

            // Deduct for performance issues
            score -= (analysis.PerformanceIssues?.Count(i => i.Severity == "High") ?? 0) * 2.0;
            score -= (analysis.PerformanceIssues?.Count(i => i.Severity == "Medium") ?? 0) * 1.0;
            score -= (analysis.PerformanceIssues?.Count(i => i.Severity == "Low") ?? 0) * 0.5;

            // Deduct for reliability issues
            score -= (analysis.ReliabilityIssues?.Count(i => i.Severity == "High") ?? 0) * 1.5;
            score -= (analysis.ReliabilityIssues?.Count(i => i.Severity == "Medium") ?? 0) * 0.8;
            score -= (analysis.ReliabilityIssues?.Count(i => i.Severity == "Low") ?? 0) * 0.3;

            // Bonus for good practices
            if (analysis.HasRelayCore) score += 0.5;
            if (analysis.HasLogging) score += 0.3;
            if (analysis.HasValidation) score += 0.3;
            if (analysis.HasCaching) score += 0.2;

            return Math.Max(0, Math.Min(10, score));
        }
        catch (Exception ex)
        {
            Spectre.Console.AnsiConsole.MarkupLine($"[red]Error calculating overall score: {ex.Message}[/]");
            return 0.0; // Return a safe default
        }
    }
}