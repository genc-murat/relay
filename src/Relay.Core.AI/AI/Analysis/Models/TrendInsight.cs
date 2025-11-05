namespace Relay.Core.AI;

public class TrendInsight
{
    public string Category { get; set; } = string.Empty;
    public InsightSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
}
