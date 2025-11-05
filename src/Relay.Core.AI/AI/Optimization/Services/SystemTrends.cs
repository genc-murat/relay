using System;
using System.Collections.Generic;

namespace Relay.Core.AI.Optimization.Services;

/// <summary>
/// System trends analysis
/// </summary>
public class SystemTrends
{
    public TrendDirection CpuTrend { get; set; }
    public TrendDirection MemoryTrend { get; set; }
    public TrendDirection ThroughputTrend { get; set; }
    public TrendDirection ErrorRateTrend { get; set; }
    public TimeSpan AnalysisPeriod { get; set; }
    public double TrendStrength { get; set; }
    public IEnumerable<string> Insights { get; set; } = Array.Empty<string>();
}
