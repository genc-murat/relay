using System;

namespace Relay.Core.AI
{
    public class SystemValidationResult
    {
        public bool IsStable { get; set; }
        public double StabilityScore { get; set; }
        public SystemValidationIssue[] Issues { get; set; } = Array.Empty<SystemValidationIssue>();
        public DateTime ValidationTime { get; set; }
        public SystemPerformanceInsights SystemInsights { get; set; } = null!;
    }
}