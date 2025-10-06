using Relay.CLI.Commands.Models.Performance;

namespace Relay.CLI.Commands.Models;

// Data classes for analysis
public class ProjectAnalysis
{
    public string ProjectPath { get; set; } = "";
    public string AnalysisDepth { get; set; } = "";
    public bool IncludeTests { get; set; }
    public DateTime Timestamp { get; set; }
    public List<string> ProjectFiles { get; set; } = new();
    public List<string> SourceFiles { get; set; } = new();
    public List<HandlerInfo> Handlers { get; set; } = new();
    public List<RequestInfo> Requests { get; set; } = new();
    public List<PerformanceIssue> PerformanceIssues { get; set; } = new();
    public List<ReliabilityIssue> ReliabilityIssues { get; set; } = new();
    public List<Recommendation> Recommendations { get; set; } = new();
    public bool HasRelayCore { get; set; }
    public bool HasMediatR { get; set; }
    public bool HasLogging { get; set; }
    public bool HasValidation { get; set; }
    public bool HasCaching { get; set; }
}


