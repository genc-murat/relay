namespace Relay.CLI.Commands;

public class PerformanceAnalysis
{
    public int ProjectCount { get; set; }
    public int HandlerCount { get; set; }
    public int OptimizedHandlerCount { get; set; }
    public int CachedHandlerCount { get; set; }
    public int AsyncMethodCount { get; set; }
    public int ValueTaskCount { get; set; }
    public int TaskCount { get; set; }
    public int CancellationTokenCount { get; set; }
    public int ConfigureAwaitCount { get; set; }
    public int RecordCount { get; set; }
    public int StructCount { get; set; }
    public int LinqUsageCount { get; set; }
    public int StringBuilderCount { get; set; }
    public int StringConcatInLoopCount { get; set; }
    public bool HasRelay { get; set; }
    public bool ModernFramework { get; set; }
    public bool HasPGO { get; set; }
    public bool HasOptimizations { get; set; }
    public int PerformanceScore { get; set; }
    public List<PerformanceRecommendation> Recommendations { get; } = new();
}
