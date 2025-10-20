namespace Relay.CLI.Commands;

// Data models for AI CLI results
public class AIAnalysisResults
{
    public string ProjectPath { get; set; } = "";
    public int FilesAnalyzed { get; set; }
    public int HandlersFound { get; set; }
    public double PerformanceScore { get; set; }
    public double AIConfidence { get; set; }
    public AIPerformanceIssue[] PerformanceIssues { get; set; } = Array.Empty<AIPerformanceIssue>();
    public OptimizationOpportunity[] OptimizationOpportunities { get; set; } = Array.Empty<OptimizationOpportunity>();
}
