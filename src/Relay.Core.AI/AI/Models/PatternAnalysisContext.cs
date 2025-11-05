using Relay.Core.AI.Models;
using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Supporting analysis classes for advanced pattern recognition
    /// </summary>
    internal class PatternAnalysisContext
    {
        public Type RequestType { get; set; } = null!;
        public RequestAnalysisData AnalysisData { get; set; } = null!;
        public RequestExecutionMetrics CurrentMetrics { get; set; } = null!;
        public SystemLoadMetrics SystemLoad { get; set; } = null!;
        public double HistoricalTrend { get; set; }
    }
}
